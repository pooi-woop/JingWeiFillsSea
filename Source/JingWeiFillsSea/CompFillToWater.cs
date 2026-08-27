// ==============================================================================
// 精卫填海 - ThingComp: 完成建造时把陆地变为水体
// ==============================================================================
//
// 作用：当殖民者完成"注水标记"建筑的建造后，将这一格的地形变为水体（默认 WaterShallow），
//      然后销毁标记本身（保持地图整洁，仅留下水体）。
//
// 与 CompFillWater（水->陆地）方向相反：本题以人为本，把陆地/任意地形灌成水。
// 该标记没有 PlaceWorker 限制，可放置在任意可建造的格子（尤其是陆地上）。
//
// 实现要点：
//   - CompProperties_FillToWater 是 XML 中可配置的参数类
//   - CompFillToWater 是运行时执行的 ThingComp
//   - 重写 PostSpawnSetup：在建筑生成（即建造完成）后触发
//   - 调用 map.terrainGrid.SetTerrain 将格子设为指定的水体 TerrainDef
// ==============================================================================

using System.Linq;
using Verse;
using RimWorld;

namespace JWFH
{
    /// <summary>
    /// XML 中可配置的参数类。
    /// </summary>
    public class CompProperties_FillToWater : CompProperties
    {
        /// <summary>
        /// 完成后将该格变为哪种水体地形。默认 "WaterShallow"（浅水，可通行）。
        /// 在 XML 中可改为 "WaterDeep"、"WaterOceanShallow"、"WaterOceanDeep" 等。
        /// </summary>
        public string convertToWaterDefName = "WaterShallow";

        /// <summary>
        /// 完成后是否销毁标记本身。
        /// </summary>
        public bool destroySelfOnComplete = true;

        /// <summary>
        /// 缓存解析后的水体 TerrainDef，避免运行时反复查询。
        /// </summary>
        public TerrainDef WaterTerrain { get; private set; }

        public CompProperties_FillToWater()
        {
            compClass = typeof(CompFillToWater);
        }

        /// <summary>
        /// 在所有 Def 加载后解析水体地形 Def 引用。
        /// </summary>
        public override void ResolveReferences(ThingDef parent)
        {
            base.ResolveReferences(parent);
            if (string.IsNullOrEmpty(convertToWaterDefName))
                convertToWaterDefName = "WaterShallow";

            WaterTerrain = DefDatabase<TerrainDef>.GetNamed(convertToWaterDefName, false);
            if (WaterTerrain == null)
            {
                Log.Warning($"[精卫填海] 未找到水体地形 Def: {convertToWaterDefName}，回退到 WaterShallow。");
                WaterTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail("WaterShallow")
                               ?? DefDatabase<TerrainDef>.AllDefs.FirstOrDefault(t => t.IsWater);
            }
            // 极端兜底（正常情况下不会走到）
            if (WaterTerrain == null)
                WaterTerrain = TerrainDefOf.Soil;
        }
    }

    /// <summary>
    /// ThingComp 实现类。负责在建筑生成（建造完成）后把这一格变为水体。
    /// </summary>
    public class CompFillToWater : ThingComp
    {
        public CompProperties_FillToWater Props => (CompProperties_FillToWater)props;

        /// <summary>
        /// 在 Thing 生成到地图上之后调用。对于建筑，这是"建造完成"的时刻。
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // 仅在新建（即建造完成）时执行；读档时跳过
            if (respawningAfterLoad) return;

            IntVec3 cell = parent.Position;
            Map map = parent.Map;

            if (map == null)
            {
                Log.Warning("[精卫填海] CompFillToWater: map 为 null，无法设置地形。");
                return;
            }

            TerrainDef current = map.terrainGrid.TerrainAt(cell);
            TerrainDef to = Props.WaterTerrain;

            // 目标为空、本身就是目标水格、或已经是任意水体时，转换无意义，直接返回。
            // 标记统一由 Patch_BuildingSpawnSetup_MarkerDestroy 在 SpawnSetup 结束后销毁。
            if (to == null || current == to || (current != null && current.IsWater))
            {
                Log.Message($"[精卫填海] 在 {cell} 已是水体（或目标为空），跳过注水，仅移除标记。");
                return;
            }

            // 把这一格的地形设为水体
            map.terrainGrid.SetTerrain(cell, to);

            Log.Message($"[精卫填海] 已在 {cell} 将 {current?.defName ?? "无地形"} 变为 {to.defName}。");

            // 注意：这里【不能】同步 destroy 自身！
            // 原版 Building.SpawnSetup 在跑完 ThingWithComps.SpawnSetup（内部会调用本 comp 的
            // PostSpawnSetup）后，紧接着执行 base.Map.listerBuildings.Add(this)。
            // 若在此销毁，thing.Map 会变成 null，原版那一行 ldfld 就会抛 NullReferenceException
            // （表现为每放置/建造一个标记就爆一次 "Root level exception in OnGUI()"）。
            // 销毁已交由 Patch_BuildingSpawnSetup_MarkerDestroy（Building.SpawnSetup 的
            // Harmony Postfix）在 spawn 流程完整结束后再执行。
        }
    }
}