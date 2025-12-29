using CrmApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmApp.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Migrations kullanarak veritabanını güncelle
        // Bu komut veritabanı yoksa oluşturur, varsa eksik migrationları uygular.
        context.Database.Migrate();

        // Kullanıcı isteği üzerine otomatik veri ekleme (seeding) kapatıldı.
        // Artık proje başladığında mevcut veriler korunacak, veritabanı boşsa boş kalacak.
    }
}
