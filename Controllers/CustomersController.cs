using CrmApp.Models.DTOs;
using CrmApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmApp.Controllers;

public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Lütfen geçerli bir Excel dosyası seçin.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            int count = await _customerService.ImportCustomersFromExcelAsync(stream);
            TempData["Success"] = $"{count} müşteri başarıyla aktarıldı.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Dosya yüklenirken hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _customerService.GetCustomersAsync();
        return View(customers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto customerDto)
    {
        if (!ModelState.IsValid)
        {
            return View(customerDto);
        }

        await _customerService.AddCustomerAsync(customerDto);
        return RedirectToAction(nameof(Index));
    }
    
    // The "Advanced Query" logic encapsulated
    public async Task<IActionResult> ActiveCustomers()
    {
        var activeCustomers = await _customerService.GetActiveCustomersAsync(30);
        return View("Index", activeCustomers);
    }

    // Bulk Insert Simulation
    [HttpPost]
    public async Task<IActionResult> BulkInsert()
    {
        // Dummy data generation
        var dummyCustomers = new List<CreateCustomerDto>();
        for (int i = 0; i < 1000; i++)
        {
            dummyCustomers.Add(new CreateCustomerDto
            {
                FirstName = $"User{i}",
                LastName = $"Test{i}",
                Email = $"user{i}_{Guid.NewGuid()}@example.com",
                TotalPurchaseAmount = i * 10,
                LastPurchaseDate = DateTime.UtcNow.AddDays(-i % 60) // Mix of active and inactive
            });
        }

        await _customerService.BulkInsertCustomersAsync(dummyCustomers);
        return RedirectToAction(nameof(Index));
    }

    // API Endpoints for AJAX
    [HttpGet]
    public async Task<IActionResult> GetSalesData()
    {
        var data = await _customerService.GetLast7DaysSalesAsync();
        return Json(data);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _customerService.DeleteCustomerAsync(id);
            return Json(new { success = true, message = "Müşteri başarıyla silindi." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Silme işlemi sırasında hata oluştu: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        var results = await _customerService.SearchCustomersAsync(term);
        return Json(results);
    }
}
