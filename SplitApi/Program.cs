using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ------------ Swagger paslaugos ------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(cfg =>
{
    cfg.SwaggerDoc("v1",
        new OpenApiInfo { Title = "Split API", Version = "v1" });
});
// -------------------------------------------

var app = builder.Build();

// ------------ Swagger middleware -----------
app.UseSwagger();      // JSON  → /swagger/v1/swagger.json
app.UseSwaggerUI();    // HTML  → /swagger (+ /swagger/index.html)
// -------------------------------------------

// paprastas testas
app.MapGet("/weatherforecast", () => "ok");

app.Run();
