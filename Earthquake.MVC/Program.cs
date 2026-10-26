using Earthquake.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Habilitar controladores y vistas.
builder.Services.AddControllersWithViews();

// Habilitar la caché en memoria del MVC.
builder.Services.AddMemoryCache();

// Registrar el servicio y configurar su cliente HTTP.
builder.Services.AddHttpClient<
    IEarthquakeApiService,
    EarthquakeApiService>(client =>
    {
        var baseUrl = builder.Configuration["ApiBaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Falta configurar ApiBaseUrl en appsettings.json del MVC.");
        }

        client.BaseAddress = new Uri(
            baseUrl.TrimEnd('/') + "/");

        client.Timeout = TimeSpan.FromMinutes(10);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Publicar CSS, JavaScript e imágenes.
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Mantener Home hasta crear el nuevo controlador y su vista.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Earthquake}/{action=Index}/{id?}");

app.Run();