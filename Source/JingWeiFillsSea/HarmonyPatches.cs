// ==============================================================================
// 精卫填海 - Harmony 补丁
// ==============================================================================
//
// 与愚公填山的 Harmony patch 结构完全对称，只是 defName 改为 "JWFH_WaterFiller"。
//
// Patch 1: 让原版 Construction workgiver 跳过我们的水体填充标记
// Patch 2: 让我们的标记建筑的 WorkToBuild stat 受 Mod 设置倍率影响
// ==============================================================================

using HarmonyLib;
using Verse;
using RimWorld;

namespace JWFH
{
    // Patch 1: 让原版 WorkGiver_ConstructFinishFrames 跳过我们的水体填充标记
    [HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), "HasJobOnThing")]
    public static class Patch_WorkGiver_ConstructFinishFrames_HasJobOnThing
    {
        public static void Postfix(ref bool __result, Thing t)
        {
            if (!__result) return;

            if (t is Frame frame)
            {
                ThingDef buildDef = frame.def.entityDefToBuild as ThingDef;
                if (buildDef != null && buildDef.defName == "JWFH_WaterFiller")
                {
                    __result = false;
                }
            }
        }
    }

    // Patch 2: 让 WorkToBuild stat 受 Mod 设置倍率影响
    [HarmonyPatch(typeof(StatExtension), "GetStatValue")]
    public static class Patch_StatExtension_GetStatValue
    {
        private static ThingDef _waterFillerDef;
        private static ThingDef WaterFillerDef
        {
            get
            {
                if (_waterFillerDef == null)
                {
                    _waterFillerDef = DefDatabase<ThingDef>.GetNamed("JWFH_WaterFiller");
                }
                return _waterFillerDef;
            }
        }

        public static void Postfix(Thing thing, StatDef stat, ref float __result)
        {
            if (stat != StatDefOf.WorkToBuild) return;

            if (JingWeiFillsSeaMod.Settings == null) return;

            if (thing.def == WaterFillerDef)
            {
                __result *= JingWeiFillsSeaMod.Settings.WorkAmountMultiplier;
            }
        }
    }
}
