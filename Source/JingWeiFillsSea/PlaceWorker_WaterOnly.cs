// ==============================================================================
// 精卫填海 - PlaceWorker: 限制只能在"水或沼泽"地形上放置
// ==============================================================================
//
// 作用：让"水体填充标记"建筑只能在水面/沼泽上放置。
// 这样玩家不会误在普通陆地上放置该标记。
//
// 实现要点：
//   - 继承 Verse.PlaceWorker
//   - 重写 AllowsPlacing: 返回 AcceptanceReport (true 表示可放置)
//   - 检查格子的地形是否属于水/沼泽
//
// 与愚公填山不同（愚公填山没有 PlaceWorker，可在任何地面放置）。
// ==============================================================================

using Verse;
using RimWorld;

namespace JWFH
{
    /// <summary>
    /// 限制建筑只能在"水或沼泽"地形上放置。
    /// 在 XML 的 <placeWorkers> 中引用。
    /// </summary>
    public class PlaceWorker_WaterOnly : PlaceWorker
    {
        /// <summary>
        /// 判断是否接受在指定格子放置此建筑。
        /// 返回 AcceptanceReport.Accepted 表示可以放置；
        /// 返回 AcceptanceReport.WithReason("原因") 表示不可以放置并显示原因。
        /// </summary>
        /// <param name="checkingDef">正在放置的 BuildableDef（含 ThingDef 和 TerrainDef）</param>
        /// <param name="loc">放置位置（左下角）</param>
        /// <param name="rot">旋转方向</param>
        /// <param name="map">所在地图</param>
        /// <param name="thingToIgnore">忽略的目标（通常为 null）</param>
        /// <param name="thing">已存在的 thing（可选）</param>
        public override AcceptanceReport AllowsPlacing(
            BuildableDef checkingDef, IntVec3 loc, Rot4 rot,
            Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            // 遍历建筑占用的所有格子（对于 1x1 建筑只有 1 格）
            // GenAdj.CellsOccupiedBy 返回建筑占用的所有格子
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size))
            {
                TerrainDef terrain = map.terrainGrid.TerrainAt(cell);

                // 检查是否是水或沼泽
                if (!IsWaterOrMarsh(terrain))
                {
                    // 返回失败报告，附带原因（会在游戏 UI 中显示）
                    return new AcceptanceReport("水体填充标记只能放置在水或沼泽上。");
                }
            }

            // 通过本类检查，调用基类方法以处理其他限制（如不能与其他建筑重叠等）
            return base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
        }

        /// <summary>
        /// 判断地形是否属于"水或沼泽"。
        /// </summary>
        private bool IsWaterOrMarsh(TerrainDef terrain)
        {
            if (terrain == null) return false;

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
