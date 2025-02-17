using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Neat.Trainer.Modules.Config;

[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonSettings = new ()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    public static ConfigModel GetSettings(string configName)
    {
        var json = ReadJson(configName);
        var result = JsonSerializer.Deserialize<ConfigModel>(json, JsonSettings) ?? throw new JsonException("Failed to deserialize config");
        return result with
        {
            Name = configName,
        };
    }

    private static string ReadJson(string configName)
    {
        var path = Path.Combine(Constants.AssetsPath, "configs", $"{configName}.json5");
        if (!File.Exists(path)) throw new FileNotFoundException($"Config file not found: {path}");

        return File.ReadAllText(path);
    }
}
