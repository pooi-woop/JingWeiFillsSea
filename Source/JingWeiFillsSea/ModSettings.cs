// ==============================================================================
// 精卫填海 - Mod 设置
// ==============================================================================
//
// 默认工作量倍率 = 1.0（对应 XML 中 WorkToBuild=48000，约 4 个游戏日）
// 允许范围 0.1 ~ 10.0
// ==============================================================================

using UnityEngine;
using Verse;

namespace JWFH
{
    public class ModSettings_JingWeiFillsSea : ModSettings
    {
        /// <summary>
        /// 工作量倍率。默认 1.0。
        /// </summary>
        public float WorkAmountMultiplier = 1.0f;

        public void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // 显示当前倍率对应的预估天数（XML 基础值为 4 个游戏日）
            listing.Label(
                $"工作量倍率: {WorkAmountMultiplier:F2}  (预估 {4 * WorkAmountMultiplier:F1} 个游戏日)");

            // 滑块: 0.1 ~ 10.0
            WorkAmountMultiplier = listing.Slider(WorkAmountMultiplier, 0.1f, 10.0f);

            listing.Gap();
            listing.Label("提示: 倍率=1.0 时为标准 4 个游戏日；0.5=2 日；2.0=8 日。");

            listing.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref WorkAmountMultiplier, "WorkAmountMultiplier", 1.0f);
        }
    }
}
