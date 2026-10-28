using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DevPulse.App.Models;

public class CodeAuditRequest
{
    [Required(ErrorMessage = "Pega un log, stack trace o fragmento de código.")]
    [StringLength(100_000, ErrorMessage = "El contenido no puede superar los 100.000 caracteres.")]
    public string SourceCode { get; set; } = string.Empty;

    [Required]
    public string AuditType { get; set; } = "Diagnosis";
}

public class CodeAuditResponse
{
    public int HealthScore { get; set; }
    public List<string> DetectedIssues { get; set; } = [];
    public string SecurityRisk { get; set; } = "Unknown";
    public string RefactoredCode { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
}

public class CodeAuditPageModel
{
    public CodeAuditRequest Request { get; set; } = new();
    public CodeAuditResponse? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string ActiveProvider { get; set; } = "Sin configurar";
    public string ActiveModel { get; set; } = string.Empty;
    public bool IsProviderConfigured { get; set; }
    public IReadOnlyList<SelectListItem> AuditTypes { get; } =
    [
        new("Diagnóstico de error", "Diagnosis"),
        new("Seguridad", "Security"),
        new("Rendimiento", "Performance"),
        new("Calidad de código", "CleanCode")
    ];
}
