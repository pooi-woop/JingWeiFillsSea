// ==============================================================================
// 精卫填海 - ThingComp: 完成建造时把水格变为陆地
// ==============================================================================
//
// 作用：当殖民者完成"水体填充标记"建筑的建造后，将这一格的地形从水变为"土壤"，
//      然后销毁标记本身（保持地图整洁，仅留下陆地）。
//
// 实现要点：
//   - CompProperties_FillWater 是 XML 中可配置的参数类
//   - CompFillWater 是运行时执行的 ThingComp
//   - 重写 PostSpawnSetup：在建筑生成（即建造完成）后触发
//   - 调用 map.terrainGrid.SetTerrain 将格子设为指定 TerrainDef（默认: Soil 土壤）
// ==============================================================================

using Verse;
using RimWorld;

namespace JWFH
{
    /// <summary>
    /// XML 中可配置的参数类。
    /// </summary>
    public class CompProperties_FillWater : CompProperties
    {
        /// <summary>
        /// 完成后将水格替换为哪种陆地地形。默认 "Soil" 土壤。
        /// 你可以在 XML 中改为 "Gravel"(碎石)、"Sand"(沙地)、"Marsh"(沼泽干涸后) 等。
        /// </summary>
        public string replaceWithTerrainDefName = "Soil";

        /// <summary>
        /// 完成后是否销毁标记本身。
        /// </summary>
        public bool destroySelfOnComplete = true;

        /// <summary>
        /// 缓存解析后的 TerrainDef，避免运行时反复查询。
        /// </summary>
        public TerrainDef ReplaceWithTerrain { get; private set; }

        public CompProperties_FillWater()
        {
            compClass = typeof(CompFillWater);
        }

        /// <summary>
        /// RimWorld 在加载所有 Def 后会调用此方法，用于解析 Def 引用。
        /// 我们在这里把字符串 defName 解析为 TerrainDef 对象。
        /// </summary>
        public override void ResolveReferences(ThingDef parent)
        {
            base.ResolveReferences(parent);
            if (string.IsNullOrEmpty(replaceWithTerrainDefName))
                replaceWithTerrainDefName = "Soil";
            ReplaceWithTerrain = DefDatabase<TerrainDef>.GetNamed(replaceWithTerrainDefName, false);
            if (ReplaceWithTerrain == null)
            {
                Log.Warning($"[精卫填海] 未找到地形 Def: {replaceWithTerrainDefName}, 回退到 Soil。");
                ReplaceWithTerrain = TerrainDefOf.Soil;
            }
        }
    }

    /// <summary>
    /// ThingComp 实现类。负责在建筑生成（建造完成）后修改地形。
    /// </summary>
    public class CompFillWater : ThingComp
    {
        public CompProperties_FillWater Props => (CompProperties_FillWater)props;

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
                Log.Warning("[精卫填海] CompFillWater: map 为 null，无法设置地形。");
                return;
            }

            // 取当前地形（用于日志和检查）
            TerrainDef currentTerrain = map.terrainGrid.TerrainAt(cell);

            // 验证当前地形确实是水或沼泽
            // (防御性检查——正常情况下 PlaceWorker 已保证)
            bool isWater = IsWaterOrMarsh(currentTerrain);
            if (!isWater)
            {
                Log.Warning($"[精卫填海] 当前地形 {currentTerrain?.defName} 不是水/沼泽，跳过填充。");
                return;
            }

            // 把这一格的地形设为指定陆地（默认 Soil 土壤）
            // SetTerrain 会自动处理地形网格、可见性、植被等更新
            map.terrainGrid.SetTerrain(cell, Props.ReplaceWithTerrain);

            // 生成视觉提示：散落的泥土
            if (ThingDefOf.Filth_Dirt != null)
            {
                FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Dirt);
            }

            Log.Message($"[精卫填海] 已在 {cell} 将 {currentTerrain.defName} 替换为 {Props.ReplaceWithTerrain.defName}。");

            // 注意：这里【不能】同步 destroy 自身！
            // 原版 Building.SpawnSetup 在跑完 ThingWithComps.SpawnSetup（内部会调用本 comp 的
            // PostSpawnSetup）后，紧接着执行 base.Map.listerBuildings.Add(this)。
            // 若在此销毁，thing.Map 会变成 null，原版那一行 ldfld 就会抛 NullReferenceException
            // （表现为每放置/建造一个标记就爆一次 "Root level exception in OnGUI()"）。
            // 销毁已交由 Patch_BuildingSpawnSetup_MarkerDestroy（Building.SpawnSetup 的
            // Harmony Postfix）在 spawn 流程完整结束后再执行。
        }

        /// <summary>
        /// 判断地形是否属于"水或沼泽"（可被填充）。
        /// 覆盖原版所有水类地形。
        /// </summary>
        private bool IsWaterOrMarsh(TerrainDef terrain)
        {
            if (terrain == null) return false;

            // 直接检查 defName
            switch (terrain.defName)
            {
                case "WaterShallow":
                case "WaterDeep":
                case "WaterOceanShallow":
                case "WaterOceanDeep":
                case "WaterMovingShallow":
                case "WaterMovingChestDeep":
                case "Marsh":
                    return true;
                default:
                    return false;
            }
        }
    }
}
