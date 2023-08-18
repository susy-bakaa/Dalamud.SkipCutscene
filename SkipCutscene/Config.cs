using Dalamud.Configuration;

namespace SkipCutscene;

public class Config : IPluginConfiguration {
    public int Version { get; set; }
    public bool IsEnabled { get; set; } = true;
}