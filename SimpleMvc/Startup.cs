using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleMvc.Data;
using SimpleMvc.Models;
using SimpleMvc.Services;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using SimpleMvc.Mappings;
using NETCore.MailKit.Extensions;
using NETCore.MailKit.Infrastructure.Internal;
using SimpleMvc.Config;

namespace SimpleMvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            //Setup Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds services required for using options.
            services.AddOptions();
            
            // Register the IConfiguration instance
            services.AddSingleton(Configuration);

            // Configure settings            
            services.Configure<SiteSettings>(Configuration.GetSection("SiteSettings"));
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Configure Identity (Security)
            services.AddIdentity<ApplicationUser, ApplicationRole>(config =>
            {
                // If you change this, you need to change the regular expression in the code too!
                config.Password.RequiredLength = 8;
                config.Password.RequireDigit = true;
                config.Password.RequireLowercase = true;
                config.Password.RequireUppercase = true;
                config.Password.RequireNonAlphanumeric = false;
                config.User.RequireUniqueEmail = true;
                config.SignIn.RequireConfirmedEmail = true;
                config.Lockout.MaxFailedAccessAttempts = 10;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            if (Configuration["Authentication:Facebook:Enabled"] == "true")
            {
                services.AddAuthentication().AddFacebook(options =>
                {
                    options.ClientId = Configuration["Authentication:Facebook:AppId"];
                    options.ClientSecret = Configuration["Authentication:Facebook:AppSecret"];
                });
            }

            if (Configuration["Authentication:Google:Enabled"] == "true")
            {
                services.AddAuthentication().AddGoogle(options =>
                {
                    options.ClientId = Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                });
            }
            
            // Add application services.
            services.AddTransient<IEmailService, MailKitService>();

            // Add Serilog
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            // Add Database Initializer
            services.AddScoped<IDbInitializer, DbInitializer>();

            services.AddMvc()
                 .AddJsonOptions(options =>
                 {
                     options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                 });

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "Sample API",
                    Description = "A simple example ASP.NET Core Web API",
                    TermsOfService = "None",
                    Contact = new Contact { Name = "Samples Contributor", Email = "", Url = "https://github.com/nicholashew/aspNet-samples" },
                    License = new License { Name = "Use under License XXX", Url = "https://example.com/license" }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IDbInitializer dbInitializer)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();

                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            // AutoMapper configs for map between Model and ViewModel
            AutoMapperConfig.Configure();

            // Initialize Database Seed before route start to prevent DI Service dsiposed error
            dbInitializer.InitializeAsync().Wait();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });            
        }
    }
}
