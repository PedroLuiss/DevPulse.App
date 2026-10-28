using System.Diagnostics;
using DevPulse.App.Models;
using DevPulse.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevPulse.App.Controllers;

public class AiSettingsController(
    IAiProviderSettingsStore settingsStore,
    CodeAuditorService codeAuditorService,
    ILogger<AiSettingsController> logger) : Controller
{
    private static readonly IReadOnlyList<AiProviderPreset> Presets =
    [
        new("OpenAI", "https://api.openai.com/v1", "gpt-4o-mini", "Proveedor con API key"),
        new("OpenRouter", "https://openrouter.ai/api/v1", "openai/gpt-4o-mini", "Catálogo de modelos; API key"),
        new("Groq", "https://api.groq.com/openai/v1", "llama-3.3-70b-versatile", "Modelos rápidos; API key"),
        new("DeepSeek", "https://api.deepseek.com/v1", "deepseek-chat", "API key"),
        new("Mistral", "https://api.mistral.ai/v1", "mistral-small-latest", "API key"),
        new("Together AI", "https://api.together.xyz/v1", "meta-llama/Meta-Llama-3.1-8B-Instruct-Turbo", "Modelos abiertos; API key"),
        new("Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-2.0-flash", "Compatibilidad OpenAI; API key"),
        new("Ollama (local)", "http://localhost:11434/v1", "llama3.2", "Servidor local; normalmente sin API key"),
        new("LM Studio (local)", "http://localhost:1234/v1", "modelo-local", "Servidor local; normalmente sin API key"),
        new("OpenAI-compatible (custom)", "https://", "", "Introduce la URL base, el modelo y la clave del proveedor")
    ];

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var configuration = await settingsStore.GetAsync(cancellationToken);
        return View(CreatePageModel(configuration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        AiProviderSettingsForm settings,
        bool testConnection,
        CancellationToken cancellationToken)
    {
        ValidateProviderSettings(settings);
        if (!ModelState.IsValid)
        {
            var existing = await settingsStore.GetAsync(cancellationToken);
            return View("Index", CreatePageModel(existing, settings));
        }

        try
        {
            await settingsStore.SaveAsync(settings, cancellationToken);
            var savedConfiguration = await settingsStore.GetAsync(cancellationToken);
            var pageModel = CreatePageModel(savedConfiguration);

            if (testConnection)
            {
                await codeAuditorService.TestConnectionAsync(cancellationToken);
                pageModel.StatusMessage = "Configuración guardada y conexión probada correctamente.";
            }
            else
            {
                pageModel.StatusMessage = "Configuración guardada. Ya puedes usarla para analizar código.";
            }

            return View("Index", pageModel);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo guardar o probar la configuración del proveedor de IA.");
            var savedConfiguration = await settingsStore.GetAsync(cancellationToken);
            var pageModel = CreatePageModel(savedConfiguration);
            pageModel.ErrorMessage = testConnection
                ? "La configuración se guardó, pero no se pudo probar la conexión. Verifica la URL, el modelo, la clave y el estado del proveedor."
                : "No se pudo guardar la configuración. Revisa los permisos de escritura del perfil local.";
            return View("Index", pageModel);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("~/Views/Home/Error.cshtml",
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void ValidateProviderSettings(AiProviderSettingsForm settings)
    {
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            ModelState.AddModelError(
                "Settings.BaseUrl",
                "Introduce una URL absoluta HTTP(S), sin usuario, contraseña, parámetros ni fragmento.");
            return;
        }

        if (uri.Scheme == Uri.UriSchemeHttp
            && !uri.IsLoopback
            && !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                "Settings.BaseUrl",
                "Para proveedores remotos usa HTTPS. HTTP solo está permitido para servidores locales.");
        }
    }

    private static AiProviderSettingsPageModel CreatePageModel(
        AiProviderConfiguration configuration,
        AiProviderSettingsForm? submitted = null)
    {
        return new AiProviderSettingsPageModel
        {
            Settings = submitted ?? new AiProviderSettingsForm
            {
                Provider = configuration.Provider,
                BaseUrl = configuration.BaseUrl,
                Model = configuration.Model
            },
            HasSavedApiKey = configuration.HasApiKey,
            Presets = Presets
        };
    }
}
