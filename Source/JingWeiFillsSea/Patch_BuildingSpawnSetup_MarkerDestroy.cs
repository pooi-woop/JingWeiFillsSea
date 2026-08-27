// ==============================================================================
// 精卫填海 - 在 Building.SpawnSetup 完成后销毁标记（修复 NRE 红字）
// ==============================================================================
//
// 背景与根因：
//   原版 Building.SpawnSetup 的执行顺序是——
//     1. base.SpawnSetup(map, bool)   // ThingWithComps.SpawnSetup，
//                                      // 内部会逐个调用所有 ThingComp 的 PostSpawnSetup
//     2. base.Map.listerBuildings.Add(this)
//   旧代码在 Comp 的 PostSpawnSetup 里同步调用 parent.Destroy(DestroyMode.Vanish)，
//   销毁会把 this.Map 置为 null。于是在第 2 步 lsterBuildings.Add 处对 null 解引用，
//   抛出 NullReferenceException——表现就是玩家每放置/建造一个标记，
//   调试窗就爆一次 "Root level exception in OnGUI()"（同一份调用栈，Ref 编号一致）。
//
// 修复思路：
//   不再在 Comp 内同步销毁，而是改在 Building.SpawnSetup 的 Harmony Postfix 里，
//   等整个 spawn 流程（包括 listerBuildings.Add）完整走完之后再销毁标记。
//   这样对游戏行为没有任何改变（标记依旧立即消失），只是不再打断原版方法执行。
//
// 为什么这样做安全：
//   经核实，GenSpawn.Spawn 在 SpawnSetup 返回后只检查 Spawned / stackCount /
//   passability（标记为 Standable，全部跳过），Designator_Build.DesignateSingleCell
//   之后也只是对标记做 StyleDef / GlowColor 等纯赋值——已在 spawn 时被销毁的
//   标记不会再被解引用，因此不会引发二次异常。
// ==============================================================================

using HarmonyLib;
using RimWorld;
using Verse;

namespace JWFH
{
    /// <summary>
    /// 在标记建筑构建完成后销毁它（保留地形修改结果）。
    /// 精卫填海的两个标记（JWFH_WaterFiller / JWFH_FloodFiller）都由此统一销毁。
    /// </summary>
    [HarmonyPatch(typeof(Verse.Building), nameof(Verse.Building.SpawnSetup))]
    public static class Patch_BuildingSpawnSetup_MarkerDestroy
    {
        public static void Postfix(Building __instance, bool respawningAfterLoad)
        {
            // 读档恢复生成时不销毁（地形转换只在新建阶段执行一次）
            if (respawningAfterLoad) return;

            // 防御：若在 spawn 过程中已被销毁则跳过
            if (__instance.Destroyed) return;

            // 只处理本 mod 的两种标记建筑
            if (!MarkerDefs.IsOurMarker(__instance.def)) return;

            // 依据 comp 的 destroySelfOnComplete 决定是否销毁。
            // 这两个标记各自只挂一个对应 comp，另一个为 null，null 视为"不设限"。
            CompFillToWater toWater = __instance.TryGetComp<CompFillToWater>();
            CompFillWater toLand = __instance.TryGetComp<CompFillWater>();
            bool destroySelf =
                (toWater == null || toWater.Props.destroySelfOnComplete) &&
                (toLand == null || toLand.Props.destroySelfOnComplete);

            if (destroySelf)
            {
                // DestroyMode.Vanish 表示"消失"，不留建筑残骸
                __instance.Destroy(DestroyMode.Vanish);
            }
        }
    }
}