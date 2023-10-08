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
using Dalamud.Plugin.Services;

namespace SkipCutscene {

public class SkipCutscene : IDalamudPlugin {

    private const short SkipValueEnabled = -28528;
    private const short SkipValueDisabledOffset1 = 13173;
    private const short SkipValueDisabledOffset2 = 6260;
    private bool _isCutsceneSkipEnabled;
    private readonly string[] _commandAliases = { "/skipcs", "/skipcut", "/skipcutscene" };
    private readonly CommandInfo _commandInfo;
    private readonly CutsceneAddressResolver _cutsceneAddressResolver;

    public string Name => "SkipCutscene";

    [PluginService] private ISigScanner SigScanner { get; set; }
    [PluginService] private ICommandManager CommandManager { get; set; }
    [PluginService] private IChatGui ChatGui { get; set; }
    [PluginService] public static IPluginLog PluginLog { get; set; }

    public SkipCutscene() 
    {
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

    private void AddCommandAliases() 
    {
        foreach (string alias in _commandAliases) 
        {
            CommandManager.AddHandler(alias, _commandInfo);
        }
    }

    private void OnCutsceneSkipToggleCommand(string command, string arguments) 
    {

        if (arguments.ToLower().Contains("on") || arguments.ToLower().Contains("yes"))
        {
            _isCutsceneSkipEnabled = true;
        } 
        else if (arguments.ToLower().Contains("off") || arguments.ToLower().Contains("no"))
        {
            _isCutsceneSkipEnabled = false;
        }
        else
        {
            _isCutsceneSkipEnabled = !_isCutsceneSkipEnabled;
        }

        SetCutsceneSkip(_isCutsceneSkipEnabled);

        var chatMessage = new XivChatEntry() {
            Type = XivChatType.SystemMessage,
            Message = new SeStringBuilder()
                .AddUiForeground($"[{Name}] ", 45)
                .AddText("MSQ Roulette Cutscenes will ")
                .AddItalics(_isCutsceneSkipEnabled ? "NOT PLAY" : "PLAY")
                .AddText(" now.")
                .Build()
        };

        ChatGui.Print(chatMessage);
    }

    private void SetCutsceneSkip(bool enabled) 
    {
        SafeMemory.Write(_cutsceneAddressResolver.Offset1, enabled ? SkipValueEnabled : SkipValueDisabledOffset1);
        SafeMemory.Write(_cutsceneAddressResolver.Offset2, enabled ? SkipValueEnabled : SkipValueDisabledOffset2);
    }

    public void Dispose() 
    {
        SetCutsceneSkip(false);
        GC.SuppressFinalize(this);
    }
}
}