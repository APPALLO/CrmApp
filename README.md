# CRM Pro - Müşteri Yönetim Sistemi

Bu proje, yüksek performanslı ve ölçeklenebilir bir Müşteri İlişkileri Yönetimi (CRM) uygulamasıdır. Modern web teknolojileri kullanılarak geliştirilmiş olup, özellikle büyük veri setleriyle (1 milyon+ kayıt) sorunsuz çalışacak şekilde optimize edilmiştir.

## 🚀 Özellikler

### 1. Müşteri Yönetimi (CRUD)
*   Müşteri ekleme, düzenleme, silme ve listeleme.
*   Detaylı müşteri profili görüntüleme.
*   Gelişmiş arama ve filtreleme.

### 2. Yüksek Performanslı Veri İçe Aktarma (Import)
*   **Excel (.xlsx, .xls) ve CSV** desteği.
*   **Streaming (Akış) Mimarisi:** Dosyalar belleğe yüklenmeden satır satır okunur, bu sayede RAM tüketimi minimumda tutulur.
*   **SqlBulkCopy Entegrasyonu:** Veriler Entity Framework yerine doğrudan ADO.NET Bulk Copy ile veritabanına basılır. Bu sayede saniyeler içinde binlerce kayıt yüklenebilir.
*   **Akıllı Sütun Eşleştirme:** "Ad", "İsim", "Name" gibi farklı varyasyonları otomatik tanır.
*   **Hata Toleransı:** Eksik veya hatalı veriler (örn. boş tutar) otomatik olarak varsayılan değerlerle (0) doldurulur, işlem kesilmez.
*   **Canlı İlerleme Çubuğu:** Yükleme durumu anlık olarak yüzdelik dilimle gösterilir.

### 3. Gelişmiş Listeleme ve Sayfalama
*   **Sanal Sıralama (Virtual Sequencing):** Veritabanı ID'lerinden bağımsız olarak listede her zaman 1'den başlayan ardışık sıra numaraları gösterilir.
*   **Akıllı Sayfalama (Google Style):** Binlerce sayfa olsa bile kullanıcı dostu navigasyon (Örn: `1 ... 45 46 47 ... 99`).
*   **Performanslı Sorgular:** `AsNoTracking` ve veritabanı indeksleri ile optimize edilmiş okuma işlemleri.

### 4. Veritabanı ve Altyapı
*   **Code First Migrations:** Veritabanı şeması kod üzerinden yönetilir.
*   **İndeksleme Stratejisi:** `FirstName`, `LastName`, `CreatedAt` gibi sık sorgulanan alanlar için özel indeksler tanımlanmıştır.
*   **Resiliency (Dayanıklılık):** Geçici veritabanı bağlantı kopmalarında otomatik yeniden deneme (Retry Pattern) mekanizması aktiftir.
*   **Veri Kalıcılığı:** Uygulama yeniden başlatıldığında veriler korunur.

## 🛠 Teknolojiler

*   **.NET 9.0** (ASP.NET Core MVC)
*   **Entity Framework Core 9.0** (ORM)
*   **SQL Server** (LocalDB)
*   **FluentValidation** (Validasyon)
*   **ExcelDataReader** (Excel Okuma)
*   **Bootstrap 5** (UI Framework)
*   **jQuery & AJAX** (Frontend Etkileşimi)
*   **SweetAlert2** (Modern Bildirimler)

## ⚙️ Kurulum ve Çalıştırma

1.  **Gereksinimler:**
    *   .NET 9.0 SDK
    *   SQL Server (veya LocalDB)

2.  **Projeyi Klonlayın/İndirin:**
    ```bash
    git clone [repo-url]
    cd CrmApp
    ```

3.  **Veritabanını Oluşturun:**
    Terminalde proje dizinindeyken şu komutu çalıştırın:
    ```bash
    dotnet ef database update
    ```

4.  **Uygulamayı Başlatın:**
    ```bash
    dotnet run
    ```
    Tarayıcınızda `http://localhost:5228` adresine gidin.

## 🧪 Test Verisi (Stres Testi)

Sistemi test etmek için:
1.  Paneldeki **"Excel Yükle"** butonuna tıklayın.
2.  Büyük bir `.csv` veya `.xlsx` dosyası (örneğin 50.000+ satır) seçin.
3.  Yüklemeyi başlatın ve hızın keyfini çıkarın.

**Örnek CSV Formatı:**
```csv
Ad,Soyad,Email,Telefon,Toplam Tutar,Tarih
Ahmet,Yılmaz,ahmet@test.com,5551234567,1500,2024-01-01
```

## 📝 Notlar

*   Proje geliştirme ortamında (Development) `http` üzerinden çalışacak şekilde yapılandırılmıştır.
*   Veritabanı bağlantı cümlesi `appsettings.json` dosyasında `DefaultConnection` altında bulunur.
