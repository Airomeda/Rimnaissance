using HarmonyLib;
using System;
using Verse;

namespace Rimnaissance
{
    public class RimnaissanceCoreMod : Mod
    {
        //private static readonly Type patchType = typeof(HarmonyPatches);
        public RimnaissanceCoreMod(ModContentPack content) : base(content)
        {
            Log.Message("<color=yellow>[Rimnaissance]</color> Enlightened by <i>Rimnaissance!</i>");

            //Settings = GetSettings<EnlightenedSettings>();

            var enlightenment = new Harmony("com.RimnaissanceCore");
            enlightenment.PatchAll();
        }
    }
}
