# Earthquake App — Caso Práctico 1

Aplicación .NET para consultar terremotos de USGS, guardarlos en SQL Server y buscar por país.

## Tecnologías
- .NET 10 (ASP.NET Core Web API + MVC)
- Entity Framework Core + SQL Server (LocalDB)
- Repository Pattern + DTO Pattern
- MemoryCache

## Estructura
- `Earthquake.API` — Web API principal
- `Earthquake.Data` — Entidades EF + DbContext + Migraciones
- `Earthquake.DTO` — DTOs
- `Earthquake.Repository` — Repository Pattern
- `Earthquake.MVC` — Frontend

## Endpoints
- `GET /api/earthquakes/country/{country}` — Terremotos por país + info del país

## Cómo correr
```bash
dotnet ef database update -p Earthquake.Data -s Earthquake.API
dotnet run --project Earthquake.API