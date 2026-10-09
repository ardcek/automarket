using AracKiralama.Web.Helpers;
using AracKiralama.Web.Models.Entities;
using AracKiralama.Web.Models.Enums;
using AracKiralama.Web.Services;
using AracKiralama.Web.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AracKiralama.Web.Data;

/// <summary>
/// Veritabanı boşsa örnek verileri oluşturur: roller, demo hesaplar, şubeler, araçlar,
/// ekstralar ve son 12 aya yayılmış kiralama geçmişi (dashboard grafikleri dolu görünsün diye).
///
/// Veritabanını sıfırlamak için: uygulamayı kapatın, "arackiralama.db" dosyasını silin, tekrar çalıştırın.
/// Rastgelelik sabit tohumla (42) yapılır → her kurulumda aynı veriler oluşur.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Rota123!";
    private static string? _demoPasswordHash;

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in new[] { Roles.Admin, Roles.Staff, Roles.Customer })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (await db.Vehicles.AnyAsync()) return;   // daha önce doldurulmuş

        var rnd = new Random(42);
        var pricing = new PricingService(Options.Create(new PricingOptions()));

        // ---------- Kullanıcılar ----------
        var admin = await CreateUserAsync(userManager, "admin@rota.com", "Selin", "Aydın", Roles.Admin, new DateTime(1985, 4, 12), rnd);
        var staff = await CreateUserAsync(userManager, "personel@rota.com", "Murat", "Kaya", Roles.Staff, new DateTime(1992, 9, 3), rnd);
        var demoCustomer = await CreateUserAsync(userManager, "musteri@rota.com", "Ayşe", "Yılmaz", Roles.Customer, new DateTime(1994, 2, 20), rnd);
        // 20 yaşında, ehliyeti 1 yıllık: SUV / lüks araç kiralayamaz → iş kuralını göstermek için.
        await CreateUserAsync(userManager, "genc@rota.com", "Emre", "Doğan", Roles.Customer, DateTime.Today.AddYears(-20).AddDays(-30), rnd, licenseAfterYears: 19);

        var customers = new List<AppUser> { demoCustomer };
        foreach (var (first, last) in CustomerNames)
        {
            var email = $"{Ascii(first)}.{Ascii(last)}@example.com";
            customers.Add(await CreateUserAsync(userManager, email, first, last, Roles.Customer,
                new DateTime(rnd.Next(1965, 2001), rnd.Next(1, 13), rnd.Next(1, 28)), rnd));
        }

        // ---------- Şubeler, markalar, ekstralar ----------
        var branches = new List<Branch>
        {
            new() { Name = "İstanbul Havalimanı", City = "İstanbul", Address = "Tayakadın Mah. Terminal Cad. No:1, Arnavutköy", Phone = "0212 555 10 10" },
            new() { Name = "Kadıköy", City = "İstanbul", Address = "Caferağa Mah. Moda Cad. No:42, Kadıköy", Phone = "0216 555 20 20" },
            new() { Name = "Ankara Esenboğa", City = "Ankara", Address = "Esenboğa Havalimanı Dış Hatlar, Çubuk", Phone = "0312 555 30 30" },
            new() { Name = "İzmir Adnan Menderes", City = "İzmir", Address = "Dokuz Eylül Mah. Havalimanı Cad., Gaziemir", Phone = "0232 555 40 40" },
            new() { Name = "Antalya Havalimanı", City = "Antalya", Address = "Yenigöl Mah. Havalimanı Yolu, Muratpaşa", Phone = "0242 555 50 50" },
        };
        db.Branches.AddRange(branches);

        var brands = new[] { "Audi", "Fiat", "Ford", "Hyundai", "Peugeot", "Renault", "Volkswagen" }
            .Select(n => new Brand { Name = n, LogoUrl = $"/img/brands/{n.ToLowerInvariant()}.png" })
            .ToDictionary(b => b.Name);
        db.Brands.AddRange(brands.Values);

        var extras = new List<Extra>
        {
            new() { Name = "Tam Kasko", Icon = "bi-shield-check", DailyPrice = 350, Description = "Hasar durumunda muafiyet bedeli ödemezsiniz." },
            new() { Name = "Ek Sürücü", Icon = "bi-person-plus", DailyPrice = 150, Description = "Araç ikinci bir sürücü tarafından da kullanılabilir." },
            new() { Name = "Bebek Koltuğu", Icon = "bi-emoji-smile", DailyPrice = 120, Description = "0-4 yaş için ISOFIX bağlantılı koltuk." },
            new() { Name = "Navigasyon", Icon = "bi-geo-alt", DailyPrice = 90, Description = "Güncel haritalı GPS cihazı." },
            new() { Name = "Mobil Wi-Fi", Icon = "bi-wifi", DailyPrice = 110, Description = "Sınırsız 4.5G internet paylaşımı." },
            new() { Name = "Kar Zinciri", Icon = "bi-snow", DailyPrice = 80, Description = "Kış şartları için." },
        };
        db.Extras.AddRange(extras);

        // ---------- Araçlar (eski projedeki görsellerden) ----------
        var vehicles = new List<Vehicle>();
        var usedPlates = new HashSet<string>();
        foreach (var spec in VehicleSpecs)
        {
            var branch = branches[rnd.Next(branches.Count)];
            var year = rnd.Next(spec.MinYear, 2026);
            var vehicle = new Vehicle
            {
                Brand = brands[spec.Brand],
                Model = spec.Model,
                Year = year,
                Category = spec.Category,
                Fuel = spec.Fuel,
                Transmission = spec.Transmission,
                Seats = spec.Seats,
                Color = Colors[rnd.Next(Colors.Length)],
                Kilometers = (2026 - year) * rnd.Next(12_000, 22_000) + rnd.Next(1_000, 9_000),
                DailyPrice = spec.DailyPrice,
                Deposit = DepositFor(spec.Category),
                MinDriverAge = MinAgeFor(spec.Category),
                MinLicenseYears = MinLicenseFor(spec.Category),
                Branch = branch,
                Plate = UniquePlate(branch.City, rnd, usedPlates),
                ImageUrl = $"/img/cars/{spec.Brand}_{spec.Model.Replace(' ', '_')}.jpg",
                Description = DescriptionFor(spec),
                CreatedAt = DateTime.Now.AddYears(-1),
            };
            vehicles.Add(vehicle);
        }
        db.Vehicles.AddRange(vehicles);
        await db.SaveChangesAsync();

        // ---------- Kiralama geçmişi ----------
        var now = DateTime.Now;
        var today = DateTime.Today;
        var reservations = new List<Reservation>();

        Reservation Make(Vehicle v, AppUser c, DateTime start, DateTime end, ReservationStatus status)
        {
            var pickup = v.Branch!;
            var drop = rnd.NextDouble() < 0.15 ? branches[rnd.Next(branches.Count)] : pickup;
            var chosenExtras = extras.OrderBy(_ => rnd.Next()).Take(rnd.Next(0, 3)).ToList();
            var r = new Reservation
            {
                Customer = c, Vehicle = v, PickupBranch = pickup, ReturnBranch = drop,
                StartDate = start, EndDate = end, Status = status,
                CreatedAt = start.AddDays(-rnd.Next(1, 15)).AddHours(-rnd.Next(0, 10)),
                HandledById = status == ReservationStatus.Pending ? null : staff.Id,
            };
            if (r.CreatedAt > now) r.CreatedAt = now.AddHours(-rnd.Next(1, 48));
            ReservationService.ApplyQuote(r, pricing.Calculate(v, start, end, chosenExtras, pickup.Id != drop.Id));
            reservations.Add(r);
            return r;
        }

        AppUser RandomCustomer() => customers[rnd.Next(customers.Count)];

        for (var i = 0; i < vehicles.Count; i++)
        {
            var v = vehicles[i];
            // İlk 6 araç için "bugün" senaryoları (aşağıda) yer açmak amacıyla geçmiş daha erken biter.
            var historyUntil = i < 6 ? now.AddDays(-15) : now.AddDays(45);
            var cursor = today.AddDays(-340 + rnd.Next(0, 25));

            while (true)
            {
                var start = cursor.AddDays(rnd.Next(1, 10)).Date.AddHours(rnd.Next(8, 19));
                var length = rnd.NextDouble() switch { < 0.05 => 30, < 0.15 => rnd.Next(7, 15), _ => rnd.Next(1, 7) };
                var end = start.AddDays(length).AddHours(rnd.Next(-2, 3));
                if (end > historyUntil) break;
                cursor = end;

                if (end < now)
                {
                    var roll = rnd.NextDouble();
                    var status = roll < 0.05 ? ReservationStatus.Cancelled : roll < 0.09 ? ReservationStatus.Rejected : ReservationStatus.Completed;
                    var r = Make(v, RandomCustomer(), start, end, status);
                    if (status == ReservationStatus.Completed) CompleteHistorical(r, v, pricing, rnd, staff);
                    else r.StatusReason = status == ReservationStatus.Cancelled ? "Müşteri tarafından iptal edildi." : "Talep uygun bulunmadı.";
                }
                else if (start <= now)
                {
                    var r = Make(v, RandomCustomer(), start, end, ReservationStatus.Active);
                    Deliver(r, v);
                }
                else
                {
                    Make(v, RandomCustomer(), start, end, rnd.NextDouble() < 0.7 ? ReservationStatus.Approved : ReservationStatus.Pending);
                }
            }
        }

        // ---------- "Bugün" senaryoları (dashboard'daki "Bugünün İşleri" dolu görünsün) ----------
        // 0-1: iade tarihi geçmiş, hâlâ müşteride → Gecikmiş
        foreach (var v in vehicles.Take(2))
        {
            var r = Make(v, RandomCustomer(), now.AddDays(-6).Date.AddHours(10), now.AddDays(-1).Date.AddHours(10), ReservationStatus.Active);
            Deliver(r, v);
            r.IsOverdue = true;
        }
        // 2-3: bugün teslim edilecek (onaylı)
        foreach (var v in vehicles.Skip(2).Take(2))
            Make(v, RandomCustomer(), now.AddHours(2), now.AddDays(4).AddHours(2), ReservationStatus.Approved);
        // 4-5: bugün iade alınacak (kirada)
        foreach (var v in vehicles.Skip(4).Take(2))
        {
            var r = Make(v, RandomCustomer(), now.AddDays(-3), now.AddHours(3), ReservationStatus.Active);
            Deliver(r, v);
        }
        // Onay bekleyen birkaç yeni talep
        foreach (var v in vehicles.Skip(6).OrderBy(_ => rnd.Next()).Take(4))
        {
            var start = today.AddDays(rnd.Next(60, 80)).AddHours(10);
            Make(v, RandomCustomer(), start, start.AddDays(rnd.Next(2, 6)), ReservationStatus.Pending);
        }

        // Demo müşterinin her durumdan en az bir rezervasyonu olsun.
        var forDemo = new[] { ReservationStatus.Completed, ReservationStatus.Approved, ReservationStatus.Pending };
        foreach (var s in forDemo)
        {
            var r = reservations.Where(x => x.Status == s && x.Customer != demoCustomer).OrderByDescending(x => x.StartDate).Skip(2).FirstOrDefault();
            if (r is not null) r.Customer = demoCustomer;
        }

        // Kodlar oluşturulma sırasına göre: RZ-2025-0001, RZ-2025-0002 ...
        foreach (var group in reservations.OrderBy(r => r.CreatedAt).GroupBy(r => r.CreatedAt.Year))
        {
            var n = 1;
            foreach (var r in group) r.Code = $"RZ-{group.Key}-{n++:D4}";
        }
        db.Reservations.AddRange(reservations);

        // ---------- Bakım kayıtları ----------
        // Şu an bakımda olan araç: önümüzdeki günlerde kiralaması olmayan ilk müsait araç seçilir.
        var inService = vehicles.Skip(6).First(v => v.Status == VehicleStatus.Available
            && !reservations.Any(r => r.Vehicle == v && AvailabilityService.BlockingStatuses.Contains(r.Status)
                                      && AvailabilityService.Overlaps(r.StartDate, r.EndDate, today.AddDays(-1), today.AddDays(3))));
        inService.Status = VehicleStatus.Maintenance;
        db.Maintenances.Add(new Maintenance
        {
            Vehicle = inService, Type = MaintenanceType.Repair, StartDate = today.AddDays(-1), EndDate = today.AddDays(2).AddHours(18),
            Kilometers = inService.Kilometers, Cost = 8_500, Description = "Ön fren balatası ve disk değişimi.",
        });
        foreach (var v in vehicles.OrderBy(_ => rnd.Next()).Take(18))
        {
            var start = today.AddDays(-rnd.Next(20, 330)).AddHours(9);
            var type = (MaintenanceType)rnd.Next(0, 5);
            db.Maintenances.Add(new Maintenance
            {
                Vehicle = v, Type = type, StartDate = start, EndDate = start.AddDays(rnd.Next(1, 3)),
                Kilometers = Math.Max(0, v.Kilometers - rnd.Next(2_000, 15_000)),
                Cost = type switch { MaintenanceType.Periodic => rnd.Next(4, 9) * 1000, MaintenanceType.Tire => rnd.Next(8, 16) * 1000, MaintenanceType.Cleaning => 750, _ => rnd.Next(2, 12) * 1000 },
                Description = type.GetDisplayName(),
                IsCompleted = true,
            });
        }

        await db.SaveChangesAsync();
    }

    // ===================== Yardımcılar =====================

    private static void Deliver(Reservation r, Vehicle v)
    {
        r.DeliveredAt = r.StartDate;
        r.StartKm = v.Kilometers;
        r.FuelLevelOut = FuelLevel.Full;
        v.Status = VehicleStatus.Rented;
    }

    /// <summary>Geçmiş bir kiralamayı iade edilmiş hale getirir (bazılarında gecikme / yakıt / hasar ücreti).</summary>
    private static void CompleteHistorical(Reservation r, Vehicle v, PricingService pricing, Random rnd, AppUser staff)
    {
        var km = rnd.Next(80, 450) * r.TotalDays;
        r.DeliveredAt = r.StartDate;
        r.StartKm = Math.Max(0, v.Kilometers - km - rnd.Next(500, 3000));
        r.EndKm = r.StartKm + km;
        r.FuelLevelOut = FuelLevel.Full;
        r.FuelLevelIn = rnd.NextDouble() < 0.12 ? FuelLevel.Half : FuelLevel.Full;
        r.ActualReturnDate = rnd.NextDouble() < 0.1 ? r.EndDate.AddHours(rnd.Next(2, 30)) : r.EndDate.AddMinutes(-rnd.Next(0, 90));
        r.LateFee = pricing.CalculateLateFee(r.DailyPrice, r.EndDate, r.ActualReturnDate.Value);
        r.FuelFee = pricing.CalculateFuelFee(r.FuelLevelOut.Value, r.FuelLevelIn.Value);
        if (rnd.NextDouble() < 0.04)
        {
            r.DamageFee = rnd.Next(2, 10) * 500;
            r.DamageReports.Add(new DamageReport { Description = "Arka tamponda çizik.", Cost = r.DamageFee, CreatedAt = r.ActualReturnDate.Value });
        }
        ReservationService.RecalculateTotal(r);

        r.Payments.Add(new Payment
        {
            Amount = r.TotalPrice, Type = PaymentType.Rental,
            Method = (PaymentMethod)rnd.Next(0, 3), PaidAt = r.ActualReturnDate.Value, ReceivedById = staff.Id,
        });
    }

    private static async Task<AppUser> CreateUserAsync(UserManager<AppUser> um, string email, string first, string last,
        string role, DateTime birthDate, Random rnd, int licenseAfterYears = 0)
    {
        var licenseAge = licenseAfterYears > 0 ? licenseAfterYears : rnd.Next(18, 24);
        var user = new AppUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            FirstName = first, LastName = last,
            PhoneNumber = $"05{rnd.Next(30, 56)} {rnd.Next(100, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}",
            TcNo = TcKimlikNo.Generate(rnd),
            BirthDate = birthDate,
            LicenseNumber = rnd.Next(100000, 999999).ToString(),
            LicenseIssueDate = birthDate.AddYears(licenseAge).AddDays(rnd.Next(0, 200)),
            CreatedAt = DateTime.Now.AddDays(-rnd.Next(30, 400)),
        };
        // Şifre hash'leme (PBKDF2) bilerek yavaş bir işlemdir. Bütün demo hesapların şifresi aynı olduğu için
        // bir kez hash'leyip hepsine veriyoruz; zayıf sunucularda açılışı ciddi hızlandırır.
        // (Gerçek kullanıcılar kayıt olurken her şifre ayrı ve tuzlu olarak hash'lenir.)
        user.PasswordHash = _demoPasswordHash ??= um.PasswordHasher.HashPassword(user, DemoPassword);
        var result = await um.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException("Seed kullanıcısı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        await um.AddToRoleAsync(user, role);
        return user;
    }

    private static string UniquePlate(string city, Random rnd, HashSet<string> used)
    {
        var code = city switch { "İstanbul" => "34", "Ankara" => "06", "İzmir" => "35", "Antalya" => "07", _ => "34" };
        const string letters = "ABCDEFGHJKLMNPRSTUVYZ";
        while (true)
        {
            var l = new string(Enumerable.Range(0, rnd.Next(2, 4)).Select(_ => letters[rnd.Next(letters.Length)]).ToArray());
            var plate = $"{code} {l} {rnd.Next(100, 1000)}";
            if (used.Add(plate)) return plate;
        }
    }

    /// <summary>"İrem Şahin" → "irem.sahin" (Identity kullanıcı adında Türkçe karakter kabul etmez).</summary>
    private static string Ascii(string s)
    {
        var map = new Dictionary<char, char>
        {
            ['ç'] = 'c', ['Ç'] = 'c', ['ğ'] = 'g', ['Ğ'] = 'g', ['ı'] = 'i', ['İ'] = 'i',
            ['ö'] = 'o', ['Ö'] = 'o', ['ş'] = 's', ['Ş'] = 's', ['ü'] = 'u', ['Ü'] = 'u', [' '] = '.',
        };
        return new string(s.Select(c => map.TryGetValue(c, out var a) ? a : char.ToLowerInvariant(c)).ToArray());
    }

    private static decimal DepositFor(VehicleCategory c) => c switch
    {
        VehicleCategory.Economy => 3_000, VehicleCategory.Midsize => 5_000, VehicleCategory.Suv => 7_500,
        VehicleCategory.Luxury => 12_000, _ => 4_000,
    };

    private static int MinAgeFor(VehicleCategory c) => c switch
    {
        VehicleCategory.Luxury => 27, VehicleCategory.Suv or VehicleCategory.Commercial => 23, _ => 21,
    };

    private static int MinLicenseFor(VehicleCategory c) => c switch
    {
        VehicleCategory.Luxury => 5, VehicleCategory.Suv => 3, VehicleCategory.Midsize or VehicleCategory.Commercial => 2, _ => 1,
    };

    private static string DescriptionFor(VehicleSpec s) => s.Category switch
    {
        VehicleCategory.Economy => $"{s.Brand} {s.Model}; düşük yakıt tüketimi ve kolay park imkânıyla şehir içi kullanım için ideal.",
        VehicleCategory.Midsize => $"{s.Brand} {s.Model}; geniş bagajı ve konforlu sürüşüyle uzun yollar için güvenilir bir tercih.",
        VehicleCategory.Suv => $"{s.Brand} {s.Model}; yüksek sürüş pozisyonu ve geniş iç hacmiyle aile tatilleri için mükemmel.",
        VehicleCategory.Luxury => $"{s.Brand} {s.Model}; premium donanımı ve üst düzey konforuyla iş seyahatlerinin vazgeçilmezi.",
        _ => $"{s.Brand} {s.Model}; yüksek taşıma kapasitesiyle ticari ihtiyaçlarınız ve kalabalık gruplar için.",
    };

    private static readonly string[] Colors = ["Beyaz", "Siyah", "Gri", "Gümüş", "Lacivert", "Kırmızı", "Mavi"];

    private static readonly (string, string)[] CustomerNames =
    [
        ("Mehmet", "Demir"), ("Zeynep", "Çelik"), ("Ahmet", "Şahin"), ("Elif", "Yıldız"), ("Mustafa", "Öztürk"),
        ("Fatma", "Arslan"), ("Can", "Koç"), ("Deniz", "Kurt"), ("Burak", "Özdemir"), ("Ece", "Aslan"),
        ("Hakan", "Polat"), ("Gizem", "Erdoğan"), ("Oğuz", "Güneş"), ("Merve", "Aksoy"), ("Kerem", "Tekin"),
        ("Seda", "Kılıç"), ("Tolga", "Bulut"), ("İrem", "Karaca"), ("Serkan", "Yavuz"), ("Buse", "Ünal"),
    ];

    private record VehicleSpec(string Brand, string Model, VehicleCategory Category, int Seats, FuelType Fuel,
        TransmissionType Transmission, decimal DailyPrice, int MinYear = 2019);

    private static readonly VehicleSpec[] VehicleSpecs =
    [
        new("Audi", "A3", VehicleCategory.Midsize, 5, FuelType.Gasoline, TransmissionType.Automatic, 1950),
        new("Audi", "A4", VehicleCategory.Luxury, 5, FuelType.Diesel, TransmissionType.Automatic, 2650),
        new("Audi", "A5", VehicleCategory.Luxury, 4, FuelType.Gasoline, TransmissionType.Automatic, 3100),
        new("Audi", "A6", VehicleCategory.Luxury, 5, FuelType.Diesel, TransmissionType.Automatic, 3650),
        new("Audi", "A7", VehicleCategory.Luxury, 4, FuelType.Gasoline, TransmissionType.Automatic, 4500),
        new("Audi", "Q3", VehicleCategory.Suv, 5, FuelType.Gasoline, TransmissionType.Automatic, 2700),
        new("Audi", "Q5", VehicleCategory.Suv, 5, FuelType.Diesel, TransmissionType.Automatic, 3300),
        new("Audi", "Q7", VehicleCategory.Luxury, 7, FuelType.Diesel, TransmissionType.Automatic, 4800),
        new("Fiat", "500L", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 1150),
        new("Fiat", "Doblo", VehicleCategory.Commercial, 5, FuelType.Diesel, TransmissionType.Manual, 1300),
        new("Fiat", "Egea", VehicleCategory.Economy, 5, FuelType.Diesel, TransmissionType.Manual, 1050),
        new("Fiat", "Fiorino", VehicleCategory.Commercial, 2, FuelType.Diesel, TransmissionType.Manual, 1100),
        new("Fiat", "Linea", VehicleCategory.Economy, 5, FuelType.Lpg, TransmissionType.Manual, 900, 2016),
        new("Fiat", "Palio", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 800, 2014),
        new("Ford", "Fiesta", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 1100),
        new("Ford", "Focus", VehicleCategory.Midsize, 5, FuelType.Diesel, TransmissionType.Automatic, 1450),
        new("Ford", "Mondeo", VehicleCategory.Midsize, 5, FuelType.Hybrid, TransmissionType.Automatic, 1850),
        new("Ford", "Tourneo Courier", VehicleCategory.Commercial, 5, FuelType.Diesel, TransmissionType.Manual, 1350),
        new("Ford", "Transit", VehicleCategory.Commercial, 9, FuelType.Diesel, TransmissionType.Manual, 2300),
        new("Hyundai", "Accent", VehicleCategory.Economy, 5, FuelType.Lpg, TransmissionType.Manual, 900, 2016),
        new("Hyundai", "Accent Blue", VehicleCategory.Economy, 5, FuelType.Diesel, TransmissionType.Automatic, 1000),
        new("Hyundai", "Bayon", VehicleCategory.Suv, 5, FuelType.Gasoline, TransmissionType.Automatic, 1550),
        new("Hyundai", "Tucson", VehicleCategory.Suv, 5, FuelType.Hybrid, TransmissionType.Automatic, 2350),
        new("Hyundai", "i20", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Automatic, 1150),
        new("Hyundai", "i20N", VehicleCategory.Midsize, 5, FuelType.Gasoline, TransmissionType.Manual, 2200),
        new("Hyundai", "i30", VehicleCategory.Midsize, 5, FuelType.Gasoline, TransmissionType.Automatic, 1350),
        new("Peugeot", "206", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 750, 2010),
        new("Peugeot", "208", VehicleCategory.Economy, 5, FuelType.Electric, TransmissionType.Automatic, 1300),
        new("Peugeot", "3008", VehicleCategory.Suv, 5, FuelType.Diesel, TransmissionType.Automatic, 2250),
        new("Peugeot", "301", VehicleCategory.Economy, 5, FuelType.Diesel, TransmissionType.Manual, 1000),
        new("Peugeot", "307", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 850, 2012),
        new("Peugeot", "308", VehicleCategory.Midsize, 5, FuelType.Gasoline, TransmissionType.Automatic, 1450),
        new("Peugeot", "5008", VehicleCategory.Suv, 7, FuelType.Diesel, TransmissionType.Automatic, 2650),
        new("Renault", "Broadway", VehicleCategory.Economy, 5, FuelType.Lpg, TransmissionType.Manual, 700, 2008),
        new("Renault", "Clio", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 1100),
        new("Renault", "Express", VehicleCategory.Commercial, 5, FuelType.Diesel, TransmissionType.Manual, 1150),
        new("Renault", "Megane", VehicleCategory.Midsize, 5, FuelType.Diesel, TransmissionType.Automatic, 1400),
        new("Renault", "Symbol", VehicleCategory.Economy, 5, FuelType.Diesel, TransmissionType.Manual, 950),
        new("Renault", "Talisman", VehicleCategory.Midsize, 5, FuelType.Diesel, TransmissionType.Automatic, 1900),
        new("Volkswagen", "Arteon", VehicleCategory.Luxury, 5, FuelType.Gasoline, TransmissionType.Automatic, 3200),
        new("Volkswagen", "Caddy", VehicleCategory.Commercial, 5, FuelType.Diesel, TransmissionType.Manual, 1400),
        new("Volkswagen", "Golf", VehicleCategory.Midsize, 5, FuelType.Gasoline, TransmissionType.Automatic, 1600),
        new("Volkswagen", "Jetta", VehicleCategory.Midsize, 5, FuelType.Gasoline, TransmissionType.Automatic, 1550),
        new("Volkswagen", "Passat", VehicleCategory.Midsize, 5, FuelType.Diesel, TransmissionType.Automatic, 2100),
        new("Volkswagen", "Polo", VehicleCategory.Economy, 5, FuelType.Gasoline, TransmissionType.Manual, 1250),
        new("Volkswagen", "Tiguan", VehicleCategory.Suv, 5, FuelType.Diesel, TransmissionType.Automatic, 2500),
        new("Volkswagen", "Transporter", VehicleCategory.Commercial, 9, FuelType.Diesel, TransmissionType.Manual, 2650),
    ];
}
