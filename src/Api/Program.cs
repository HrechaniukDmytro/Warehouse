using Api.Endpoints;
using Core.Abstractions;
using Core.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var dataPath = Path.Combine(AppContext.BaseDirectory, "data", "items.json");
builder.Services.AddSingleton<ICatalogStore>(_ => new FileCatalogStore(dataPath));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });

    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json")
               .WithTitle("Warehouse API")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Curl);
    });
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapProducts();

app.Run();