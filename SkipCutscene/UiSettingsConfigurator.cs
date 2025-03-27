using System.Diagnostics;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;

// Thank you to meoiswa for the original implementation borrowed from their plugin, Clarity in Chaos. https://github.com/meoiswa/ClarityInChaos
namespace SkipCutscene
{
    public unsafe class UiSettingsConfigurator
    {
        private readonly SkipCutscene plugin;
        private readonly GroupManager* groupManager;
        private GroupingSize lastGroupingSize = GroupingSize.Solo;

        public UiSettingsConfigurator(SkipCutscene plugin)
        {
            this.plugin = plugin;
            groupManager = GroupManager.Instance();
        }

        public GroupingSize GetCurrentGroupingSize()
        {
            var memberCount = groupManager->MainGroup.MemberCount;
            var allianceFlags = groupManager->MainGroup.AllianceFlags;

            var currentSize = memberCount switch
            {
                < 4 => GroupingSize.Solo,
                >= 4 and < 8 => GroupingSize.LightParty,
                _ when allianceFlags is not 0 => GroupingSize.Alliance,
                _ => GroupingSize.FullParty
            };

            return currentSize;
        }

        public void OnUpdate(IFramework framework)
        {
            if (plugin.Configuration._SkipBehaviour != SkipBehaviour.Automatic)
                return;

            if (plugin.IsOccupied)
                return;

            GroupingSize groupingSize = GetCurrentGroupingSize();

            if (groupingSize == lastGroupingSize)
                return;
            else
                lastGroupingSize = groupingSize;

            if (lastGroupingSize == GroupingSize.Solo)
            {
                plugin.Configuration.Enabled = false;
                plugin.Configuration.Save();
                plugin.PrintLog("Skipping disabled because you are not in a full party.", XivChatType.SystemMessage);
            }
            else
            {
                plugin.Configuration.Enabled = true;
                plugin.Configuration.Save();
                plugin.PrintLog("Skipping enabled because you are in a full party.", XivChatType.SystemMessage);
            }

            plugin.SetCutsceneSkip(plugin.Configuration.Enabled);
        }
    }
}
