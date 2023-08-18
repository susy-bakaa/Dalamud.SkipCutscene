using System;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace SkipCutscene;

public class SkipCutscene : IDalamudPlugin {
    private const short SkipValueEnabled = -28528;
    private const short SkipValueDisabledOffset1 = 13173;
    private const short SkipValueDisabledOffset2 = 6260;
    private bool _isCutsceneSkipEnabled;
    private readonly string[] _commandAliases = { "/skipcs", "/skipcutscene" };
    private readonly CommandInfo _commandInfo;
    private readonly CutsceneAddressResolver _cutsceneAddressResolver;

    public string Name => "SkipCutscene";

    [PluginService] private SigScanner SigScanner { get; set; }
    [PluginService] private CommandManager CommandManager { get; set; }
    [PluginService] private ChatGui ChatGui { get; set; }

    public SkipCutscene() {
        _isCutsceneSkipEnabled = false;
        _commandInfo = new CommandInfo(OnCutsceneSkipToggleCommand);
        _cutsceneAddressResolver = new CutsceneAddressResolver();
        _cutsceneAddressResolver.Setup(SigScanner);

        if (!_cutsceneAddressResolver.Valid) {
            PluginLog.Error("Cutscene offset not found.");
            PluginLog.Warning("Plugin disabling...");
            Dispose();
            return;
        }

        PluginLog.Information("Cutscene offsets found.");

        AddCommandAliases();
    }

    private void AddCommandAliases() {
        foreach (string alias in _commandAliases) {
            CommandManager.AddHandler(alias, _commandInfo);
        }
    }

    private void OnCutsceneSkipToggleCommand(string command, string arguments) {
        _isCutsceneSkipEnabled = !_isCutsceneSkipEnabled;
        SetCutsceneSkip(_isCutsceneSkipEnabled);

        var chatMessage = new XivChatEntry() {
            Type = XivChatType.Echo,
            Message = new SeStringBuilder()
                .AddUiForeground($"[{Name}] ", 45)
                .AddText("MSQ Cutscenes are now ")
                .AddItalics(_isCutsceneSkipEnabled ? "disabled" : "enabled")
                .AddText(".")
                .Build()
        };

        ChatGui.PrintChat(chatMessage);
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
