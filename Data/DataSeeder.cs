using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Data
{
    /// <summary>
    /// Seeder de données démo : Agents, Commissions, Currencies, Reviews
    /// Appelé dans Program.cs après RoleSeeder et UserSeeder
    /// </summary>
    public class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            await SeedCurrenciesAsync(context);
            await SeedCommissionsAsync(context);
            await SeedAgentsAsync(context, userManager);
            await SeedReviewsAsync(context, userManager);
        }

        // ─────────────────────────────────────────────────────────
        // CURRENCIES — taux réalistes avril 2026
        // ─────────────────────────────────────────────────────────
        private static async Task SeedCurrenciesAsync(ApplicationDbContext db)
        {
            if (await db.Currencies.AnyAsync()) return;

            db.Currencies.AddRange(
                new Currency { Code = "USD", Name = "US Dollar", Symbol = "$", ExchangeRate = 1m, IsActive = true, FlagEmoji = "🇺🇸", LastUpdated = DateTime.Now },
                new Currency { Code = "EUR", Name = "Euro", Symbol = "€", ExchangeRate = 0.92m, IsActive = true, FlagEmoji = "🇪🇺", LastUpdated = DateTime.Now },
                new Currency { Code = "LBP", Name = "Lebanese Pound", Symbol = "LL", ExchangeRate = 89500m, IsActive = true, FlagEmoji = "🇱🇧", LastUpdated = DateTime.Now },
                new Currency { Code = "AED", Name = "UAE Dirham", Symbol = "د.إ", ExchangeRate = 3.67m, IsActive = true, FlagEmoji = "🇦🇪", LastUpdated = DateTime.Now },
                new Currency { Code = "SAR", Name = "Saudi Riyal", Symbol = "﷼", ExchangeRate = 3.75m, IsActive = true, FlagEmoji = "🇸🇦", LastUpdated = DateTime.Now },
                new Currency { Code = "GBP", Name = "British Pound", Symbol = "£", ExchangeRate = 0.79m, IsActive = true, FlagEmoji = "🇬🇧", LastUpdated = DateTime.Now },
                new Currency { Code = "TRY", Name = "Turkish Lira", Symbol = "₺", ExchangeRate = 32.5m, IsActive = true, FlagEmoji = "🇹🇷", LastUpdated = DateTime.Now },
                new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "E£", ExchangeRate = 48.5m, IsActive = true, FlagEmoji = "🇪🇬", LastUpdated = DateTime.Now }
            );
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────
        // COMMISSIONS — 4 tiers selon le montant
        // ─────────────────────────────────────────────────────────
        private static async Task SeedCommissionsAsync(ApplicationDbContext db)
        {
            if (await db.Commissions.AnyAsync()) return;

            db.Commissions.AddRange(
                new Commission
                {
                    Label = "$0 – $100",
                    MinAmount = 0m,
                    MaxAmount = 100m,
                    Rate = 3.0m,
                    AgentShare = 1.5m,
                    PlatformShare = 1.5m,
                    IsActive = true,
                    UpdatedAt = DateTime.Now
                },
                new Commission
                {
                    Label = "$100 – $500",
                    MinAmount = 100.01m,
                    MaxAmount = 500m,
                    Rate = 2.5m,
                    AgentShare = 1.25m,
                    PlatformShare = 1.25m,
                    IsActive = true,
                    UpdatedAt = DateTime.Now
                },
                new Commission
                {
                    Label = "$500 – $2000",
                    MinAmount = 500.01m,
                    MaxAmount = 2000m,
                    Rate = 2.0m,
                    AgentShare = 1.0m,
                    PlatformShare = 1.0m,
                    IsActive = true,
                    UpdatedAt = DateTime.Now
                },
                new Commission
                {
                    Label = "$2000+",
                    MinAmount = 2000.01m,
                    MaxAmount = 9999999m,
                    Rate = 1.5m,
                    AgentShare = 0.75m,
                    PlatformShare = 0.75m,
                    IsActive = true,
                    UpdatedAt = DateTime.Now
                }
            );
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────
        // AGENTS — 4 agents dans différentes villes du Liban
        // ─────────────────────────────────────────────────────────
        private static async Task SeedAgentsAsync(ApplicationDbContext db, UserManager<User> userManager)
        {
            // Ne pas re-seeder si des agents existent déjà
            if (await db.Agents.CountAsync() > 1) return;

            var agentData = new[]
            {
                new { Email="agent.hamra@moneymoney.com",    Name="Hamra Exchange",    Owner="Karim Nassar",   Address="Hamra Street, Beirut",      Phone="+961 1 750 100", Hours="Mon-Sat 9:00-20:00", Lat=33.8959, Lng=35.4784, Commission=245.50m },
                new { Email="agent.ashrafieh@moneymoney.com",Name="Achrafieh Express", Owner="Nadia Khoury",  Address="Sassine Square, Achrafieh",  Phone="+961 1 200 300", Hours="Mon-Sat 8:30-19:00", Lat=33.8875, Lng=35.5136, Commission=189.00m },
                new { Email="agent.tripoli@moneymoney.com",  Name="Tripoli Transfer",  Owner="Hassan Khalil", Address="Tell Square, Tripoli",       Phone="+961 6 430 500", Hours="Mon-Fri 9:00-18:00", Lat=34.4369, Lng=35.8497, Commission=112.75m },
                new { Email="agent.saida@moneymoney.com",    Name="Saida MoneyPoint",  Owner="Rima Jaber",    Address="Old Souk, Saida",            Phone="+961 7 720 200", Hours="Mon-Sat 9:00-17:00", Lat=33.5632, Lng=35.3714, Commission=78.25m  }
            };

            for (int i = 0; i < agentData.Length; i++)
            {
                var d = agentData[i];
                if (await userManager.FindByEmailAsync(d.Email) != null) continue;

                var user = new User
                {
                    Email = d.Email,
                    UserName = d.Email,
                    FullName = d.Owner,
                    EmailConfirmed = true,
                    AccountNumber = $"ACC-2026-{(30001 + i):D5}",
                    PreferredCurrency = "USD",
                    Balance = 0
                };

                var result = await userManager.CreateAsync(user, "Agent@12345");
                if (!result.Succeeded) continue;

                await userManager.AddToRoleAsync(user, Roles.Agent);

                db.Agents.Add(new Agent
                {
                    UserId = user.Id,
                    StoreName = d.Name,
                    OwnerName = d.Owner,
                    Address = d.Address,
                    Phone = d.Phone,
                    WorkingHours = d.Hours,
                    Latitude = d.Lat,
                    Longitude = d.Lng,
                    Status = AgentStatus.Approved,
                    TotalCommissionEarned = d.Commission,
                    RegisteredAt = DateTime.Now.AddDays(-(30 - i * 5)),
                    ApprovedAt = DateTime.Now.AddDays(-(28 - i * 5))
                });
            }
            await db.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────
        // REVIEWS — avis d'utilisateurs sur le service
        // ─────────────────────────────────────────────────────────
        private static async Task SeedReviewsAsync(ApplicationDbContext db, UserManager<User> userManager)
        {
            if (await db.Reviews.AnyAsync()) return;

            // Récupérer des users existants
            var users = await db.Users.Take(4).ToListAsync();
            if (!users.Any()) return;

            var reviews = new[]
            {
                new { Rating=5, Comment="Very fast transfer! My family received the money in minutes. Excellent service.",       DaysAgo=2  },
                new { Rating=5, Comment="The app is easy to use. Cash-in at the Hamra branch was smooth and professional.",      DaysAgo=5  },
                new { Rating=4, Comment="Good service overall. The exchange rates are fair and the commissions are reasonable.",  DaysAgo=8  },
                new { Rating=4, Comment="Happy with the service. The agent was helpful and the transaction was processed fast.",  DaysAgo=12 },
                new { Rating=3, Comment="Service is OK but could improve the mobile experience. Transfer worked well though.",   DaysAgo=15 },
                new { Rating=5, Comment="Best money transfer service in Lebanon! I use it every month to send money abroad.",     DaysAgo=18 },
                new { Rating=4, Comment="Reliable and secure. The serial number tracking feature is very useful.",               DaysAgo=22 }
            };

            for (int i = 0; i < reviews.Length; i++)
            {
                var r = reviews[i];
                var user = users[i % users.Count];
                db.Reviews.Add(new Review
                {
                    UserId = user.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = DateTime.Now.AddDays(-r.DaysAgo)
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
