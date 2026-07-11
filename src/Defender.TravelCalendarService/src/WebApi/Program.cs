using Defender.Common.Extension;
using Defender.TravelCalendarService.Application;
using Defender.TravelCalendarService.Infrastructure;
using Defender.TravelCalendarService.WebApi;
using Hellang.Middleware.ProblemDetails;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
builder.Logging.ClearProviders().AddSerilog(logger).AddConsole();
builder.Services.AddCommonServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddTravelCalendarWebApi(builder.Configuration, builder.Environment);
builder.Services.AddDefenderHealthChecks();

var app = builder.Build();
if (app.Environment.IsLocalOrDevelopment())
{
    app.UseDeveloperExceptionPage(); app.UseSwagger(); app.UseSwaggerUI();
}
else app.UseHsts();
app.UseRouting(); app.UseCors("AllowAll"); app.UseAuthentication(); app.UseAuthorization(); app.UseProblemDetails(); app.MapControllers(); app.MapDefenderHealthChecks();
await app.RunAsync();

public partial class Program;
