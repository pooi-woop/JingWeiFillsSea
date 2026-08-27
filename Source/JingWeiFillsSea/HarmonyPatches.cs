// ==============================================================================
// 精卫填海 - Harmony 补丁
// ==============================================================================
//
// Patch 1: 让原版 Construction workgiver 跳过我们的标记建筑
// Patch 2: 让标记建筑的 WorkToBuild stat 受 Mod 设置倍率影响
//   - 注意：Frame.WorkToBuild 走的是 BuildableDef.GetStatValueAbstract
//     （而非 StatExtension.GetStatValue），所以倍率补丁必须打在
//     GetStatValueAbstract 上才能同时影响显示与实际建造工作量。
// Patch 3: 为标记的建造框架（Frame）绘制进度条
// ==============================================================================

using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace JWFH
{
    // 本模组两个标记建筑（水体填充/注水）的 defName 集合。
    internal static class MarkerDefs
    {
        internal static readonly string[] Names = { "JWFH_WaterFiller", "JWFH_FloodFiller" };

        internal static bool IsOurMarker(ThingDef def)
        {
            if (def == null) return false;
            for (int i = 0; i < Names.Length; i++)
            {
                if (def.defName == Names[i]) return true;
            }
            return false;
        }
    }

    // Patch 1: 让原版 WorkGiver_ConstructFinishFrames 跳过我们的标记建筑
    // 注意：RimWorld 1.6 中该类未重写 HasJobOnThing（在基类 WorkGiver_Scanner 中），
    // 按方法名补丁继承虚方法会在 Harmony 2.x 下解析失败。故改在类自身声明的
    // JobOnThing 上打补丁，目标为我们的标记框架时返回 null。
    [HarmonyPatch(typeof(WorkGiver_ConstructFinishFrames), "JobOnThing")]
    public static class Patch_WorkGiver_ConstructFinishFrames_JobOnThing
    {
        public static Job Postfix(Job __result, Thing t)
        {
            if (t is Frame frame)
            {
                ThingDef buildDef = frame.def.entityDefToBuild as ThingDef;
                if (MarkerDefs.IsOurMarker(buildDef))
                {
                    return null;
                }
            }
            return __result;
        }
    }

    // Patch 2: 让 WorkToBuild stat 受 Mod 设置倍率影响
    // Frame.WorkToBuild => def.entityDefToBuild.GetStatValueAbstract(WorkToBuild, Stuff)，
    // 因此必须补丁 BuildableDef.GetStatValueAbstract，才能同时影响：
    //   - 框架/蓝图信息面板显示的"工作量/剩余工作量"
    //   - JobDriver_ConstructFinishFrame 中实际消耗的 workToBuild
    [HarmonyPatch(typeof(BuildableDef), "GetStatValueAbstract")]
    public static class Patch_BuildableDef_GetStatValueAbstract
    {
        public static void Postfix(BuildableDef __instance, StatDef stat, ref float __result)
        {
            if (stat != StatDefOf.WorkToBuild) return;

            if (JingWeiFillsSeaMod.Settings == null) return;

            if (__instance is ThingDef td && MarkerDefs.IsOurMarker(td))
            {
                __result *= JingWeiFillsSeaMod.Settings.WorkAmountMultiplier;
            }
        }
    }

    // Patch 3: 为标记的建造框架绘制进度条
    [HarmonyPatch(typeof(Frame), "DrawAt")]
    public static class Patch_Frame_DrawAt_ProgressBar
    {
        private static readonly Material BarFilledMat =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.9f, 0.5f));
        private static readonly Material BarUnfilledMat =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.2f, 0.2f));

        public static void Postfix(Frame __instance)
        {
            if (!(__instance.def.entityDefToBuild is ThingDef buildDef)) return;
            if (!MarkerDefs.IsOurMarker(buildDef)) return;
            if (__instance.WorkToBuild <= 0f) return;

            GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
            {
                center = __instance.DrawPos + Vector3.up * 0.3f,
                size = new Vector2(0.9f, 0.12f),
                fillPercent = Mathf.Clamp01(__instance.PercentComplete),
                filledMat = BarFilledMat,
                unfilledMat = BarUnfilledMat,
                margin = 0.05f,
                rotation = Rot4.North
            });
        }
    }
}
