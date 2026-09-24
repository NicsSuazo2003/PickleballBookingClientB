// Data/Seeders/DbSeeder.cs
using PickleballBookingSystem.Entities;

namespace PickleballBookingSystem.Data;

public static class DbSeeder
{
    public static void Initialize(AppDbContext db)
    {
        // ✅ PaddlePlace seed — only runs if the DB is completely empty
        if (!db.Clients.Any())
        {
            var client = new Client
            {
                Id = Guid.NewGuid(),
                Name = "PaddlePlace",
                Subdomain = "paddleplace",
                LogoUrl = null,
                PrimaryColor = "#1A2E1A",     // ← replace with PaddlePlace's brand color
                AccentColor = "#C9A94E",      // ← replace with PaddlePlace's accent
                GcashNumber = "0917 000 0000",   // ← replace with PaddlePlace's GCash
                GcashAccountName = "PaddlePlace", // ← replace
                CreatedAt = DateTime.UtcNow,
                Status = "active"
            };

            db.Clients.Add(client);
            db.SaveChanges();

            var clientId = client.Id;

            // ✅ Seed courts
            if (!db.Courts.Any())
            {
                var courts = new List<Court>
                {
                    new Court
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Name = "Court 1",
                        Type = "indoor",
                        Indoor = true,
                        PricePerHour = 350,      // ← change for PaddlePlace
                        PeakPricePerHour = 450,  // ← change
                        Description = "Indoor court",
                        ImageUrl = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48",
                        Images = new List<string> { "https://images.unsplash.com/photo-1534438327276-14e5300c3a48" },
                        Amenities = new List<string> { "WiFi", "Lighting" },
                        Rating = 4.8,
                        Status = "active",
                        OpenTime = new TimeOnly(8, 0),
                        CloseTime = new TimeOnly(22, 0),
                        Dimensions = "44ft x 20ft",
                        Surface = "Cushion"
                    },
                    new Court
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Name = "Court 2",
                        Type = "indoor",
                        Indoor = true,
                        PricePerHour = 350,
                        PeakPricePerHour = 450,
                        Description = "Indoor court",
                        ImageUrl = "https://images.unsplash.com/photo-1622163642998-1ea32b0bbc67",
                        Images = new List<string> { "https://images.unsplash.com/photo-1622163642998-1ea32b0bbc67" },
                        Amenities = new List<string> { "WiFi", "Lighting" },
                        Rating = 4.5,
                        Status = "active",
                        OpenTime = new TimeOnly(8, 0),
                        CloseTime = new TimeOnly(22, 0),
                        Dimensions = "44ft x 20ft",
                        Surface = "Cushion"
                    }
                };

                db.Courts.AddRange(courts);
                db.SaveChanges();
            }

            // ✅ Seed admin user
            if (!db.Users.Any())
            {
                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    Email = "admin@paddleplace.ph",     // ← change to PaddlePlace's admin email
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"), // ← change
                    Name = "PaddlePlace Admin",
                    Phone = "",
                    Role = "admin",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(admin);
                db.SaveChanges();
            }
        }
    }
}