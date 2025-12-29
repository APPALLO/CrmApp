using CrmApp.Models.DTOs;
using CrmApp.Models.Common;

namespace CrmApp.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 20);
    Task AddCustomerAsync(CreateCustomerDto customerDto);
    Task BulkInsertCustomersAsync(List<CreateCustomerDto> customerDtos);
    Task<List<CustomerDto>> GetActiveCustomersAsync(int days);
    Task<List<SalesStatsDto>> GetLast7DaysSalesAsync();
    Task<int> ImportCustomersFromExcelAsync(Stream fileStream);
    Task<int> ImportCustomersFromCsvAsync(Stream fileStream);
    Task DeleteCustomerAsync(int id);
    Task BulkDeleteCustomersAsync(List<int> ids);
    Task<List<CustomerDto>> SearchCustomersAsync(string searchTerm);
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task UpdateCustomerAsync(int id, CreateCustomerDto customerDto);
    Task ResetDatabaseAsync();
}
