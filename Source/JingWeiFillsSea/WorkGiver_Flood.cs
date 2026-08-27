// ==============================================================================
// 精卫填海 - 自定义 WorkGiver: 用"挖掘"(Mining) 工作类型建造"注水标记"
// ==============================================================================
//
// 与 WorkGiver_FillSea（水体填充标记，水->陆地）互为独立工作分配：
// 本类负责"注水标记"(JWFH_FloodFiller)，把陆地灌成水体。
// 同类支持蓝图(Blueprint) 与框架(Frame) 两个阶段，用 IsOurMarker() 统一识别。
// ==============================================================================

using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace JWFH
{
    /// <summary>
    /// 自定义 WorkGiver_Scanner。
    /// 让殖民者用"挖掘"工作类型建造注水标记（同时支持蓝图与框架）。
    /// </summary>
    public class WorkGiver_Flood : WorkGiver_Scanner
    {
        private const string MarkerDefName = "JWFH_FloodFiller";

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        /// <summary>
        /// 判断一个 Thing 是否我们的注水标记（蓝图、框架都算）。
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

            if (t.IsForbidden(pawn) && !forced) return false;

            if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly)) return false;

            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HasJobOnThing(pawn, t, forced)) return null;

            Job job;
            if (t is Blueprint)
            {
                // 蓝图 -> 框架
                job = JobMaker.MakeJob(JobDefOf.PlaceNoCostFrame, t);
            }
            else
            {
                // 框架 -> 完成（真实工作量在此消耗）
                job = JobMaker.MakeJob(JobDefOf.FinishFrame, t);
            }

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