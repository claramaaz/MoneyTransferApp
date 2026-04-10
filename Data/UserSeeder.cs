using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransferApp.Constants;
using MoneyTransferApp.Data;
using MoneyTransferApp.Models;

namespace MoneyTransferApp.Data
{
    public class UserSeeder
    {
        public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Admin par défaut
            await CreateUserWithRole(userManager, "admin@moneymoney.com", "Admin@12345", "Admin User", Roles.Admin);

            // User de test
            await CreateUserWithRole(userManager, "user@moneymoney.com", "User@12345", "Test User", Roles.User);

            // Agent de test
            var agentEmail = "agent@moneymoney.com";
            if (await userManager.FindByEmailAsync(agentEmail) == null)
            {
                var agentUser = new User
                {
                    Email = agentEmail,
                    UserName = agentEmail,
                    FullName = "Test Agent",
                    EmailConfirmed = true,
                    AccountNumber = "ACC-2026-55555",
                    PreferredCurrency = "USD",
                    Balance = 0
                };

                var result = await userManager.CreateAsync(agentUser, "Agent@12345");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(agentUser, Roles.Agent);

                    // Créer l'entrée Agent liée dans la table Agents
                    var alreadyExists = await context.Agents
                        .AnyAsync(a => a.UserId == agentUser.Id);

                    if (!alreadyExists)
                    {
                        context.Agents.Add(new Agent
                        {
                            UserId = agentUser.Id,
                            StoreName = "Money Money Beirut",
                            OwnerName = "Test Agent",             
                            Address = "Hamra Street, Beirut",
                            WorkingHours = "Mon-Sat 9am-6pm",
                            Latitude = 33.8959,
                            Longitude = 35.4784,
                            Status = AgentStatus.Approved,
                            TotalCommissionEarned = 0              
                        });
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

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
                    Balance = 1000
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}