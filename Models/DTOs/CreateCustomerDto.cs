namespace CrmApp.Models.DTOs;

public class CreateCustomerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
}
