using AutoMapper;
using CrmApp.Data;
using CrmApp.Models;
using CrmApp.Models.DTOs;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CrmApp.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public CustomerService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync()
    {
        var customers = await _context.Customers.ToListAsync();
        return _mapper.Map<List<CustomerDto>>(customers);
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

    public async Task<int> ImportCustomersFromExcelAsync(Stream fileStream)
    {
        var importedCustomers = new List<Customer>();

        using (var reader = ExcelReaderFactory.CreateReader(fileStream))
        {
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            var dataTable = result.Tables[0];

            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    // Basic validation: Check if mandatory fields are present
                    if (row["Ad"] == DBNull.Value || row["Soyad"] == DBNull.Value || row["Email"] == DBNull.Value)
                        continue;

                    var customer = new Customer
                    {
                        FirstName = row["Ad"].ToString()!,
                        LastName = row["Soyad"].ToString()!,
                        Email = row["Email"].ToString()!,
                        PhoneNumber = row.Table.Columns.Contains("Telefon") && row["Telefon"] != DBNull.Value ? row["Telefon"].ToString() : null,
                        TotalPurchaseAmount = row.Table.Columns.Contains("Toplam Tutar") && row["Toplam Tutar"] != DBNull.Value ? Convert.ToDecimal(row["Toplam Tutar"]) : 0,
                        LastPurchaseDate = row.Table.Columns.Contains("Son Alışveriş Tarihi") && row["Son Alışveriş Tarihi"] != DBNull.Value 
                            ? Convert.ToDateTime(row["Son Alışveriş Tarihi"]) 
                            : null
                    };

                    importedCustomers.Add(customer);
                }
                catch
                {
                    // Skip rows with errors (or handle them as needed)
                    continue;
                }
            }
        }

        if (importedCustomers.Any())
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Customers.AddRangeAsync(importedCustomers);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return importedCustomers.Count;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return 0;
    }

    public async Task<List<CustomerDto>> SearchCustomersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<CustomerDto>();
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
}
