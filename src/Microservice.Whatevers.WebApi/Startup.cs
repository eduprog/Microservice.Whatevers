using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using FluentValidation.AspNetCore;
using Microservice.Whatevers.Repositories.IoC;
using Microservice.Whatevers.Services.IoC;
using Microservice.Whatevers.Services.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;

namespace Microservice.Whatevers.Api
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddApiVersioning();

            services.AddMvcCore(options => options.SuppressAsyncSuffixInActionNames = false)
               .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<WhateverModelValidator>());

            services.AddHttpClient("google", c => { c.BaseAddress = new Uri(Configuration["UrlBaseGoogle"]); })
               .AddPolicyHandler(GetRetryPolicy());

            IocServices.Register(services);
            IoCRepositories.Register(services, Assembly.GetExecutingAssembly().GetName().Name);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
            HttpPolicyExtensions.HandleTransientHttpError()
               .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
               .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)));
    }
}