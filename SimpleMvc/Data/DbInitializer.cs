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

                // Create seed for development enviroment
                if (_env.IsDevelopment() && _config["DbInitializer:IterateDevDatabase"].ToLower() == "true")
                {
                    var rnd = new Random();

                    for (int i = 1, l = 10; i < l; i++)
                    {
                        _ctx.Buildings.Add(new Building
                        {
                            BuildingName = "Building" + i,
                            Address1 = "addr" + i,
                            Address2 = "",
                            Postal = "999999",
                            Country = "SomeWhere",
                            SiteName = "SecretSite",
                            Latitude = rnd.NextDecimal(1.0m, 1.0999m),
                            Longitude = rnd.NextDecimal(1.1m, 1.3m),
                        });
                    }
                    _ctx.SaveChanges();

                    _ctx.Problems.AddRange(new Problem[] {
                         new Problem { CategoryName="Building", NatureName="No female" },
                         new Problem { CategoryName="Electrical", NatureName="No power" },
                         new Problem { CategoryName="Water", NatureName="No water" },
                         new Problem { CategoryName="Others", NatureName="Others" }
                     });
                    _ctx.SaveChanges();

                    for (int i = 1, l = 5; i < l; i++)
                    {
                        _ctx.Tickets.Add(new Ticket
                        {
                            FirstName = "aspnetsample",
                            LastName = "user",
                            ContactNumber = "88888888",
                            Email = "aspnetsample@mailinator.com",
                            BuildingId = rnd.Next(1, _ctx.Buildings.Count()),
                            ProblemId = rnd.Next(1, _ctx.Problems.Count()),
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = 1,
                            Attachments = new List<Attachment> {
                                new Attachment {
                                    FileName = "foo.jpg",
                                    FileBinary = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADQAAAA0CAYAAADFeBvrAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAARASURBVGhD7ZpZSFRhFMc/y4ceApPqoQgrNcsWW6ysIAqKgoIeooUg2ujBpyAIoo1CioKeigQpM7cx07K0hWjBFiqtyCKLLCjabBMly706nXPud2funTszzujcRfEPB3S+M8P9zfnO+c75VEAvUx+Q09UH5BQ1t/6RP+nVI4HSi9+AGJkNccuvyFc86lFAFJXhSy+CGJsPYvoZEMOy4E7Vd7mqqMcApZ/FqIzOUUCmnAaRUgS/mzvkqkc9Aih5/XUQY/Kg3+xiEONdMG7VVblilKOBPn1vwmgUckT6E0x8LqzaeV+u+pZjgVxX33Pii5lFCsyoHNh3olqu+pcjgTYfeMT5QiAMg2CnLr6Tq4HlOKDkdZgv4/I9MDHZUHbns1ztXI4CGrqoFETSaYicc9YNc/lerVwNTo4BYoCphTqY0tvBR0aV/UD//oGYhpUs+YwHBgtAVtlb6RCabAVq7/jLW4wOSzdMXC6kZb6QHqHLNiCGmVQAYoYCQyYS82Ht3grp0TXZBITbjNoXGRmGwUhRheuubAHinNHCYP4MnF8iV7sny4H6zcI8kQWALCIFf8etFi5ZChS14Ly7NJNxEcAu4OXbn9Kj+7IMiIYxMVk5NFWjDvpw/ivpER5ZArR4y20QE1x6GIRL2XRTeoRPpgPtyajmSOhgcDijrWeGTAW68fAr5wjligqj5s2bD43SK7wyDYjGYxGbw1VNF51EF2w/9kx6hV9+gUrKP8KAuee4r6JvdOjiUqh5H/y3yg8/o0gPg2fPYOyozZRPoCVb7yoXEnJa5G1ClxMjTsGtJ9+kl39xEUgq0MHwZ+BnNja1Sy9zZACaThcS4134AJ6HUY0PRYxWU4vxtkVVAY3O2GBq84aMDs/9WS+ll3nSAV2r+AJiSKZh32uNTvmEFcYLPhI3nLG5xrzBrTcID1UrZIhQZXUd501AKEz2qlf18h0eUZ5pezQytarV/miWXubKZw5V1dQbyq3WKErTvDrjPRnPeVsZfHFE2Jj2UHqZL59ApJLyTxwpX1D8reParyYll340tPr8ArjxRCAr5ReItCGtEsREfbVSjeaXbUeV84TyQzuouX2wQyi++YF9rFJAIFIkRQPLt+FhsX2JXnABTuLsT4elYR1zKWbZJfkp1qlToKc1De5LP8NDIyhd1XqvqVuyts6aQqBVp0CkeanlujnG++G9X6NOeuWOwHfQZikooLZ2PF/8RMnb+PBNyJPvtF5BAZFW73rA28sXhNaoyziYbX5H4E9BA1G746+Mq8ZzDhYDOxU0ECll0w3dBYe3ifg87tLtVEhA1ypxYBtr7Ab4L2uYN3aUaW+FBESigzZC0+fxWIEN6ZHC19LDXoUMtGa3Uhz4rMECEL3wArS0+f6fATsUMhCPGPRndSwQqYcey1edo5CBSCLqOF+AOFFdAmoOMLHarS4BOVl9QE5XLwMC+A/+5TagzRzS2wAAAABJRU5ErkJggg=="
                                },
                                new Attachment {
                                    FileName = "bar.jpg",
                                    FileBinary = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADQAAAA0CAYAAADFeBvrAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAARASURBVGhD7ZpZSFRhFMc/y4ceApPqoQgrNcsWW6ysIAqKgoIeooUg2ujBpyAIoo1CioKeigQpM7cx07K0hWjBFiqtyCKLLCjabBMly706nXPud2funTszzujcRfEPB3S+M8P9zfnO+c75VEAvUx+Q09UH5BQ1t/6RP+nVI4HSi9+AGJkNccuvyFc86lFAFJXhSy+CGJsPYvoZEMOy4E7Vd7mqqMcApZ/FqIzOUUCmnAaRUgS/mzvkqkc9Aih5/XUQY/Kg3+xiEONdMG7VVblilKOBPn1vwmgUckT6E0x8LqzaeV+u+pZjgVxX33Pii5lFCsyoHNh3olqu+pcjgTYfeMT5QiAMg2CnLr6Tq4HlOKDkdZgv4/I9MDHZUHbns1ztXI4CGrqoFETSaYicc9YNc/lerVwNTo4BYoCphTqY0tvBR0aV/UD//oGYhpUs+YwHBgtAVtlb6RCabAVq7/jLW4wOSzdMXC6kZb6QHqHLNiCGmVQAYoYCQyYS82Ht3grp0TXZBITbjNoXGRmGwUhRheuubAHinNHCYP4MnF8iV7sny4H6zcI8kQWALCIFf8etFi5ZChS14Ly7NJNxEcAu4OXbn9Kj+7IMiIYxMVk5NFWjDvpw/ivpER5ZArR4y20QE1x6GIRL2XRTeoRPpgPtyajmSOhgcDijrWeGTAW68fAr5wjligqj5s2bD43SK7wyDYjGYxGbw1VNF51EF2w/9kx6hV9+gUrKP8KAuee4r6JvdOjiUqh5H/y3yg8/o0gPg2fPYOyozZRPoCVb7yoXEnJa5G1ClxMjTsGtJ9+kl39xEUgq0MHwZ+BnNja1Sy9zZACaThcS4134AJ6HUY0PRYxWU4vxtkVVAY3O2GBq84aMDs/9WS+ll3nSAV2r+AJiSKZh32uNTvmEFcYLPhI3nLG5xrzBrTcID1UrZIhQZXUd501AKEz2qlf18h0eUZ5pezQytarV/miWXubKZw5V1dQbyq3WKErTvDrjPRnPeVsZfHFE2Jj2UHqZL59ApJLyTxwpX1D8reParyYll340tPr8ArjxRCAr5ReItCGtEsREfbVSjeaXbUeV84TyQzuouX2wQyi++YF9rFJAIFIkRQPLt+FhsX2JXnABTuLsT4elYR1zKWbZJfkp1qlToKc1De5LP8NDIyhd1XqvqVuyts6aQqBVp0CkeanlujnG++G9X6NOeuWOwHfQZikooLZ2PF/8RMnb+PBNyJPvtF5BAZFW73rA28sXhNaoyziYbX5H4E9BA1G746+Mq8ZzDhYDOxU0ECll0w3dBYe3ifg87tLtVEhA1ypxYBtr7Ab4L2uYN3aUaW+FBESigzZC0+fxWIEN6ZHC19LDXoUMtGa3Uhz4rMECEL3wArS0+f6fATsUMhCPGPRndSwQqYcey1edo5CBSCLqOF+AOFFdAmoOMLHarS4BOVl9QE5XLwMC+A/+5TagzRzS2wAAAABJRU5ErkJggg=="
                                }
                            }
                        });
                    }
                    _ctx.SaveChanges();
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