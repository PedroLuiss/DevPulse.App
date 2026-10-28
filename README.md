# DevPulse

**DevPulse** es una aplicación web ASP.NET Core MVC para analizar fragmentos de código, logs y stack traces con un modelo de inteligencia artificial. Presenta un resumen, hallazgos, una estimación del nivel de riesgo y, cuando hay contexto suficiente, una propuesta de código corregido.

La aplicación permite seleccionar un proveedor y un modelo desde una pantalla independiente. Puedes conectar servicios en la nube compatibles con la API de chat de OpenAI o modelos locales servidos por Ollama y LM Studio.

> El diagnóstico y la puntuación son sugerencias generadas por IA: no sustituyen una revisión técnica ni una auditoría de seguridad profesional.

## Funcionalidades

- Análisis orientado a diagnóstico, seguridad, rendimiento o calidad de código.
- Informe con puntuación de salud, hallazgos, nivel de riesgo y propuesta de código.
- Página **Configurar IA** para seleccionar proveedor, URL base y nombre exacto del modelo.
- Botón para guardar la configuración y probar la conexión.
- Soporte para API keys y proveedores locales que no requieren una clave.
- Almacenamiento local cifrado de la API key mediante ASP.NET Core Data Protection.
- Validación de entradas y manejo de errores.

## Proveedores

La aplicación incluye valores iniciales para:

| Proveedor | Tipo de conexión |
|---|---|
| OpenAI | API compatible con OpenAI |
| OpenRouter | API compatible con OpenAI |
| Groq | API compatible con OpenAI |
| DeepSeek | API compatible con OpenAI |
| Mistral | API compatible con OpenAI |
| Together AI | API compatible con OpenAI |
| Google Gemini | Endpoint compatible con OpenAI |
| Ollama | Servidor de modelos local |
| LM Studio | Servidor de modelos local |
| OpenAI-compatible (custom) | URL y modelo personalizados |

La integración genérica envía solicitudes a `POST /chat/completions` e incluye la API key como Bearer token cuando se proporciona. La compatibilidad y los nombres de modelos disponibles pueden variar: comprueba la documentación y la cuenta del proveedor antes de configurar la aplicación.

Los planes gratuitos, límites de uso y requisitos de facturación dependen de cada proveedor y pueden cambiar. DevPulse no proporciona ni crea cuentas o API keys. Las APIs propietarias que no implementan este formato, así como los flujos OAuth específicos, no están incluidos.

## Requisitos

- Windows, macOS o Linux.
- .NET SDK 10.0 o posterior compatible con `net10.0`.
- Un proveedor compatible configurado en la página de ajustes:
  - Para un proveedor en la nube, una cuenta, un modelo habilitado y una API key si el proveedor la requiere.
  - Para un proveedor local, el servidor instalado y en ejecución, con el modelo descargado/cargado.

Puedes comprobar el SDK instalado con:

```powershell
dotnet --info
```

## Instalación

### Descargar el proyecto

Clona el repositorio y entra en la carpeta del proyecto:

```powershell
git clone https://github.com/PedroLuiss/DevPulse.App.git
cd DevPulse.App
```

Si ya tienes el código fuente, abre una terminal en la carpeta que contiene `DevPulse.App.csproj`.

### Restaurar paquetes

```powershell
dotnet restore
```

## Ejecutar la aplicación

Para iniciar la aplicación usando el perfil HTTPS definido por el proyecto:

```powershell
dotnet run --launch-profile https
```

Abre la dirección que aparece en la terminal. Con la configuración inicial del proyecto normalmente es:

```text
https://localhost:7030
```

Si el sistema indica que el certificado HTTPS de desarrollo no es de confianza, confírmalo con:

```powershell
dotnet dev-certs https --trust
```

También puedes usar el perfil HTTP:

```powershell
dotnet run --launch-profile http
```

En ese caso, la dirección definida actualmente es `http://localhost:5243`. Para detener el servidor, vuelve a la terminal y presiona `Ctrl+C`.

Para recargar la aplicación automáticamente durante el desarrollo:

```powershell
dotnet watch run --launch-profile https
```

## Configurar un proveedor de IA

1. Inicia DevPulse y abre **Configurar IA** en la navegación, o visita `/AiSettings`.
2. Selecciona un proveedor de la lista.
3. Comprueba la **URL base de la API**. Si usas un proveedor personalizado, introduce la URL base que documenta ese servicio; la aplicación añade `/chat/completions`.
4. Introduce el **identificador exacto del modelo** habilitado para tu cuenta o disponible en tu servidor local.
5. Si el proveedor requiere una clave, pégala en **API key**. Para servidores locales puede dejarse vacía.
6. Selecciona **Guardar y probar** para verificar la conexión. También puedes guardar sin probar y analizar más tarde.
7. Vuelve al analizador, pega un log, stack trace o código y elige el enfoque del análisis.

Al cambiar de proveedor, la clave anterior se conserva si dejas vacío el campo de API key. Para reemplazarla, introduce una nueva; para eliminarla, marca **Eliminar la clave guardada** y guarda la configuración.

### Ejemplo con Ollama

Instala Ollama, descarga un modelo y asegúrate de que el servicio esté en ejecución. Por ejemplo:

```powershell
ollama pull llama3.2
```

Después, en **Configurar IA**, selecciona **Ollama (local)**, usa el modelo `llama3.2` y guarda la conexión. La URL local sugerida es `http://localhost:11434/v1`.

### Ejemplo con LM Studio

Descarga o carga un modelo en LM Studio e inicia su servidor local compatible con OpenAI. Selecciona **LM Studio (local)**, revisa la URL sugerida (`http://localhost:1234/v1`) y escribe el identificador de modelo que el servidor espera.

## Almacenamiento y privacidad

- La configuración se guarda en el perfil local del usuario, en:
  - Windows: `%LOCALAPPDATA%\DevPulse\ai-settings.json`
  - Otros sistemas: la carpeta de datos locales del usuario, dentro de `DevPulse/ai-settings.json`.
- La API key se protege con ASP.NET Core Data Protection. No se guarda en `appsettings.json` ni se muestra nuevamente en la interfaz.
- Las claves de protección de ASP.NET Core se mantienen en el perfil local de la máquina. Al cambiar de equipo o perfil, la clave almacenada podría no ser descifrable; configura de nuevo el proveedor en ese caso.
- El código o log enviado se transmite al proveedor seleccionado para generar el análisis. No pegues secretos, tokens, datos personales ni código que no tengas autorización para compartir.
- Esta versión no implementa autenticación de usuarios o administración. Úsala localmente o en una red privada de confianza; no la publiques directamente en Internet.

## Estructura principal

```text
Controllers/
  AiSettingsController.cs       Pantalla y acciones de configuración del proveedor
  HomeController.cs             Analizador y página principal
Models/
  AiProviderSettingsModels.cs   Modelos de configuración de IA
  CodeAuditModel.cs             Solicitud, resultado y vista del análisis
Services/
  AiProviderSettingsStore.cs     Persistencia local cifrada de la configuración
  CodeAuditorService.cs          Conexión con el proveedor y análisis de IA
Views/
  AiSettings/Index.cshtml        Formulario para configurar y probar el proveedor
  Home/Index.cshtml              Analizador e informe
wwwroot/
  css/site.css                   Estilos de la aplicación
Program.cs                       Registro de servicios y rutas MVC
```

## Compilar y comprobar

Desde la carpeta del proyecto:

```powershell
dotnet build
```

Para ejecutar las pruebas, si se agregan al proyecto:

```powershell
dotnet test
```

## Solución de problemas

### No se puede conectar al proveedor

- Pulsa **Guardar y probar** para confirmar que el proveedor responde.
- Revisa la URL base y el identificador exacto del modelo.
- Verifica que la API key sea válida y que tu cuenta tenga acceso al modelo.
- Asegúrate de que el servidor local de Ollama o LM Studio esté en ejecución y que el modelo esté instalado/cargado.
- Los servicios remotos deben usar HTTPS. HTTP está permitido para direcciones locales.

### Error al compilar

Confirma que tienes instalado el SDK que soporta .NET 10 y ejecuta:

```powershell
dotnet restore
dotnet build
```

### El resultado no es el esperado

Proporciona contexto suficiente y elimina datos sensibles antes de enviar el contenido. La precisión depende del modelo, del prompt y de la información disponible; revisa siempre las recomendaciones antes de aplicar cambios.

## Tecnologías

- ASP.NET Core MVC sobre .NET 10.
- C# y Razor.
- `Microsoft.Extensions.AI` y `Microsoft.Extensions.AI.OpenAI` están incluidos como dependencias del proyecto.
- ASP.NET Core Data Protection para proteger la API key local.

## Licencia

No se ha especificado una licencia en este repositorio. Antes de redistribuir o reutilizar el proyecto, confirma los derechos y añade una licencia si corresponde.
