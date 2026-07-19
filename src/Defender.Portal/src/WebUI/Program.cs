using Defender.Common.Extension;
using Defender.Portal.Application;
using Defender.Portal.Infrastructure;
using Defender.Portal.WebUI;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
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
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

builder.Services.AddDefenderHealthChecks();
builder.Services.AddDefenderCors(builder.Environment);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = 2;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (builder.Environment.IsLocalOrDevelopment())
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
app.UseStaticFiles();
app.UseRouting();

app.UseCors(CorsExtensions.DefenderCorsPolicy);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseProblemDetails();

var metricsEnabled = builder.Configuration.GetValue("Defender:Observability:Metrics:Enabled", false);
if (metricsEnabled)
{
    app.UseHttpMetrics();
    app.MapMetrics("/metrics");
}

app.MapControllerRoute(
    name: "default",
    pattern: "api/{controller}/{action=Index}");
app.MapDefenderHealthChecks();

app.MapFallbackToFile("index.html");

await app.RunAsync();
