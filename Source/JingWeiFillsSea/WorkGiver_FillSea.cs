// ==============================================================================
// 精卫填海 - 自定义 WorkGiver: 用"挖掘"(Mining) 工作类型建造水体填充标记
// ==============================================================================
//
// 实现与愚公填山的 WorkGiver_FillMountain 完全对称，只是 defName 不同。
// ==============================================================================

using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace JWFH
{
    /// <summary>
    /// 自定义 WorkGiver_Scanner。
    /// 让殖民者用"挖掘"工作类型建造水体填充标记（同时支持蓝图与框架）。
    /// </summary>
    public class WorkGiver_FillSea : WorkGiver_Scanner
    {
        private const string MarkerDefName = "JWFH_WaterFiller";

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        /// <summary>
        /// 判断一个 Thing 是否我们的水体填充标记（蓝图、框架都算）。
        /// </summary>
        private static bool IsOurMarker(Thing t)
        {
            if (t is Blueprint || t is Frame)
            {
                ThingDef buildDef = t.def.entityDefToBuild as ThingDef;
                return buildDef != null && buildDef.defName == MarkerDefName;
            }
            return t.def.defName == MarkerDefName;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var things = pawn.Map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial));

            foreach (Thing t in things)
            {
                if (IsOurMarker(t))
                {
                    yield return t;
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Blueprint || t is Frame)) return false;
            if (!IsOurMarker(t)) return false;

            // 防御：目标已被销毁/未生成时直接返回 false，避免对缺失对象解引用抛 NRE。
            if (!t.Spawned) return false;
            if (t.Map == null) return false;

            if (t.IsForbidden(pawn) && !forced) return false;

            // 殖民者需要能"接触"到水格——通常表示能站在水边
            // 但深水格是 Impassable, 殖民者无法站进去, 只能站旁边. PathEndMode.Touch 处理这个.
            if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly)) return false;

            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;

            // 节流：非强制（自动安排）时限制同一 tick 内的领作业数，抑制任务风暴。
            // 注意：此判断必须放在 HasJobOnThing（CanGiveJob）里而非 JobOnThing 中——
            // RimWorld 约定 CanGiveJob 返回 true 时 JobOnThing 必须给出有效 Job，
            // 若在 JobOnThing 返回 null 会触发 "provided target but yielded no actual job"
            // 同步校验报错并使小人发呆。放在这里返回 false 只是"暂无可做"，不会报警。
            if (!forced && !JobThrottle.TryAllow(pawn)) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // 不要在此复查 HasJobOnThing 并返回 null！
            // RimWorld 的 JobGiver_Work 提交候选后会调用本方法，一旦返回 null 就触发
            // "provided target but yielded no actual job" 的 Log.ErrorOnce（无害但刷屏）——
            // 常见于多个小人抢同一框架、批量框选导致水体连续变化，目标在两次调用间被销毁/被预订。
            // 这里对我们的标记恒定生成 Job；若目标确实已销毁，会在 JobDriver 内部干净地结束，不报错。
            if (t == null) return null;
            if (!(t is Blueprint || t is Frame)) return null;
            if (!IsOurMarker(t)) return null;

            Job job = t is Blueprint
                ? JobMaker.MakeJob(JobDefOf.PlaceNoCostFrame, t)   // 蓝图 -> 框架
                : JobMaker.MakeJob(JobDefOf.FinishFrame, t);       // 框架 -> 完成（真实工作量在此消耗）

            job.expiryInterval = 2000;
            job.checkOverrideOnExpire = false;
            return job;
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.Touch; }
        }
    }
}
