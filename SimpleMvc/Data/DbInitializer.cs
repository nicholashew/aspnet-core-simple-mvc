using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleMvc.Common;
using SimpleMvc.Models;
using System;
using System.Threading.Tasks;

namespace SimpleMvc.Data
{
    public class DbInitializer : IDbInitializer
    {
        private ApplicationDbContext _ctx;
        private IHostingEnvironment _env;
        private IConfiguration _config;
        private IServiceProvider _serviceProvider;
        private ILogger<DbInitializer> _logger;

        public DbInitializer(ApplicationDbContext ctx,
          IConfiguration config,
          IHostingEnvironment env,
          IServiceProvider serviceProvider,
          ILogger<DbInitializer> logger
          )
        {
            _ctx = ctx;
            _env = env;
            _config = config;
            _serviceProvider = serviceProvider;
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

                using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    // Get role manager service
                    var _roleManager = serviceScope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();

                    // Create roles if not exists
                    var roles = new string[] {
                        Constants.Roles.SuperAdministrator,
                        Constants.Roles.Administrator,
                        Constants.Roles.Editor,
                        Constants.Roles.User
                    };

                    foreach (var roleName in roles)
                    {
                        if (!(await _roleManager.RoleExistsAsync(roleName)))
                        {
                            await _roleManager.CreateAsync(new ApplicationRole(roleName));
                        }
                    }

                    if (_config["DbInitializer:SuperUser:EnsureCreated"].ToLower() == "true")
                    {
                        // Get user manager service
                        var _userManager = serviceScope.ServiceProvider.GetService<UserManager<ApplicationUser>>();

                        // Create the default Super User account
                        string hostEmail = _config["DbInitializer:SuperUser:Email"];
                        string hostPassword = _config["DbInitializer:SuperUser:TempPassword"];

                        if (!string.IsNullOrWhiteSpace(hostEmail) && !string.IsNullOrWhiteSpace(hostPassword))
                        {
                            var admin = await _userManager.FindByEmailAsync(hostEmail);

                            if (admin == null)
                            {
                                admin = new ApplicationUser
                                {
                                    UserName = hostEmail,
                                    Email = hostEmail,
                                    EmailConfirmed = true
                                };

                                // Create Super User
                                var result = await _userManager.CreateAsync(admin, hostPassword);
                                if (!result.Succeeded)
                                {
                                    string errors = "";
                                    foreach (var error in result.Errors)
                                    {
                                        errors += error.Description;
                                    }
                                    _logger.LogCritical($"Failed to create Super User - {errors}");
                                    throw new InvalidOperationException($"Failed to create Super User - {errors}");
                                }
                                else
                                {
                                    // Add to Super User Role
                                    if (!(await _userManager.AddToRoleAsync(admin, Constants.Roles.SuperAdministrator)).Succeeded)
                                    {
                                        _logger.LogCritical("Failed to update Super User Role");
                                        throw new InvalidOperationException("Failed to update Super User Role");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DbInitializer Initialize failed.");
            }
        }
    }
}