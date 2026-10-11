
using ClinicManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClinicManagementSystem.Web.Data.Seeders
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            var roleManager = serviceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = serviceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Staff" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                result.Errors.Select(e => e.Description)));
                    }
                }
            }

            var email = configuration["SeedAdmin:Email"];
            var password = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(
                    admin, password);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ",
                            result.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var result = await userManager.AddToRoleAsync(
                    admin, "Admin");

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
