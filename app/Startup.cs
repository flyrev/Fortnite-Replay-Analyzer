using FortniteReplayAnalyzer.Controllers.ExternalApis;
using FortniteReplayAnalyzer.ExternalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace FortniteReplayAnalyzer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            var fortniteIoApiKey = Configuration["FORTNITE_IO_API_KEY"];
            var fortniteIoBaseUrl = "https://fortniteapi.io";
            services.AddSingleton<FortniteIoApiClient>(provider => new FortniteIoApiClient(provider.GetRequiredService<ILogger<FortniteIoApiClient>>(), fortniteIoBaseUrl, fortniteIoApiKey));

            var s3Key = Configuration["AWS_S3_ACCESS_KEY"];
            var s3Secret = Configuration["AWS_S3_ACCESS_SECRET"];
            var s3Bucket = Configuration["AWS_S3_BUCKET"];

            Console.WriteLine($"Using bucket {s3Bucket}");
            services.AddSingleton<ReplayAnalysisStorage>(provider => new ReplayAnalysisStorage(provider.GetRequiredService<ILogger<ReplayAnalysisStorage>>(), s3Key, s3Secret, s3Bucket));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
