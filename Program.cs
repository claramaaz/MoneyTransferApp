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
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── 1. MVC ────────────────────────────────────────────
            builder.Services.AddControllersWithViews();

            // ── 2. Database ───────────────────────────────────────
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ── 3. Identity (PDF 10) ──────────────────────────────
            builder.Services.AddDefaultIdentity<User>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/Login";
            });

            // ── 4. Repository Pattern (PDF 11) ────────────────────
            builder.Services.AddScoped<IRepository<TxModel>, TransactionRepository>();
            builder.Services.AddScoped<IRepository<Models.Beneficiary>, BeneficiaryRepository>();
            builder.Services.AddScoped<IRepository<Models.Agent>, AgentRepository>();
            builder.Services.AddScoped<TransactionRepository>();
            builder.Services.AddScoped<BeneficiaryRepository>();
            builder.Services.AddScoped<AgentRepository>();

            // ── 5. HttpClient pour les appels API Anthropic ────────
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // ── 6. Seeders ────────────────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await RoleSeeder.SeedRolesAsync(services);
                await UserSeeder.SeedUsersAsync(services);
                // NOUVEAU : seeder pour currencies, commissions, agents, reviews
                await DataSeeder.SeedAsync(services);
            }

            // ── 7. Pipeline ───────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
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
