using Neat.Trainer.Modules.Config;
namespace Neat.Viewer.Modules.Storage;

public static class ConfigsService
{
    public static IReadOnlyCollection<ConfigModel> GetConfigs()
    {
        var files = Directory.GetFiles(Path.Combine(Constants.AssetsPath, "configs"), "*.json5");
        return files
            .Select(x => ConfigService.GetSettings(Path.GetFileNameWithoutExtension(x)))
            .Where(x => !x.Name.StartsWith('_'))
            .OrderBy(x => x.Name)
            .ToList();
    }
}
