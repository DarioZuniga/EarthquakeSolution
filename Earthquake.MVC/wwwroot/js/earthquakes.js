const app = document.getElementById("earthquake-app");
const form = document.getElementById("search-form");
const button = document.getElementById("search-button");
const loading = document.getElementById("loading");

function mostrarError(message) {
    return Swal.fire({
        title: "No se pudo buscar",
        text: message,
        icon: "warning"
    });
}

form.addEventListener("submit", function (event) {
    event.preventDefault();

    if (button.disabled) {
        return;
    }

    const input = document.getElementById("country").value;

    const country = input.trim().replace(/\s+/g, " ");

    // Validar el texto antes de enviar la solicitud.
    if (!country) {
        mostrarError("Escribe el nombre de un país.");
        return;
    }

    if (input.length > 100) {
        mostrarError("El máximo es 100 caracteres.");
        return;
    }

    if (!/^\p{L}+(?: \p{L}+)*$/u.test(country)) {
        mostrarError(
            "Usa solo letras y espacios, "
            + "sin números ni caracteres especiales."
        );
        return;
    }

    button.disabled = true;
    loading.hidden = false;

    document.getElementById("results").hidden = true;
    document.getElementById("country-card").hidden = true;

    const url = app.dataset.searchUrl
        + "?country="
        + encodeURIComponent(country);

    fetch(url)
        .then(function (response) {
            if (!response.ok) {
                throw new Error(
                    "El sitio no pudo completar la consulta."
                );
            }

            return response.json();
        })
        .then(function (json) {
            if (!json.ok) {
                return mostrarError(json.message);
            }

            mostrarResultados(json.data);
        })
        .catch(function () {
            mostrarError(
                "No fue posible completar la búsqueda. "
                + "Vuelve a intentar."
            );
        })
        .finally(function () {
            button.disabled = false;
            loading.hidden = true;
        });
});

function mostrarResultados(data) {
    // Mostrar la información del país.
    document.getElementById("country-name").textContent =
        data.country.name;

    document.getElementById("country-capital").textContent =
        data.country.capital || "No disponible";

    document.getElementById("country-population").textContent =
        Number(data.country.population).toLocaleString("es-CR");

    document.getElementById("country-card").hidden = false;

    // Preparar los resultados.
    const items = data.earthquakes || [];

    const body = document.getElementById("earthquake-rows");

    body.replaceChildren();

    document.getElementById("results-title").textContent =
        "Sismos encontrados: " + items.length;

    document.getElementById("empty-message").hidden =
        items.length > 0;

    document.getElementById("table-container").hidden =
        items.length === 0;

    document.getElementById("results").hidden = false;

    // Agregar una fila por cada sismo.
    for (const quake of items) {
        const row = body.insertRow();

        row.insertCell().textContent = quake.place;

        row.insertCell().textContent = quake.magnitude;

        row.insertCell().textContent =
            new Date(quake.time).toLocaleString("es-CR");

        row.insertCell().textContent =
            Number(quake.latitude).toFixed(4);

        row.insertCell().textContent =
            Number(quake.longitude).toFixed(4);
    }
}

// Mostrar un error de preparación si el controlador lo envió.
if (app.dataset.initError) {
    mostrarError(app.dataset.initError);
}