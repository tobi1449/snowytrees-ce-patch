using HarmonyLib;
using Verse;

namespace SnowyTreesCEPatch;

public class SnowyTreesCePatchMod : Mod
{
    public SnowyTreesCePatchMod(ModContentPack content)
        : base(content)
    {
        LongEventHandler.QueueLongEvent(() =>
        {
            var harmony = new Harmony("tobi1449.snowytreescepatch");
            harmony.PatchAll();
        }, "Load SnowyTrees textures for CE", false, null);
    }
}