using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkRebalancer
{
    public class ThingWorkAmount : IWorkAmount
    {
        public static IEnumerable<IWorkAmount> GetAll()
        {
            foreach (var x in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                ThingWorkAmount thing = new ThingWorkAmount(x);
                if (thing.HasWorkValue())
                {
                   yield return thing;
                }
            }
        }

        private float? uninstallWork;
        private float? workToBuildStat;
        private float? workToMakeStat;
        private float? workToBuildFactor;
        private float? workToMakeFactor;

        private float? SetMod(StatModifier stat, float? value, float? percentOfBase = null)
        {
            if (value == null)
                return null;
            
            if (stat == null)
            {
                Log.Error($"[WorkRebalancer] SetMod:: value not null, stat null");
                return null;
            }

            if (percentOfBase == null)
            {
                stat.value = (float) value;
            }
            else
            {
                Loger.Append($"{((ThingDef)Ref).LabelCap} {stat.stat.LabelCap}: {value} => ");
                stat.value = (float) value * (float) percentOfBase;
                Loger.AppendLine($"{stat.value}");
            }
            return stat.value;
        }

        public ThingWorkAmount(ThingDef def)
        {
            Ref = def;
            uninstallWork = def.building?.uninstallWork;
            workToBuildStat = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild)?.value;
            workToMakeStat = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake)?.value;
            workToBuildFactor = def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild)?.value;
            workToMakeFactor = def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake)?.value;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => uninstallWork != null || workToBuildStat != null || workToMakeStat != null || workToBuildFactor != null || workToMakeFactor != null;

        public void Restore()
        {
            ThingDef def = (ThingDef) Ref;
            if (uninstallWork != null) def.building.uninstallWork = (float)uninstallWork;
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildStat);
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeStat);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildFactor);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeFactor);
        }

        public void Set(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            if (uninstallWork != null) def.building.uninstallWork = (float)uninstallWork * percentOfBaseValue;
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildStat, percentOfBaseValue);
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeStat, percentOfBaseValue);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildFactor, percentOfBaseValue);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeFactor, percentOfBaseValue);
        }

        public void SetStats(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            if (uninstallWork != null) def.building.uninstallWork = (float)uninstallWork * percentOfBaseValue;
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildStat, percentOfBaseValue);
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeStat, percentOfBaseValue);
        }

        public void SetFactors(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildFactor, percentOfBaseValue);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeFactor, percentOfBaseValue);
        }
    }
}