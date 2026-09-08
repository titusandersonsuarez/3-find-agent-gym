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
    "GeminiModelId": "gemini-3.6-flash",
    "SerperApiKey": "TU_SERPER_KEY",
    "MaxProspects": 5,
    "TargetCity": "Bucaramanga"
  }
}
```

`MaxProspects` limita cada ejecución a un máximo de 20 prospectos para controlar el consumo de APIs. `TargetCity` define la ciudad de búsqueda.

3. Guarda el archivo localmente; no lo subas a Git ni lo copies dentro de una imagen Docker.

## Ejecutar la aplicación

```bash
dotnet restore
dotnet run
```

La aplicación ejecuta un lote y termina por sí sola. Para una primera prueba, ejecútala manualmente; después puede desplegarse como un Azure Container Apps Job manual o programado.

## Ejecutar con Docker

Construye la imagen desde la raíz del proyecto:

```bash
docker build -t leadgym-ai .
```

Ejecuta el pipeline pasando las credenciales como variables de entorno:

```bash
docker run --rm -it \
  -e ApiSettings__GeminiApiKey="TU_GEMINI_KEY" \
  -e ApiSettings__GeminiModelId="gemini-3.6-flash" \
  -e ApiSettings__SerperApiKey="TU_SERPER_KEY" \
  leadgym-ai
```

En una plataforma cloud configura esas tres variables como secretos del servicio, no dentro del repositorio ni del Dockerfile. El proceso es una aplicación de consola que ejecuta el lote y termina; necesita un servicio de jobs o ejecución bajo demanda, no un hosting web que espere un puerto HTTP.

Cada ejecución guarda los correos generados como borradores pendientes en `data/email-drafts-AAAAmmdd-HHmmss.json`. El archivo contiene el prospecto, la auditoría, el mensaje personalizado y el estado `Pendiente`. En esta etapa todavía no se envían automáticamente.

Opcionalmente, al terminar el lote se envía ese JSON a tu correo para revisión. Para Gmail debes activar la verificación en dos pasos y crear una contraseña de aplicación. Configura estos valores como variables de entorno, especialmente la contraseña:

```powershell
$env:EmailSettings__Username = "titusandersonsuarez@gmail.com"
$env:EmailSettings__Password = "TU_CONTRASENA_DE_APLICACION_GMAIL"
$env:EmailSettings__From = "titusandersonsuarez@gmail.com"
$env:EmailSettings__To = "titusandersonsuarez@gmail.com"
dotnet run
```

También puedes configurarlos en tu plataforma cloud como secretos. El correo enviado contiene únicamente el JSON de borradores; no envía mensajes a los gimnasios.

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
