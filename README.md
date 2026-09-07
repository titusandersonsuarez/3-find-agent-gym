# LeadGym AI

LeadGym AI es un proyecto en C# que usa Microsoft Semantic Kernel para orquestar varios agentes que:

- buscan gimnasios locales con Google Search vía Serper,
- auditan presencia digital de un gimnasio,
- redactan un email comercial personalizado con IA.

El proyecto está pensado para trabajar con Gemini como modelo principal de IA.

## Tecnologías

- .NET 8
- C#
- Microsoft Semantic Kernel
- Google Gemini API
- Serper API

## Estructura del proyecto

- `Agents/` — agentes del sistema
- `Configuration/` — configuración de la aplicación
- `Models/` — modelos de dominio
- `Tools/` — plugins y utilidades (búsqueda, scraping)
- `Program.cs` — punto de entrada del sistema
- `appsettings.example.json` — ejemplo de configuración

## Requisitos

- .NET 8 SDK
- Credenciales válidas de:
  - Google AI Studio / Gemini
  - Serper

## Configuración

1. Copia `appsettings.example.json` a `appsettings.json`.
2. Completa tus claves reales:

```json
{
  "ApiSettings": {
    "GeminiApiKey": "TU_GEMINI_KEY",
    "GeminiModelId": "gemini-1.5-flash",
    "SerperApiKey": "TU_SERPER_KEY"
  }
}
```

3. Guarda el archivo localmente; no lo subas a Git.

## Ejecutar la aplicación

```bash
dotnet restore
dotnet run
```

## CI/CD

El repositorio incluye workflows de GitHub Actions para:

- CI: compilar al hacer push o pull request.
- CD: publicar el artefacto compilado.

Los archivos están en:

- `.github/workflows/ci.yml`
- `.github/workflows/cd.yml`

## Seguridad

El archivo `appsettings.json` queda ignorado por Git para no exponer llaves y secretos.

## Nota

Si las claves de Gemini o Serper no son válidas, la aplicación mostrará errores claros en consola indicando el problema.
