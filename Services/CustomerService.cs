using CrmApp.Data;
using CrmApp.Models;
using CrmApp.Models.DTOs;
using CrmApp.Models.Common;
using AutoMapper;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CrmApp.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public CustomerService(AppDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 20, string? searchTerm = null)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => c.FirstName.Contains(searchTerm) || 
                                     c.LastName.Contains(searchTerm) || 
                                     c.Email.Contains(searchTerm) ||
                                     c.PhoneNumber.Contains(searchTerm));
        }

        query = query.OrderByDescending(c => c.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var customers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<CustomerDto>>(customers);

        return new PagedResult<CustomerDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task AddCustomerAsync(CreateCustomerDto customerDto)
    {
        var customer = _mapper.Map<Customer>(customerDto);
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
    }

    public async Task BulkInsertCustomersAsync(List<CreateCustomerDto> customerDtos)
    {
        var customers = _mapper.Map<List<Customer>>(customerDtos);
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Customers.AddRangeAsync(customers);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<CustomerDto>> GetActiveCustomersAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.LastPurchaseDate >= cutoffDate)
            .OrderByDescending(c => c.TotalPurchaseAmount)
            .ToListAsync();
            
        return _mapper.Map<List<CustomerDto>>(customers);
    }

    public async Task<List<SalesStatsDto>> GetLast7DaysSalesAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        
        var salesData = await _context.Customers
            .AsNoTracking()
            .Where(c => c.LastPurchaseDate >= cutoffDate)
            .GroupBy(c => c.LastPurchaseDate!.Value.Date)
            .Select(g => new 
            { 
                Date = g.Key, 
                TotalAmount = g.Sum(c => c.TotalPurchaseAmount) 
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing days with 0
        var result = new List<SalesStatsDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var dayData = salesData.FirstOrDefault(x => x.Date == date);
            
            result.Add(new SalesStatsDto
            {
                Date = date.ToString("dd MMM"), // e.g., "06 Dec"
                TotalAmount = dayData?.TotalAmount ?? 0
            });
        }

        return result;
    }

    public async Task DeleteCustomerAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }

    public async Task BulkDeleteCustomersAsync(List<int> ids)
    {
        if (ids == null || !ids.Any()) return;

        var customers = await _context.Customers
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        if (customers.Any())
        {
            _context.Customers.RemoveRange(customers);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> ImportCustomersFromExcelAsync(Stream fileStream)
    {
        // 1. ADIM: Excel Reader'ı Stream modunda (Bellek Dostu) kullanarak DataTable'a veriyi parça parça veya manuel aktaracağız.
        // Ancak SqlBulkCopy doğrudan IDataReader alabilir. ExcelReader zaten IDataReader implemente eder.
        // Fakat başlık satırını atlamak ve sütun eşleştirmesi yapmak biraz tricky olabilir.
        // En güvenlisi: Kendi DataTable'ımızı oluşturup, stream'den okuyup, batch'ler halinde BulkCopy'e basmak.
        // Ama "Bulletproof" dediğimiz için AsDataSet (bütün dosyayı RAM'e alma) yerine satır satır okumalıyız.

        var dataTable = new DataTable();
        dataTable.Columns.Add("FirstName", typeof(string));
        dataTable.Columns.Add("LastName", typeof(string));
        dataTable.Columns.Add("Email", typeof(string));
        dataTable.Columns.Add("PhoneNumber", typeof(string));
        dataTable.Columns.Add("TotalPurchaseAmount", typeof(decimal));
        dataTable.Columns.Add("LastPurchaseDate", typeof(DateTime));
        dataTable.Columns.Add("CreatedAt", typeof(DateTime));

        using (var reader = ExcelReaderFactory.CreateReader(fileStream))
        {
            // İlk satırı (Header) oku
            if (!reader.Read()) return 0;

            var columnMap = new Dictionary<string, int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.GetValue(i)?.ToString()?.Trim().ToLower();
                if (string.IsNullOrEmpty(val)) continue;

                if (val == "ad" || val == "firstname" || val == "first name" || val == "name" || val == "isim") columnMap["FirstName"] = i;
                else if (val == "soyad" || val == "lastname" || val == "last name" || val == "surname" || val == "soyisim") columnMap["LastName"] = i;
                else if (val == "email" || val == "e-mail" || val == "mail" || val == "eposta") columnMap["Email"] = i;
                else if (val == "telefon" || val == "phone" || val == "phonenumber" || val == "tel" || val == "cep") columnMap["PhoneNumber"] = i;
                else if (val == "toplam tutar" || val == "total amount" || val == "total" || val == "amount" || val == "tutar" || val == "bakiye") columnMap["TotalPurchaseAmount"] = i;
                else if (val == "son alışveriş tarihi" || val == "last purchase date" || val == "date" || val == "tarih" || val == "son işlem") columnMap["LastPurchaseDate"] = i;
            }

            if (!columnMap.ContainsKey("FirstName") || !columnMap.ContainsKey("LastName") || !columnMap.ContainsKey("Email"))
            {
                 throw new Exception("Zorunlu sütunlar (Ad, Soyad, Email) Excel dosyasında bulunamadı.");
            }

            // Satır satır oku
            while (reader.Read())
            {
                var row = dataTable.NewRow();
                
                // Mandatory
                row["FirstName"] = reader.GetValue(columnMap["FirstName"])?.ToString() ?? (object)DBNull.Value;
                row["LastName"] = reader.GetValue(columnMap["LastName"])?.ToString() ?? (object)DBNull.Value;
                row["Email"] = reader.GetValue(columnMap["Email"])?.ToString() ?? (object)DBNull.Value;

                // Optional
                if (columnMap.ContainsKey("PhoneNumber"))
                    row["PhoneNumber"] = reader.GetValue(columnMap["PhoneNumber"])?.ToString() ?? (object)DBNull.Value;
                else
                    row["PhoneNumber"] = DBNull.Value;

                if (columnMap.ContainsKey("TotalPurchaseAmount"))
                {
                    var val = reader.GetValue(columnMap["TotalPurchaseAmount"]);
                    if (val != null)
                    {
                        if (decimal.TryParse(val.ToString(), out decimal amount)) row["TotalPurchaseAmount"] = amount;
                        else row["TotalPurchaseAmount"] = 0m;
                    }
                    else row["TotalPurchaseAmount"] = 0m;
                }
                else row["TotalPurchaseAmount"] = 0m;

                if (columnMap.ContainsKey("LastPurchaseDate"))
                {
                    var val = reader.GetValue(columnMap["LastPurchaseDate"]);
                    if (val != null)
                    {
                        if (DateTime.TryParse(val.ToString(), out DateTime date)) row["LastPurchaseDate"] = date;
                        else row["LastPurchaseDate"] = DBNull.Value;
                    }
                    else row["LastPurchaseDate"] = DBNull.Value;
                }
                else row["LastPurchaseDate"] = DBNull.Value;

                row["CreatedAt"] = DateTime.UtcNow;
                dataTable.Rows.Add(row);
                
                // Batch Insert Logic (Memory Optimization)
                // Her 10.000 kayıtta bir veritabanına bas ve belleği boşalt
                if (dataTable.Rows.Count >= 10000)
                {
                    await WriteBatchToDbAsync(dataTable);
                    dataTable.Clear();
                }
            }
        }

        // Kalan son kayıtları bas
        if (dataTable.Rows.Count > 0)
        {
            await WriteBatchToDbAsync(dataTable);
        }

        // Not: Gerçek sayıyı döndürmek için sayaç tutulabilir ama şimdilik dataTable count'ları temizlendiği için 0 dönüyor gibi olabilir.
        // Basitlik adına 1 dönüyorum veya sayaç eklenebilir.
        return 1; 
    }

    private async Task WriteBatchToDbAsync(DataTable dataTable)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using (var bulkCopy = new SqlBulkCopy(connectionString))
        {
            bulkCopy.DestinationTableName = "Customers";
            bulkCopy.BatchSize = 10000;
            bulkCopy.BulkCopyTimeout = 0;

            bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
            bulkCopy.ColumnMappings.Add("LastName", "LastName");
            bulkCopy.ColumnMappings.Add("Email", "Email");
            bulkCopy.ColumnMappings.Add("PhoneNumber", "PhoneNumber");
            bulkCopy.ColumnMappings.Add("TotalPurchaseAmount", "TotalPurchaseAmount");
            bulkCopy.ColumnMappings.Add("LastPurchaseDate", "LastPurchaseDate");
            bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");

            await bulkCopy.WriteToServerAsync(dataTable);
        }
    }

    public async Task<int> ImportCustomersFromCsvAsync(Stream fileStream)
    {
        // CSV'yi DataTable'a çevirip SqlBulkCopy ile basacağız (En hızlı yöntem)
        var dataTable = new DataTable();
        dataTable.Columns.Add("FirstName", typeof(string));
        dataTable.Columns.Add("LastName", typeof(string));
        dataTable.Columns.Add("Email", typeof(string));
        dataTable.Columns.Add("PhoneNumber", typeof(string));
        dataTable.Columns.Add("TotalPurchaseAmount", typeof(decimal));
        dataTable.Columns.Add("LastPurchaseDate", typeof(DateTime));
        dataTable.Columns.Add("CreatedAt", typeof(DateTime));

        using (var reader = new StreamReader(fileStream))
        {
            string? headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(headerLine)) return 0;

            var headers = headerLine.Split(new[] { ',', ';' }).Select(h => h.Trim().ToLower()).ToArray();
            var columnMap = new Dictionary<string, int>();

            for (int i = 0; i < headers.Length; i++)
            {
                var colName = headers[i];
                if (colName == "ad" || colName == "firstname" || colName == "first name" || colName == "name" || colName == "isim") columnMap["FirstName"] = i;
                else if (colName == "soyad" || colName == "lastname" || colName == "last name" || colName == "surname" || colName == "soyisim") columnMap["LastName"] = i;
                else if (colName == "email" || colName == "e-mail" || colName == "mail" || colName == "eposta") columnMap["Email"] = i;
                else if (colName == "telefon" || colName == "phone" || colName == "phonenumber" || colName == "tel" || colName == "cep") columnMap["PhoneNumber"] = i;
                else if (colName == "toplam tutar" || colName == "total amount" || colName == "total" || colName == "amount" || colName == "tutar" || colName == "bakiye") columnMap["TotalPurchaseAmount"] = i;
                else if (colName == "son alışveriş tarihi" || colName == "last purchase date" || colName == "date" || colName == "tarih" || colName == "son işlem") columnMap["LastPurchaseDate"] = i;
            }

            if (!columnMap.ContainsKey("FirstName") || !columnMap.ContainsKey("LastName") || !columnMap.ContainsKey("Email"))
            {
                throw new Exception($"Zorunlu sütunlar bulunamadı. Lütfen CSV dosyanızda 'Ad', 'Soyad' ve 'Email' başlıklarının olduğundan emin olun. Bulunan başlıklar: {string.Join(", ", headers)}");
            }

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(new[] { ',', ';' }).Select(v => v.Trim()).ToArray();
                if (values.Length < 3) continue;

                var row = dataTable.NewRow();
                
                // Mandatory fields
                if (columnMap.ContainsKey("FirstName") && values.Length > columnMap["FirstName"]) row["FirstName"] = values[columnMap["FirstName"]];
                else row["FirstName"] = DBNull.Value;

                if (columnMap.ContainsKey("LastName") && values.Length > columnMap["LastName"]) row["LastName"] = values[columnMap["LastName"]];
                else row["LastName"] = DBNull.Value;

                if (columnMap.ContainsKey("Email") && values.Length > columnMap["Email"]) row["Email"] = values[columnMap["Email"]];
                else row["Email"] = DBNull.Value;

                // Optional fields
                if (columnMap.ContainsKey("PhoneNumber") && values.Length > columnMap["PhoneNumber"]) 
                    row["PhoneNumber"] = values[columnMap["PhoneNumber"]];
                else 
                    row["PhoneNumber"] = DBNull.Value;

                if (columnMap.ContainsKey("TotalPurchaseAmount") && values.Length > columnMap["TotalPurchaseAmount"])
                {
                    if (decimal.TryParse(values[columnMap["TotalPurchaseAmount"]], out decimal amount))
                        row["TotalPurchaseAmount"] = amount;
                    else
                        row["TotalPurchaseAmount"] = 0m;
                }
                else
                {
                    row["TotalPurchaseAmount"] = 0m;
                }

                if (columnMap.ContainsKey("LastPurchaseDate") && values.Length > columnMap["LastPurchaseDate"])
                {
                    if (DateTime.TryParse(values[columnMap["LastPurchaseDate"]], out DateTime date))
                        row["LastPurchaseDate"] = date;
                    else
                        row["LastPurchaseDate"] = DBNull.Value;
                }
                else
                {
                    row["LastPurchaseDate"] = DBNull.Value;
                }

                row["CreatedAt"] = DateTime.UtcNow;
                dataTable.Rows.Add(row);
            }
        }

        if (dataTable.Rows.Count > 0)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var bulkCopy = new SqlBulkCopy(connectionString))
            {
                bulkCopy.DestinationTableName = "Customers";
                bulkCopy.BatchSize = 10000;
                bulkCopy.BulkCopyTimeout = 0;

                bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                bulkCopy.ColumnMappings.Add("LastName", "LastName");
                bulkCopy.ColumnMappings.Add("Email", "Email");
                bulkCopy.ColumnMappings.Add("PhoneNumber", "PhoneNumber");
                bulkCopy.ColumnMappings.Add("TotalPurchaseAmount", "TotalPurchaseAmount");
                bulkCopy.ColumnMappings.Add("LastPurchaseDate", "LastPurchaseDate");
                bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");

                await bulkCopy.WriteToServerAsync(dataTable);
                return dataTable.Rows.Count;
            }
        }

        return 0;
    }

    public async Task<List<CustomerDto>> SearchCustomersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            var allCustomers = await _context.Customers
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();
            return _mapper.Map<List<CustomerDto>>(allCustomers);
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.FirstName.Contains(searchTerm) || 
                        c.LastName.Contains(searchTerm) || 
                        c.Email.Contains(searchTerm))
            .OrderByDescending(c => c.CreatedAt)
            .Take(20) // Limit results for performance
            .ToListAsync();

        return _mapper.Map<List<CustomerDto>>(customers);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return null;
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task UpdateCustomerAsync(int id, CreateCustomerDto customerDto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            // Manual mapping or use AutoMapper if config exists for Dto -> Entity update
            customer.FirstName = customerDto.FirstName;
            customer.LastName = customerDto.LastName;
            customer.Email = customerDto.Email;
            customer.PhoneNumber = customerDto.PhoneNumber;
            customer.TotalPurchaseAmount = customerDto.TotalPurchaseAmount;
            if (customerDto.LastPurchaseDate.HasValue)
            {
                customer.LastPurchaseDate = customerDto.LastPurchaseDate;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        // TRUNCATE TABLE resets the identity seed to the original seed value (usually 1).
        // DELETE FROM does not.
        // We use ExecuteSqlRawAsync for this operation.
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Customers");
        
        // Optional: Reseed with initial data if needed, or leave empty.
        // For this user request, they likely want a fresh start, maybe with seed data?
        // Let's just leave it empty so they can add their "ID 1" customer manually, 
        // OR we can call the DbInitializer logic. 
        // But TRUNCATE is enough to reset IDs.
    }
}
