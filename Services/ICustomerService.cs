using CrmApp.Models.DTOs;

namespace CrmApp.Services;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetCustomersAsync();
    Task AddCustomerAsync(CreateCustomerDto customerDto);
    Task BulkInsertCustomersAsync(List<CreateCustomerDto> customerDtos);
    Task<List<CustomerDto>> GetActiveCustomersAsync(int days);
    Task<List<SalesStatsDto>> GetLast7DaysSalesAsync();
    Task<int> ImportCustomersFromExcelAsync(Stream fileStream);
    Task DeleteCustomerAsync(int id);
    Task<List<CustomerDto>> SearchCustomersAsync(string searchTerm);
}
