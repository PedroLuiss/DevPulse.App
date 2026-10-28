using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DevPulse.App.Models;
using DevPulse.App.Services;

namespace DevPulse.App.Controllers;

public class HomeController(
    CodeAuditorService codeAuditorService,
    IAiProviderSettingsStore settingsStore,
    ILogger<HomeController> logger) : Controller
{
    private static readonly HashSet<string> SupportedAuditTypes =
        ["Diagnosis", "Security", "Performance", "CleanCode"];

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new CodeAuditPageModel();
        await AddProviderStatusAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(
        CodeAuditPageModel model,
        CancellationToken cancellationToken)
    {
        if (!SupportedAuditTypes.Contains(model.Request.AuditType))
        {
            ModelState.AddModelError("Request.AuditType", "Selecciona un enfoque de análisis válido.");
        }

        if (!ModelState.IsValid)
        {
            await AddProviderStatusAsync(model, cancellationToken);
            return View("Index", model);
        }

        try
        {
            model.Result = await codeAuditorService.AuditCodeAsync(
                model.Request.SourceCode,
                model.Request.AuditType,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo completar el análisis de código.");
            model.ErrorMessage = "No se pudo completar el análisis. Verifica la configuración del proveedor, el modelo y la conexión.";
        }

        await AddProviderStatusAsync(model, cancellationToken);
        return View("Index", model);
    }

    private async Task AddProviderStatusAsync(CodeAuditPageModel model, CancellationToken cancellationToken)
    {
        var configuration = await settingsStore.GetAsync(cancellationToken);
        model.ActiveProvider = configuration.Provider;
        model.ActiveModel = configuration.Model;
        var isLocalEndpoint =
            Uri.TryCreate(configuration.BaseUrl, UriKind.Absolute, out var uri)
            && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
        model.IsProviderConfigured =
            (configuration.HasApiKey || isLocalEndpoint)
            && !string.IsNullOrWhiteSpace(configuration.BaseUrl)
            && !string.IsNullOrWhiteSpace(configuration.Model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
