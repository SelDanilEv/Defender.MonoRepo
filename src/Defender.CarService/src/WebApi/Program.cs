using Defender.CarService.WebApi;
using Defender.Common.Extension;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddDefenderHealthChecks();
builder.Services.AddDefenderCors(builder.Environment);

var app = builder.Build();

if (app.Environment.IsLocalOrDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });

}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsExtensions.DefenderCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseProblemDetails();
app.MapControllers();
app.MapDefenderHealthChecks();

await app.RunAsync();

public partial class Program;
