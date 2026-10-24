using Earthquake.API.Services;
using Earthquake.Data;
using Earthquake.Repository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<EarthquakeDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IEarthquakeRepository, EarthquakeRepository>();
builder.Services.AddScoped<IUsgsService, UsgsService>();
builder.Services.AddScoped<ICountryService, CountryService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();