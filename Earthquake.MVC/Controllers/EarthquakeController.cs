using System.Net;
using System.Text.RegularExpressions;
using Earthquake.MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace Earthquake.MVC.Controllers;

public class EarthquakeController : Controller
{
    private readonly IEarthquakeApiService _service;
    private readonly ILogger<EarthquakeController> _logger;

    public EarthquakeController(
        IEarthquakeApiService service,
        ILogger<EarthquakeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Abrir la pantalla.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            await _service.InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo preparar la información de sismos.");

            ViewData["InitError"] =
                "No se pudo preparar la información. "
                + "Verifica que la API esté ejecutándose "
                + "y vuelve a intentar.";
        }

        return View();
    }

    // Recibir la búsqueda del navegador.
    [HttpGet]
    public async Task<IActionResult> Search(string? country)
    {
        try
        {
            // Validar que exista texto.
            if (string.IsNullOrWhiteSpace(country))
            {
                return Json(new
                {
                    ok = false,
                    message = "Escribe el nombre de un país."
                });
            }

            // Validar la longitud.
            if (country.Length > 100)
            {
                return Json(new
                {
                    ok = false,
                    message = "El máximo es 100 caracteres."
                });
            }

            // Quitar espacios innecesarios.
            country = Regex.Replace(
                country.Trim(),
                @"\s+",
                " ");

            // Aceptar únicamente letras y espacios.
            if (!Regex.IsMatch(
                country,
                @"^\p{L}+(?: \p{L}+)*$"))
            {
                return Json(new
                {
                    ok = false,
                    message = "Usa solo letras y espacios, "
                        + "sin números ni caracteres especiales."
                });
            }

            // Consultar el servicio del MVC.
            var result = await _service.SearchAsync(country);

            return Json(new
            {
                ok = true,
                data = result
            });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Json(new
            {
                ok = false,
                message = "País no encontrado. "
                    + "Revisa el nombre ingresado."
            });
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            return Json(new
            {
                ok = false,
                message = "La búsqueda enviada no es válida."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo realizar la búsqueda por país.");

            return Json(new
            {
                ok = false,
                message = "No se pudo realizar la búsqueda. "
                    + "Verifica que la API y la base de datos "
                    + "estén disponibles."
            });
        }
    }
}