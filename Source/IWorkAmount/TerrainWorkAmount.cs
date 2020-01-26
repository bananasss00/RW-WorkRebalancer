using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkRebalancer
{
    public class TerrainWorkAmount : IWorkAmount
    {
        public static IEnumerable<IWorkAmount> GetAll()
        {
            foreach (var x in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                TerrainWorkAmount terrain = new TerrainWorkAmount(x);
                if (terrain.HasWorkValue())
                {
                    yield return terrain;
                }
            }
        }

        private float? workToBuild;

        public TerrainWorkAmount(TerrainDef def)
        {
            Ref = def;
            workToBuild = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild)?.value;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => workToBuild != null;

        public void Restore()
        {
            TerrainDef def = (TerrainDef) Ref;
            StatModifier v = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild);
            if (v == null)
            {
                Log.Error($"[WorkRebalancer] Restore:: {def.LabelCap} null");
                return;
            }

            if (workToBuild != null)
                v.value = (float) workToBuild;
        }

        public void Set(float percentOfBaseValue)
        {
            TerrainDef def = (TerrainDef) Ref;
            StatModifier v = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild);
            if (v == null)
            {
                Log.Error($"[WorkRebalancer] Set:: {def.LabelCap} null");
                return;
            }

            if (workToBuild != null)
            {
                Loger.Append($"{def.LabelCap} {v.stat.LabelCap}: {workToBuild} => ");
                v.value = (float) workToBuild * percentOfBaseValue;
                Loger.AppendLine($"{v.value}");
            }
        }
    }
}