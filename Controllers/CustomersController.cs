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

    [HttpGet]
    public IActionResult ImportExcel()
    {
        return View();
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
    public async Task<IActionResult> ImportExcel(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            var msg = "Lütfen geçerli bir dosya seçin.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = msg });
            
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            int count = 0;
            
            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            
            if (extension == ".csv")
            {
                count = await _customerService.ImportCustomersFromCsvAsync(stream);
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                count = await _customerService.ImportCustomersFromExcelAsync(stream);
            }
            else
            {
                var msg = "Desteklenmeyen dosya formatı. Lütfen .xlsx, .xls veya .csv yükleyin.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = msg });
                
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Index));
            }

            var successMsg = $"{count} müşteri başarıyla aktarıldı.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = successMsg, count = count });

            TempData["Success"] = successMsg;
        }
        catch (Exception ex)
        {
            var errorMsg = "Dosya yüklenirken hata oluştu: " + ex.Message;
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, message = errorMsg });

            TempData["Error"] = errorMsg;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Index(int page = 1, string? searchTerm = null)
    {
        var result = await _customerService.GetCustomersAsync(page, 20, searchTerm); // Sayfa başı 20 kayıt
        ViewBag.SearchTerm = searchTerm;
        return View(result);
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
    
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        // Map to CreateCustomerDto because that's what we use for forms/validation
        // Ideally we should have an UpdateCustomerDto but for simplicity we reuse or map
        var model = new CreateCustomerDto
        {
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            TotalPurchaseAmount = customer.TotalPurchaseAmount,
            LastPurchaseDate = customer.LastPurchaseDate
        };
        
        // Pass ID via ViewBag or use a specific ViewModel
        ViewBag.CustomerId = customer.Id;
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateCustomerDto customerDto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CustomerId = id;
            return View(customerDto);
        }

        await _customerService.UpdateCustomerAsync(id, customerDto);
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

    [HttpPost]
    public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
    {
        try
        {
            await _customerService.BulkDeleteCustomersAsync(ids);
            return Json(new { success = true, message = $"{ids.Count} müşteri başarıyla silindi." });
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

    [HttpPost]
    public async Task<IActionResult> ResetDatabase()
    {
        try
        {
            await _customerService.ResetDatabaseAsync();
            TempData["Success"] = "Veritabanı sıfırlandı ve ID'ler resetlendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Sıfırlama hatası: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
