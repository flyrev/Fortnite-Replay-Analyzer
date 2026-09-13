using FortniteReplayAnalyzer.Controllers.ExternalApis;
using FortniteReplayAnalyzer.ExternalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 524288000;
            });

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            var fortniteIoApiKey = Configuration["FORTNITE_IO_API_KEY"];
            var fortniteIoBaseUrl = "https://fortniteapi.io";
            services.AddSingleton<FortniteIoApiClient>(provider => new FortniteIoApiClient(provider.GetRequiredService<ILogger<FortniteIoApiClient>>(), fortniteIoBaseUrl, fortniteIoApiKey));

            var azureAccountUri = Configuration["AZURE_STORAGE_ACCOUNT_URI"];
            var azureContainer = Configuration["AZURE_STORAGE_CONTAINER"];
            var awsKey = Configuration["AWS_S3_ACCESS_KEY"];
            var awsSecret = Configuration["AWS_S3_ACCESS_SECRET"];
            var awsBucket = Configuration["AWS_S3_BUCKET"];

            if (Uri.TryCreate(azureAccountUri, UriKind.Absolute, out var accountUri) && !string.IsNullOrWhiteSpace(azureContainer))
            {
                services.AddSingleton<IReplayAnalysisStorage>(provider => new AzureBlobReplayAnalysisStorage(
                    provider.GetRequiredService<ILogger<AzureBlobReplayAnalysisStorage>>(),
                    accountUri,
                    azureContainer));
            }
            else if (!string.IsNullOrWhiteSpace(awsBucket))
            {
                services.AddSingleton<IReplayAnalysisStorage>(provider => new S3ReplayAnalysisStorage(
                    provider.GetRequiredService<ILogger<S3ReplayAnalysisStorage>>(),
                    awsKey,
                    awsSecret,
                    awsBucket));
            }
            else
            {
                services.AddSingleton<IReplayAnalysisStorage>(provider => new AzureBlobReplayAnalysisStorage(
                    provider.GetRequiredService<ILogger<AzureBlobReplayAnalysisStorage>>(),
                    null,
                    null));
            }
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
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
                    });
                });
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var httpsPort = Configuration["ASPNETCORE_HTTPS_PORT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT");
            if (env.IsDevelopment() || !string.IsNullOrWhiteSpace(httpsPort))
            {
                app.UseHttpsRedirection();
            }

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
