using Microsoft.AspNetCore.Identity;
using MoneyTransferApp.Constants;

namespace MoneyTransferApp.Data
{
    // Exactement comme dans le PPT slide 16
    public class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(Roles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            if (!await roleManager.RoleExistsAsync(Roles.User))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.User));
            }

            // On ajoute Agent en plus (notre projet a 3 rôles)
            if (!await roleManager.RoleExistsAsync(Roles.Agent))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Agent));
            }
        }
    }
}