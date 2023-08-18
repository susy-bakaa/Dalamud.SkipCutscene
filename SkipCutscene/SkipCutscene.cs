using System;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace SkipCutscene;

public class SkipCutscene : IDalamudPlugin {
    private const short SkipValueEnabled = -28528;
    private const short SkipValueDisabledOffset1 = 13173;
    private const short SkipValueDisabledOffset2 = 6260;
    private readonly Config _config;
    private readonly CutsceneAddressResolver _cutsceneAddressResolver;

    public string Name => "SkipCutscene";

    [PluginService] private DalamudPluginInterface Interface { get; set; }
    [PluginService] private SigScanner SigScanner { get; set; }
    [PluginService] private CommandManager CommandManager { get; set; }
    [PluginService] private ChatGui ChatGui { get; set; }

    public SkipCutscene() {
        _config = GetConfig();
        _cutsceneAddressResolver = new CutsceneAddressResolver();
        _cutsceneAddressResolver.Setup(SigScanner);

        if (!_cutsceneAddressResolver.Valid) {
            PluginLog.Error("Cutscene offset not found.");
            PluginLog.Warning("Plugin disabling...");
            Dispose();
            return;
        }

        PluginLog.Information("Cutscene offsets found.");
        if (_config.IsEnabled) SetCutsceneSkip(true);

        CommandManager.AddHandler("/skipcs", new CommandInfo(OnCutsceneSkipToggleCommand) {
            HelpMessage = "/skipcs: Toggle MSQ cutscene skip."
        });
    }

    private Config GetConfig() {
        if (Interface.GetPluginConfig() is not Config config || config.Version == 0) {
            config = new Config {
                IsEnabled = true,
                Version = 1
            };
        }

        return config;
    }

    private void OnCutsceneSkipToggleCommand(string command, string arguments) {
        bool wasEnabled = _config.IsEnabled;

        string message = wasEnabled
            ? $"MSQ Cutscenes are now disabled."
            : $"MSQ Cutscenes are now enabled.";

        ChatGui.Print(message);

        _config.IsEnabled = !wasEnabled;
        SetCutsceneSkip(_config.IsEnabled);
        Interface.SavePluginConfig(_config);
    }

    private void SetCutsceneSkip(bool enabled) {
        SafeMemory.Write(_cutsceneAddressResolver.Offset1, enabled ? SkipValueEnabled : SkipValueDisabledOffset1);
        SafeMemory.Write(_cutsceneAddressResolver.Offset2, enabled ? SkipValueEnabled : SkipValueDisabledOffset2);
    }

    public void Dispose() {
        SetCutsceneSkip(false);
        GC.SuppressFinalize(this);
    }
}
