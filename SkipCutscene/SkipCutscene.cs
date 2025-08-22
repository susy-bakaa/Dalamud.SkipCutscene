using System;
using System.Linq;
using System.Reflection;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using InteropGenerator.Runtime;

namespace SkipCutscene 
{
    public sealed class SkipCutscene : IDalamudPlugin
    {
        public string Name => "SkipCutscene";

        private const string commandName = "/sc";

        public const string Authors = "Windmourn, 0x526f6d656f, susy_baka";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

        public IDalamudPluginInterface PluginInterface { get; init; }
        public ICommandManager CommandManager { get; init; }
        public ISigScanner SigScanner { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem { get; init; }
        public SkipCutsceneUI Window { get; init; }
        public UiSettingsConfigurator Configurator { get; init; }

        public bool IsOccupied => Service.Condition[ConditionFlag.BoundByDuty]
                            || Service.Condition[ConditionFlag.BetweenAreas]
                            || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                            || Service.Condition[ConditionFlag.WaitingForDuty]
                            || Service.Condition[ConditionFlag.LoggingOut]
                            || Service.Condition[ConditionFlag.InDutyQueue]
                            || Service.Condition[ConditionFlag.BetweenAreas51]
                            || Service.Condition[ConditionFlag.BoundByDuty56]
                            || Service.Condition[ConditionFlag.BoundByDuty95];

        private const short SkipValueEnabled = -28528;
        private const short SkipValueDisabledOffset1 = 14709;
        private const short SkipValueDisabledOffset2 = 6260;
        private bool foundAddress = false;
        private readonly CutsceneAddressResolver _cutsceneAddressResolver;
        private string[] positiveStrings = ["on", "yes", "true", "y", "t"];
        private string[] negativeStrings = ["off", "no", "false", "n", "f"];

        public SkipCutscene(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, ISigScanner sigScanner)
        {
            pluginInterface.Create<Service>();

            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            SigScanner = sigScanner;
            WindowSystem = new("SkipCutscene");

            // Resolve the adress for the cutscene skip
            _cutsceneAddressResolver = new CutsceneAddressResolver();
            _cutsceneAddressResolver.Setup(SigScanner);

            if (!_cutsceneAddressResolver.Valid)
            {
                Service.PluginLog.Error("Cutscene offset not found.");
                PrintLog("Cutscene offset not found.", XivChatType.ErrorMessage);
                foundAddress = false;
            }
            else
            {
                foundAddress = true;
            }

            // Setup configuration
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Configurator = new UiSettingsConfigurator(this);

            Window = new SkipCutsceneUI(this)
            {
                IsOpen = Configuration.IsVisible
            };

            WindowSystem.AddWindow(Window);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Toggles the status of cutscenes.\n/sc [on|off] → Toggles the status of cutscenes to specified state.\n/sc config → Opens the configuration window.",
                ShowInHelp = true
            });

            Service.Framework.Update += Configurator.OnUpdate;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Service.ClientState.Logout += Logout;

            Service.PluginLog.Information("Cutscene offsets found.");
            PrintLog("Cutscene offsets found", XivChatType.SystemMessage);

            if (Configuration._SkipBehaviour != SkipBehaviour.Permanent)
            {
                Configuration.Enabled = false;
                Configuration.Save();
            }

            if (foundAddress)
                SetCutsceneSkip(Configuration.Enabled);
        }

        private void OnCommand(string command, string args)
        {
            var subcommands = args.Split(' ');

            var firstArg = subcommands[0];

            if (firstArg != null && firstArg.Length > 0)
            {
                if (firstArg.ToLower() == "settings" || firstArg.ToLower() == "config" || firstArg.ToLower() == "c")
                {
                    SetVisible(!Configuration.IsVisible);
                    return;
                }

                if (positiveStrings.Contains(firstArg))
                {
                    Configuration.Enabled = true;
                    Configuration.Save();
                }
                else if (negativeStrings.Contains(firstArg))
                {
                    Configuration.Enabled = false;
                    Configuration.Save();
                }
                else
                {
                    Configuration.Enabled = !Configuration.Enabled;
                    Configuration.Save();
                }
            }
            else
            {
                Configuration.Enabled = !Configuration.Enabled;
                Configuration.Save();
            }

            if (foundAddress)
                SetCutsceneSkip(Configuration.Enabled);

            PrintState();
        }

        public void SetCutsceneSkip(bool enabled) 
        {
            if (!foundAddress)
                return;

            SafeMemory.Write(_cutsceneAddressResolver.Offset1, enabled ? SkipValueEnabled : SkipValueDisabledOffset1);
            SafeMemory.Write(_cutsceneAddressResolver.Offset2, enabled ? SkipValueEnabled : SkipValueDisabledOffset2);
        }

        public void Dispose() 
        {
            SetCutsceneSkip(false);

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

            Service.Framework.Update -= Configurator.OnUpdate;
            Service.ClientState.Logout -= Logout;

            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(commandName);

            GC.SuppressFinalize(this);
        }

        public void PrintLog(string msg, XivChatType type)
        {
            if (!Configuration.DebugMessages)
            {
                return;
            }

            SeString seString;

            switch (type)
            {
                case XivChatType.ErrorMessage:
                    seString = new SeStringBuilder()
                        .AddUiForeground($"[{Name}] {msg}!", 1)
                        .AddText($" ")
                        .Build();
                    break;
                default:
                    seString = new SeStringBuilder()
                        .AddUiForeground($"[{Name}] ", 45)
                        .AddText($"{msg}.")
                        .Build();
                    break;
            }

            var chatMessage = new XivChatEntry()
            {
                Type = type,
                Message = seString
            };

            Service.ChatGui.Print(chatMessage);
        }

        public void PrintState()
        {
            var chatMessage = new XivChatEntry()
            {
                Type = XivChatType.SystemMessage,
                Message = new SeStringBuilder()
                    .AddUiForeground($"[{Name}] ", 45)
                    .AddText("MSQ Roulette Cutscenes will 》 ")
                    .AddItalics(Configuration.Enabled ? "NOT PLAY" : "PLAY")
                    .AddText(" now.")
                    .Build()
            };

            Service.ChatGui.Print(chatMessage);
        }

        private void SetVisible(bool isVisible)
        {
            Configuration.IsVisible = isVisible;
            Configuration.Save();

            Window.IsOpen = Configuration.IsVisible;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            SetVisible(!Configuration.IsVisible);
        }

        private void Logout(int i, int e)
        {
            if (Configuration._SkipBehaviour != SkipBehaviour.Permanent)
            {
                Configuration.Enabled = false;
                Configuration.Save();
                if (foundAddress)
                    SetCutsceneSkip(false);
            }
        }
    }
}