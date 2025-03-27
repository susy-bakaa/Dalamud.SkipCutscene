using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace SkipCutscene
{
    [Serializable]
    public unsafe class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool IsVisible { get; set; } = true;
        public bool Enabled { get; set; } = false;
        public SkipBehaviour _SkipBehaviour { get; set; } = SkipBehaviour.Manual;

        public bool DebugMessages = false;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
