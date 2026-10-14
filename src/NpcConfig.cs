using Newtonsoft.Json;
using Terraria.ID;

namespace NoDieNPC;

public sealed class NpcConfig
{
    public bool Freeze { get; set; } = false;

    // Preserve the original plugin's "protected by default" behavior.
    public bool GodMode { get; set; } = true;

    // Guide / Clothier deaths are used by vanilla progression rituals.
    public List<int> GodModeExemptNpcTypes { get; set; } =
        new() { NPCID.Guide, NPCID.Clothier };

    public static NpcConfig LoadOrDefault(string path)
    {
        if (TryLoad(path, out var config, out string? error))
            return config;

        Console.Error.WriteLine($"[NoDieNPC] Invalid config ({error}); using safe defaults.");
        return new NpcConfig();
    }

    public static bool TryLoad(string path, out NpcConfig config, out string? error)
    {
        config = new NpcConfig();
        error = null;
        try
        {
            if (!File.Exists(path))
            {
                config.Save(path);
                return true;
            }

            NpcConfig? loaded = JsonConvert.DeserializeObject<NpcConfig>(File.ReadAllText(path));
            if (loaded == null || loaded.GodModeExemptNpcTypes == null)
                throw new JsonException("Config root or GodModeExemptNpcTypes must not be null.");

            config = loaded;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Write complete JSON to a temporary file, then replace the original
        // to reduce the chance of leaving half-written settings after a crash.
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonConvert.SerializeObject(this, Formatting.Indented));
        File.Move(temp, path, true);
    }
}
