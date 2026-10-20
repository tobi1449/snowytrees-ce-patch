using System;
using System.Collections.Generic;
using HarmonyLib;
using Nandonalt_SnowyTrees;
using Verse;

namespace SnowyTreesCEPatch;

/// <summary>
/// Patches Combat Extended's BoundsInjector so that the extra graphics
/// provided by Nandonalt's Snowy Trees (immature, leafless, polluted, etc.)
/// are included in CE's plant bounds generation. Without this, CE falls back
/// to incorrect bounds whenever a snowy plant switches to one of those states.
/// </summary>
public static class BoundsInjectorPatches
{
    private readonly static Action<Graphic, HashSet<UnityEngine.Texture2D>> CollectGraphic =
        AccessTools.MethodDelegate<Action<Graphic, HashSet<UnityEngine.Texture2D>>>(
            AccessTools.Method(typeof(CombatExtended.BoundsInjector), "CollectGraphic"));

    /// <summary>
    /// A. Postfix on BoundsInjector.CollectTextures: adds the textures of all
    /// Snowy Trees plant graphics to the collected texture set so that CE's
    /// async GPU texture cache contains them before bounds injection runs.
    /// </summary>
    [HarmonyPatch(typeof(CombatExtended.BoundsInjector), "CollectTextures")]
    [HarmonyPostfix]
    public static void CollectTextures_Postfix(ref HashSet<UnityEngine.Texture2D> __result)
    {
        foreach (var snowyDef in DefDatabase<SnowyPlantDef>.AllDefs)
        {
            if (snowyDef == null)
            {
                continue;
            }

            foreach (PlantGraphic plantGraphic in Enum.GetValues<PlantGraphic>())
            {
                if (!snowyDef.HasPlantGraphic(plantGraphic))
                {
                    continue;
                }

                var graphic = snowyDef.GetGraphic(plantGraphic);
                if (graphic == null)
                {
                    continue;
                }

                CollectGraphic(graphic, __result);
            }
        }
    }

    /// <summary>
    /// B. Postfix on BoundsInjector.InjectPlants: pre-generates bounds for every
    /// Snowy Trees graphic belonging to the injected plant ThingDef, using the
    /// same GraphicType.Plant treatment CE applies to the vanilla plant graphics.
    /// </summary>
    [HarmonyPatch(typeof(CombatExtended.BoundsInjector), "InjectPlants")]
    [HarmonyPostfix]
    public static void InjectPlants_Postfix(ThingDef plantDef)
    {
        var snowyDef = DefDatabase<SnowyPlantDef>.GetNamed(plantDef.defName, errorOnFail: false);
        if (snowyDef == null)
        {
            return;
        }

        foreach (var plantGraphic in Enum.GetValues<PlantGraphic>())
        {
            if (!snowyDef.HasPlantGraphic(plantGraphic))
            {
                continue;
            }

            var graphic = snowyDef.GetGraphic(plantGraphic);
            if (graphic == null)
            {
                continue;
            }

            try
            {
                CombatExtended.BoundsInjector.BoundMap(graphic, CombatExtended.BoundsInjector.GraphicType.Plant);
            }
            catch (Exception e)
            {
                Log.Error($"Snowy Trees CE Patch :: Failed to inject bounds for {plantDef.defName} ({plantGraphic}): {e}");
            }
        }
    }
}