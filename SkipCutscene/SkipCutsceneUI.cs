using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace SkipCutscene
{
    public unsafe class SkipCutsceneUI : Window, IDisposable
    {
        private readonly SkipCutscene plugin;

        public SkipCutsceneUI(SkipCutscene plugin)
          : base(
            "SkipCutscene##ConfigWindow",
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoCollapse
          )
        {
            this.plugin = plugin;

            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new Vector2(488, 0),
                MaximumSize = new Vector2(1000, 1000)
            };
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public override void Draw()
        {
            var enabled = plugin.Configuration.Enabled;
            if (ImGui.Checkbox("Enabled", ref enabled))
            {
                plugin.Configuration.Enabled = enabled;
                plugin.Configuration.Save();
                plugin.PrintState();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Enable or disable the cutscene skipping feature. This flag can be directly changed with the '/skipmsq' command.");
            }

            var skipBehaviour = plugin.Configuration._SkipBehaviour;

            if (ImGui.BeginCombo("Skip Behaviour", skipBehaviour.ToString()))
            {
                foreach (var value in Enum.GetValues(typeof(SkipBehaviour)))
                {
                    bool isSelected = skipBehaviour.Equals(value);
                    if (ImGui.Selectable(value.ToString(), isSelected))
                    {
                        skipBehaviour = (SkipBehaviour)value;
                        plugin.Configuration._SkipBehaviour = skipBehaviour;
                        plugin.Configuration.Save();
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Manual: You have to turn the skip on everytime you reboot or log out of the game, just like before.\n\nAutomatic: The skip will be turned on automatically if you are in a premade Light Party.\nOtherwise works the same way as 'Manual' option.\n\nPermanent: Turns the cutscene skipping on or off permanently, which persists between game reboots.\nWARNING: Be careful with this one.");
            }

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextUnformatted("Author:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedPink, SkipCutscene.Authors);

            ImGui.TextUnformatted("Discord:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedPink, "@no00ob");

            ImGui.TextUnformatted("Version:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedBlue, SkipCutscene.Version);

            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.Separator();

            ImGuiHelpers.ScaledDummy(5.0f);

            var debugMessages = plugin.Configuration.DebugMessages;
            if (ImGui.Checkbox("Print Debug Information", ref debugMessages))
            {
                plugin.Configuration.DebugMessages = debugMessages;
                plugin.Configuration.Save();
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            plugin.Configuration.IsVisible = false;
            plugin.Configuration.Save();
        }
    }
}