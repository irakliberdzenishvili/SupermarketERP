
using DotNetApiTemplate.Models.Data;
using Microsoft.AspNetCore.Identity;
using SupermarketERP.Constants;

namespace SupermarketERP.DB.Seed;

public static class EmployeeSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var userManager =
            services.GetRequiredService<UserManager<User>>();

        var employees = configuration
            .GetSection("SeedEmployees")
            .Get<List<SeedEmployee>>() ?? [];

        if (employees.Count == 0)
            return;

        var password = configuration["SeedEmployeesPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SeedEmployeesPassword is not configured.");
        }

        foreach (var employee in employees)
        {
            if (string.IsNullOrWhiteSpace(employee.Email) ||
                !Roles.All.Contains(employee.Role))
            {
                throw new InvalidOperationException(
                    "Invalid seed employee configuration.");
            }

            var user = await userManager.FindByEmailAsync(
                employee.Email);

            if (user is null)
            {
                user = new User
                {
                    UserName = employee.Email,
                    Email = employee.Email,
                    EmailConfirmed = false
                };

                var result = await userManager.CreateAsync(
                    user, password);

                EnsureSucceeded(result);
            }

            if (!await userManager.IsInRoleAsync(
                user, employee.Role))
            {
                var result = await userManager.AddToRoleAsync(
                    user, employee.Role);

                EnsureSucceeded(result);
            }
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result)
    {
        if (result.Succeeded)
            return;

        throw new InvalidOperationException(
            string.Join("; ",
                result.Errors.Select(x => x.Description)));
    }

    private sealed class SeedEmployee
    {
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
