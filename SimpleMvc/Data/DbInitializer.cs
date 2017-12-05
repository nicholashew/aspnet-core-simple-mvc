using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleMvc.Common;
using SimpleMvc.Extensions;
using SimpleMvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Data
{
    public class DbInitializer : IDbInitializer
    {
        private ApplicationDbContext _ctx;
        private IHostingEnvironment _env;
        private IConfiguration _config;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<ApplicationRole> _roleManager;
        private ILogger<DbInitializer> _logger;

        public DbInitializer(ApplicationDbContext ctx,
            IHostingEnvironment env,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<DbInitializer> logger
            )
        {
            _ctx = ctx;
            _env = env;
            _config = config;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (_env.IsDevelopment() && _config["DbInitializer:IterateDevDatabase"].ToLower() == "true")
                {
                    await _ctx.Database.EnsureDeletedAsync();
                    await _ctx.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Iterate Database successfully.");
                }

                // Create roles if not exists
                var roles = new string[] { Constants.Roles.SuperAdministrator, Constants.Roles.Administrator, Constants.Roles.Editor };

                foreach (var roleName in roles)
                {
                    if (!(await _roleManager.RoleExistsAsync(roleName)))
                    {
                        await _roleManager.CreateAsync(new ApplicationRole(roleName));
                    }
                }

                // Create the default Super User account
                if (_config["DbInitializer:SuperUser:EnsureCreated"].ToLower() == "true")
                {
                    string email = _config["DbInitializer:SuperUser:Email"];
                    string tempPassword = _config["DbInitializer:SuperUser:TempPassword"];
                    await CreateUserAsync(email, tempPassword, true, Constants.Roles.SuperAdministrator);
                }

                // Create the default Admin User account
                var adminUserSection = _config.GetSection("DbInitializer:AdminUser");
                if (adminUserSection != null)
                {
                    foreach (IConfigurationSection section in adminUserSection.GetChildren())
                    {
                        var ensureCreated = section.GetValue<bool>("EnsureCreated");
                        var email = section.GetValue<string>("Email");
                        var tempPassword = section.GetValue<string>("TempPassword");
                        if (ensureCreated)
                        {
                            await CreateUserAsync(email, tempPassword, true, Constants.Roles.Administrator);
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DbInitializer Initialize failed.");
            }
        }

        private async Task CreateUserAsync(string email, string password, bool emailConfirmed, string role)
        {
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = emailConfirmed,
                        CreatedDate = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        string errors = "";
                        foreach (var error in result.Errors)
                        {
                            errors += error.Description;
                        }
                        _logger.LogError($"Failed to create user {email}. {errors}");
                        //throw new InvalidOperationException($"Failed to create user {email}. {errors}");
                    }
                    else
                    {
                        if (!(await _userManager.AddToRoleAsync(user, role)).Succeeded)
                        {
                            _logger.LogCritical($"Failed to add role {role} to user {email}");
                            //throw new InvalidOperationException($"Failed to add role {role} to user {email}");
                        }
                    }
                }
            }
        }
    }
}