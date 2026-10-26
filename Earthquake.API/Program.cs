using Earthquake.API.Services;
using Earthquake.Data;
using Earthquake.Repository;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controladores y Swagger.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Caché en memoria.
builder.Services.AddMemoryCache();

// Cliente HTTP con tiempo suficiente para la carga histórica.
builder.Services.AddHttpClient(string.Empty, client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Conexión con SQL Server.
builder.Services.AddDbContext<EarthquakeDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"));
});

// Inyección de dependencias.
builder.Services.AddScoped<
    IEarthquakeRepository, EarthquakeRepository>();

builder.Services.AddScoped<
    IUsgsService, UsgsService>();

builder.Services.AddScoped<
    ICountryService, CountryService>();

var app = builder.Build();

// Respuesta controlada ante errores inesperados.
app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var error = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(
            error,
            "No se pudo completar la solicitud de terremotos.");

        context.Response.StatusCode =
            StatusCodes.Status503ServiceUnavailable;

        await context.Response.WriteAsJsonAsync(new
        {
            message = "No se pudo completar la consulta. "
                + "Revisa la conexión con los servicios externos "
                + "y la base de datos."
        });
    });
});

// Publicar Swagger y las rutas.
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();