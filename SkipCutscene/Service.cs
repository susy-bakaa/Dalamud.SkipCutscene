using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkipCutscene; 
public sealed class Service {
    [PluginService] public static DalamudPluginInterface Interface { get; set; }
    [PluginService] public static SigScanner SigScanner { get; set; }
    [PluginService] public static ChatGui ChatGui { get; set; }
}
