using Microsoft.AspNetCore.Identity;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Data
{
    // Exactement comme dans le PPT slide 18
    public class UserSeeder
    {
        public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // Créer un Admin par défaut
            await CreateUserWithRole(userManager, "admin@transferpay.com", "Admin@12345", "Admin User", Roles.Admin);

            // Créer un User de test
            await CreateUserWithRole(userManager, "user@transferpay.com", "User@12345", "Test User", Roles.User);
        }

        // Méthode helper — PDF 10 slide 18
        private static async Task CreateUserWithRole(
            UserManager<User> userManager,
            string email,
            string password,
            string fullName,
            string role)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new User
                {
                    Email = email,
                    UserName = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    AccountNumber = $"ACC-{DateTime.Now.Year}-{new Random().Next(10000, 99999)}",
                    PreferredCurrency = "USD",
                    Balance = 0
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}
