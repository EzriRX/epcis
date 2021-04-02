using Carter;
using FasTnT.Application;
using FasTnT.Application.Behaviors;
using FasTnT.Application.Queries.Poll;
using FasTnT.Application.Services;
using FasTnT.Infrastructure.Database;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FasTnT.Host
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var contextOptions = new DbContextOptionsBuilder<EpcisContext>().UseSqlServer("Server=(local);Database=EpcisEfCore;Integrated Security=true;");

            services.AddDbContext<EpcisContext>(o => o.UseSqlServer("Server=(local);Database=EpcisEfCore;Integrated Security=true;").EnableSensitiveDataLogging());
            services.AddMediatR(typeof(ICommand<>).Assembly);
            services.AddValidatorsFromAssembly(typeof(CommandValidationBehavior<,>).Assembly);
            services.AddTransient<IEpcisQuery, SimpleEventQuery>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandValidationBehavior<,>));
            services.AddCarter(o => o.OpenApi.Enabled = true);

            var context = new EpcisContext(contextOptions.Options);
            context.Database.Migrate();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseExceptionHandler("/epciserror");

            app.UseRouting();
            app.UseEndpoints(builder => builder.MapCarter());
        }
    }
}
