using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace SkipCutscene
{
    public class Service
    {
#pragma warning disable CS8618
        [PluginService] public static IFramework Framework { get; private set; }
        [PluginService] public static IChatGui ChatGui { get; private set; }
        [PluginService] public static IPluginLog PluginLog { get; private set; }
        [PluginService] public static IClientState ClientState { get; private set; }
#pragma warning restore CS8618
    }
}