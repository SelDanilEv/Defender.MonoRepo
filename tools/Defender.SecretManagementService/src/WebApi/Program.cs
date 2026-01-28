using Defender.Common.Extension;
using Defender.SecretManagementService.Application;
using Defender.SecretManagementService.Infrastructure;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();

builder.Logging.ClearProviders()
    .AddSerilog(logger)
    .AddConsole();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddWebUIServices(builder.Environment, builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

if (builder.Environment.IsLocalOrDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseDefaultFiles();
    app.UseStaticFiles();
}
else
{
    app.UseDeveloperExceptionPage();

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseProblemDetails();

app.MapControllerRoute(
    name: "default",
    pattern: "api/{controller}/{action=Index}");

await app.RunAsync();
