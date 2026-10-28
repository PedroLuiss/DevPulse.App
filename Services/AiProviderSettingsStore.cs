using System.Text.Json;
using DevPulse.App.Models;
using Microsoft.AspNetCore.DataProtection;

namespace DevPulse.App.Services;

public interface IAiProviderSettingsStore
{
    Task<AiProviderConfiguration> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AiProviderSettingsForm form, CancellationToken cancellationToken = default);
}

public sealed class AiProviderSettingsStore : IAiProviderSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDataProtector _protector;
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public AiProviderSettingsStore(
        IDataProtectionProvider protectionProvider,
        IWebHostEnvironment environment)
    {
        _protector = protectionProvider.CreateProtector("DevPulse.AiProviderSettings.v1");
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(appDataPath))
        {
            appDataPath = environment.ContentRootPath;
        }

        _settingsPath = Path.Combine(appDataPath, "DevPulse", "ai-settings.json");
    }

    public async Task<AiProviderConfiguration> GetAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var stored = await ReadStoredAsync(cancellationToken);
            return ToConfiguration(stored);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAsync(AiProviderSettingsForm form, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var previous = await ReadStoredAsync(cancellationToken);
            var protectedApiKey = form.RemoveSavedApiKey
                ? null
                : !string.IsNullOrWhiteSpace(form.ApiKey)
                    ? _protector.Protect(form.ApiKey.Trim())
                    : previous.ProtectedApiKey;

            var stored = new StoredSettings
            {
                Provider = form.Provider.Trim(),
                BaseUrl = form.BaseUrl.Trim().TrimEnd('/'),
                Model = form.Model.Trim(),
                ProtectedApiKey = protectedApiKey
            };

            var directory = Path.GetDirectoryName(_settingsPath)
                ?? throw new InvalidOperationException("No se pudo determinar la carpeta de configuración.");
            Directory.CreateDirectory(directory);

            var temporaryPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(stream, stored, JsonOptions, cancellationToken);
                }

                File.Move(temporaryPath, _settingsPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<StoredSettings> ReadStoredAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return new StoredSettings();
        }

        await using var stream = new FileStream(
            _settingsPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<StoredSettings>(stream, JsonOptions, cancellationToken)
            ?? new StoredSettings();
    }

    private AiProviderConfiguration ToConfiguration(StoredSettings stored)
    {
        var apiKey = string.IsNullOrEmpty(stored.ProtectedApiKey)
            ? string.Empty
            : _protector.Unprotect(stored.ProtectedApiKey);

        return new AiProviderConfiguration
        {
            Provider = stored.Provider,
            BaseUrl = stored.BaseUrl,
            Model = stored.Model,
            ApiKey = apiKey
        };
    }

    private sealed class StoredSettings
    {
        public string Provider { get; init; } = "OpenAI";
        public string BaseUrl { get; init; } = "https://api.openai.com/v1";
        public string Model { get; init; } = "gpt-4o-mini";
        public string? ProtectedApiKey { get; init; }
    }
}
