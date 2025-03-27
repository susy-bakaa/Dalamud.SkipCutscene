using System;
using System.Diagnostics;
using Dalamud.Game;
using Dalamud.Logging;

namespace SkipCutscene 
{
    public class CutsceneAddressResolver : BaseAddressResolver 
    {
        private const string Offset1Pattern = "75 33 48 8B 0D ?? ?? ?? ?? BA ?? 00 00 00 48 83 C1 10 E8 ?? ?? ?? ?? 83 78";
        private const string Offset2Pattern = "74 18 8B D7 48 8D 0D";
        private readonly nint _baseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;

        public bool Valid => Offset1 != IntPtr.Zero && Offset2 != IntPtr.Zero;
        public IntPtr Offset1 { get; private set; }
        public IntPtr Offset2 { get; private set; }

        protected override void Setup64Bit(ISigScanner sig) {
            Offset1 = sig.ScanText(Offset1Pattern);
            Offset2 = sig.ScanText(Offset2Pattern);
            LogOffsets();
        }

        private void LogOffsets() {
            long offset1FromBase = Offset1 - _baseAddress;
            long offset2FromBase = Offset2 - _baseAddress;

            Service.PluginLog.Information($"Offset1: [\"ffxiv_dx11.exe\"+{offset1FromBase:X}]");
            Service.PluginLog.Information($"Offset2: [\"ffxiv_dx11.exe\"+{offset2FromBase:X}]");
        }
    }
}