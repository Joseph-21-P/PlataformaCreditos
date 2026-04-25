# Plataforma de Créditos - Evaluación Parcial

Sistema web interno para la gestión y evaluación de solicitudes de crédito.

## Stack Tecnológico
- **Framework:** ASP.NET Core MVC (.NET 8)
- **Base de Datos:** SQLite (EF Core)
- **Caché y Sesión:** Redis
- **Infraestructura:** Render.com

## Configuración Local
1. Clonar el repositorio.
2. Ejecutar migraciones: `dotnet ef database update`.
3. Configurar Redis en `appsettings.json`.
4. Ejecutar: `dotnet run`.

## Variables de Entorno en Producción (Render)
- `ASPNETCORE_ENVIRONMENT`: Production
- `ASPNETCORE_URLS`: http://0.0.0.0:${PORT}
- `ConnectionStrings__DefaultConnection`: DataSource=app.db
- `Redis__ConnectionString`: [Tu URL de Redis Labs]

## URL de Despliegue
[INSERTAR TU URL DE RENDER AQUÍ]