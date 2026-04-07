using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;
using MoneyTransferApp.Repositories;
using TxModel = MoneyTransferApp.Models.Transaction;

namespace MoneyTransferApp
{
    public class Program
    {
        // async Task Main — nécessaire pour appeler les Seeders
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── 1. Add services to the container ─────────────────────────

            builder.Services.AddControllersWithViews();

            // ── 2. Database — ton code original ──────────────────────────
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ── 3. Identity (PDF 10, slides 4 et 8) ──────────────────────
            // AddDefaultIdentity = AddIdentity + pages de base
            // NE NÉCESSITE PAS Identity.UI si on fait nos propres vues
            builder.Services.AddDefaultIdentity<User>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()                        // PDF 10: pour gérer les rôles
            .AddEntityFrameworkStores<ApplicationDbContext>();        // PDF 10: stocker dans EF Core

            // Redirection si non connecté (PDF 10, slide 25)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/Login";
            });

            // ── 4. Repository Pattern — Dependency Injection ───────────────
            // PDF 11 slide 19: AddScoped<IRepository<T>, ConcreteRepository>()
            builder.Services.AddScoped<IRepository<TxModel>, TransactionRepository>();
            builder.Services.AddScoped<IRepository<Models.Beneficiary>, BeneficiaryRepository>();
            builder.Services.AddScoped<IRepository<Models.Agent>, AgentRepository>();

            // Accès aux méthodes spécifiques (GetByUserIdAsync etc.)
            builder.Services.AddScoped<TransactionRepository>();
            builder.Services.AddScoped<BeneficiaryRepository>();
            builder.Services.AddScoped<AgentRepository>();

            var app = builder.Build();

            // ── 5. Seeders — PDF 10 slides 16-18 ─────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                // PDF 10 slide 17:
                // RoleSeeder.SeedRolesAsync(services).Wait();
                // UserSeeder.SeedUsersAsync(services).Wait();
                await RoleSeeder.SeedRolesAsync(services);
                await UserSeeder.SeedUsersAsync(services);
            }

            // ── 6. Configure the HTTP request pipeline ────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();  // ← AVANT UseAuthorization (PDF 10)
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
