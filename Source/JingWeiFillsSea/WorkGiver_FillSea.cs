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
    /// 让殖民者用"挖掘"工作类型建造水体填充标记。
    /// </summary>
    public class WorkGiver_FillSea : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var things = pawn.Map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial));

            foreach (Thing t in things)
            {
                if (t is Frame frame)
                {
                    ThingDef buildDef = frame.def.entityDefToBuild as ThingDef;
                    if (buildDef != null && buildDef.defName == "JWFH_WaterFiller")
                    {
                        yield return t;
                    }
                }
                else if (t.def.defName == "JWFH_WaterFiller")
                {
                    yield return t;
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Frame frame)) return false;

            ThingDef buildDef = frame.def.entityDefToBuild as ThingDef;
            if (buildDef == null || buildDef.defName != "JWFH_WaterFiller") return false;

            if (frame.IsForbidden(pawn) && !forced) return false;

            // 殖民者需要能"接触"到水格——通常表示能站在水边
            // 但深水格是 Impassable, 殖民者无法站进去, 只能站旁边. PathEndMode.Touch 处理这个.
            if (!pawn.CanReach(frame, PathEndMode.Touch, Danger.Deadly)) return false;

            if (!pawn.CanReserve(frame, 1, -1, null, forced)) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HasJobOnThing(pawn, t, forced)) return null;

            Frame frame = (Frame)t;
            Job job = JobMaker.MakeJob(JobDefOf.FinishFrame, t);
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
