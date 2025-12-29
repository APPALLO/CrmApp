using CrmApp.Data;
using CrmApp.Services;
using CrmApp.Mapping;
using CrmApp.Middlewares;
using CrmApp.Validations;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Dosya yükleme limitlerini artır (Büyük Excel/CSV dosyaları için)
builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue; // 4GB'a kadar izin ver (teorik)
    x.MemoryBufferThreshold = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // Sınırsız (veya ihtiyaca göre örn: 500MB)
});

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        // Bağlantı koparsa otomatik tekrar dene (Resiliency)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

builder.Services.AddScoped<ICustomerService, CustomerService>();

// AutoMapper
builder.Services.AddAutoMapper(config => config.AddProfile<MappingProfile>());

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerDtoValidator>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

// Register encoding provider for ExcelDataReader
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// Global Exception Middleware - En başa!
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Home/Error"); // Kendi middleware'imiz var artık
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Customers}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
