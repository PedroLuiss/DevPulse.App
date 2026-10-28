using System.ComponentModel.DataAnnotations;

namespace DevPulse.App.Models;

public class AiProviderSettingsForm
{
    [Required(ErrorMessage = "Selecciona un proveedor.")]
    [StringLength(100)]
    public string Provider { get; set; } = "OpenAI";

    [Required(ErrorMessage = "Introduce la URL base compatible con OpenAI.")]
    [StringLength(500)]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    [Required(ErrorMessage = "Introduce el identificador del modelo.")]
    [StringLength(200)]
    public string Model { get; set; } = "gpt-4o-mini";

    [StringLength(2000)]
    [DataType(DataType.Password)]
    public string? ApiKey { get; set; }

    public bool RemoveSavedApiKey { get; set; }
}

public class AiProviderSettingsPageModel
{
    public AiProviderSettingsForm Settings { get; set; } = new();
    public bool HasSavedApiKey { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<AiProviderPreset> Presets { get; init; } = [];
}

public record AiProviderPreset(string Name, string BaseUrl, string SuggestedModel, string Note);

public class AiProviderConfiguration
{
    public string Provider { get; init; } = "OpenAI";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public string Model { get; init; } = "gpt-4o-mini";
    public string ApiKey { get; init; } = string.Empty;
    public bool HasApiKey => !string.IsNullOrEmpty(ApiKey);
}
