using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Neat.Core.Species;
using Neat.Trainer.Modules.Config;
namespace Neat.Trainer.Modules.Storage;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
public class StorageService : IDisposable
{
    private static readonly JsonSerializerOptions JsonSettings = new () { WriteIndented = true };
    private readonly Timer _flushTimer;
    private readonly string _storageName;
    private bool _busy;
    private StorageGenData? _pending;

    public StorageService(ConfigModel configModel)
    {
        _storageName = configModel.Name;
        _flushTimer = new Timer(Flush, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        Flush();
        _flushTimer.Dispose();
    }

    public StorageGenData ReadGenData()
    {
        var json = ReadJson(_storageName);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StorageGenData
            {
                Species = [],
                SpeciesThreshold = null,
                Iteration = 0,
            };
        }

        var data = JsonSerializer.Deserialize<StorageGenData>(json) ?? throw new JsonException("Failed to deserialize config");
        return data;
    }

    public void WriteGenomes(int iteration, float? speciesThreshold, IReadOnlyCollection<Specie> species)
    {
        _pending = new StorageGenData
        {
            Iteration = iteration,
            SpeciesThreshold = speciesThreshold,
            Species = species,
        };
    }

    public void Flush(object? state = null)
    {
        if (_busy && state != (object?) true) return;
        try
        {
            _busy = true;
            if (_pending == null) return;

            var path = GetPath(_storageName);
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory!);

            var json = JsonSerializer.Serialize(_pending, JsonSettings);
            File.WriteAllText(path, json);
            _pending = null;
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to flush storage: {Message}", ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private static string? ReadJson(string storageName)
    {
        var path = GetPath(storageName);
        if (!File.Exists(path)) return null;
        return File.ReadAllText(path);
    }

    private static string GetPath(string storageName) =>
        Path.Combine(Constants.AssetsPath, "genomes", $"{storageName}.json5");

    // Path.Combine(Directory.GetCurrentDirectory(), "assets", "genomes", $"{storageName}.json5");
}
