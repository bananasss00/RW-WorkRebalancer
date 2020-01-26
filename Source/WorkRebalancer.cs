using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using HugsLib;
using HugsLib.Settings;
using Verse;
using RimWorld;
using UnityEngine;

namespace WorkRebalancer
{
    public class WorkRebalancerMod : ModBase
    {
        private List<IWorkAmount> workDefDatabase;
        private ModSettingsPack modSettingsPack;

        public static WorkRebalancerMod Instance { get; private set; }

        public WorkRebalancerMod()
        {
            Instance = this;
            workDefDatabase = new List<IWorkAmount>();
            HarmonyInstance.Create("pirateby.WorkRebalancerMod").PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string ModIdentifier => "WorkRebalancer";

        public override void DefsLoaded()
        {
            DefDatabase<RecipeDef>.AllDefsListForReading.ForEach(x =>
            {
                RecipeWorkAmount recipe = new RecipeWorkAmount(x);
                if (recipe.HasWorkValue())
                {
                    workDefDatabase.Add(recipe);
                }
            });
            DefDatabase<ThingDef>.AllDefsListForReading.ForEach(x =>
            {
                ThingWorkAmount thing = new ThingWorkAmount(x);
                if (thing.HasWorkValue())
                {
                    workDefDatabase.Add(thing);
                }
            });
            DefDatabase<TerrainDef>.AllDefsListForReading.ForEach(x =>
            {
                TerrainWorkAmount terrain = new TerrainWorkAmount(x);
                if (terrain.HasWorkValue())
                {
                    workDefDatabase.Add(terrain);
                }
            });
            DefDatabase<ResearchProjectDef>.AllDefsListForReading.ForEach(x =>
            {
                ResearchWorkAmount research = new ResearchWorkAmount(x);
                if (research.HasWorkValue())
                {
                    workDefDatabase.Add(research);
                }
            });
            DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.plant != null).ToList().ForEach(x =>
            {
                PlantGrowDays plantGrowDays = new PlantGrowDays(x);
                if (plantGrowDays.HasWorkValue())
                {
                    workDefDatabase.Add(plantGrowDays);
                }

                PlantWorkAmount plantWork = new PlantWorkAmount(x);
                if (plantWork.HasWorkValue())
                {
                    workDefDatabase.Add(plantWork);
                }
            });

            InitializeSettings();
            ApplySettings();

            Log.Message($"WorkRebalancerMod :: DefsLoaded");
        }
        
        public void InitializeSettings()
        {
            modSettingsPack = HugsLibController.Instance.Settings.GetModSettings("WorkRebalancer");
            PercentOfBaseResearches = modSettingsPack.GetHandle(
                "PercentOfBaseResearches",
                "PercentOfBaseResearches",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseResearches.OnValueChanged = newVal => Log.Message($"New: {newVal}");

            PercentOfBaseTerrains = modSettingsPack.GetHandle(
                "PercentOfBaseTerrains",
                "PercentOfBaseTerrains",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseRecipes = modSettingsPack.GetHandle(
                "PercentOfBaseRecipes",
                "PercentOfBaseRecipes",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseThingStats = modSettingsPack.GetHandle(
                "PercentOfBaseThingStats",
                "PercentOfBaseThingStats",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseThingFactors = modSettingsPack.GetHandle(
                "PercentOfBaseThingFactors",
                "PercentOfBaseThingFactors",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBasePlantsWork = modSettingsPack.GetHandle(
                "PercentOfBasePlantsWork",
                "PercentOfBasePlantsWork",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBasePlantsGrowDays = modSettingsPack.GetHandle(
                "PercentOfBasePlantsGrowDays",
                "PercentOfBasePlantsGrowDays",
                "Rebalance all workAmount for recipes, buildings, researches",
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);

            SettingHandle<bool> handleRebalance = modSettingsPack.GetHandle("Rebalance", "Rebalance", "RebalanceDesc", false);
            handleRebalance.CustomDrawer = rect =>
            {
                if (Widgets.ButtonText(rect, "Rebalance"))
                {
                    ApplySettings();
                }

                return false;
            };
            SettingHandle<bool> handleReset = modSettingsPack.GetHandle("Reset", "Reset", "ResetDesc", false);
            handleReset.CustomDrawer = rect =>
            {
                if (Widgets.ButtonText(rect, "Reset"))
                {
                    PercentOfBaseResearches.ResetToDefault();
                    PercentOfBaseTerrains.ResetToDefault();
                    PercentOfBaseRecipes.ResetToDefault();
                    PercentOfBaseThingStats.ResetToDefault();
                    PercentOfBaseThingFactors.ResetToDefault();
                    PercentOfBasePlantsWork.ResetToDefault();
                    PercentOfBasePlantsGrowDays.ResetToDefault();
                    
                    //PercentOfBaseResearches.StringValue = 100.ToString();
                    //PercentOfBaseTerrains.StringValue = 100.ToString();
                    //PercentOfBaseRecipes.StringValue = 100.ToString();
                    //PercentOfBaseThingStats.StringValue = 100.ToString();
                    //PercentOfBaseThingFactors.StringValue = 100.ToString();

                    //PercentOfBaseResearches.Value = 100;
                    //PercentOfBaseTerrains.Value = 100;
                    //PercentOfBaseRecipes.Value = 100;
                    //PercentOfBaseThingStats.Value = 100;
                    //PercentOfBaseThingFactors.Value = 100;
                    ApplySettings();
                }

                return false;
            };

            
        }

        public void ApplySettings()
        {
            Loger.Clear();
            float percent;

            percent = PercentOfBaseResearches.Value / 100f;
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(ResearchWorkAmount)))
            {
                w.Set(percent);
            }
                    
            percent = PercentOfBaseTerrains.Value / 100f;
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(TerrainWorkAmount)))
            {
                w.Set(percent);
            }
                    
            percent = PercentOfBaseRecipes.Value / 100f;
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(RecipeWorkAmount)))
            {
                w.Set(percent);
            }
                    
            percent = PercentOfBaseThingStats.Value / 100f;
            foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
            {
                w.SetStats(percent);
            }
                    
            percent = PercentOfBaseThingFactors.Value / 100f;
            foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
            {
                w.SetFactors(percent);
            }

            percent = PercentOfBasePlantsWork.Value / 100f;
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantWorkAmount)))
            {
                w.Set(percent);
            }

            percent = PercentOfBasePlantsGrowDays.Value / 100f;
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantGrowDays)))
            {
                w.Set(percent);
            }
            Loger.Save("dumpRebuilder.txt");
        }

        public SettingHandle<int> PercentOfBaseResearches;
        public SettingHandle<int> PercentOfBaseTerrains;
        public SettingHandle<int> PercentOfBaseRecipes;
        public SettingHandle<int> PercentOfBaseThingStats;
        public SettingHandle<int> PercentOfBaseThingFactors;
        public SettingHandle<int> PercentOfBasePlantsWork;
        public SettingHandle<int> PercentOfBasePlantsGrowDays;
    }

    public static class Loger
    {
        private static StringBuilder sb = new StringBuilder();
        [System.Diagnostics.Conditional("DEBUG")]
        public static void AppendLine(string s) => sb.AppendLine(s);
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Append(string s) => sb.Append(s);
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Clear() => sb = new StringBuilder();
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Save(string fn) => File.WriteAllText(fn, sb.ToString());
    }

    /*
            var things = DefDatabase<ThingDef>.AllDefsListForReading;
     */

    public interface IWorkAmount
    {
        object Ref { get; set; }
        void Set(float percentOfBaseValue);
        void Restore();
        bool HasWorkValue();
    }

    public class RecipeWorkAmount : IWorkAmount
    {
        private float workAmount;

        public RecipeWorkAmount(RecipeDef def)
        {
            Ref = def;
            workAmount = def.workAmount;
        }

        public bool HasWorkValue() => true;

        public object Ref { get; set; }

        public void Restore()
        {
            RecipeDef def = (RecipeDef) Ref;
            def.workAmount = workAmount;
        }

        public void Set(float percentOfBaseValue)
        {
            RecipeDef def = (RecipeDef) Ref;
            Loger.Append($"{def.LabelCap} : {workAmount} => ");
            def.workAmount = workAmount * percentOfBaseValue;
            Loger.AppendLine($"{def.workAmount}");
        }
    }

    public class ResearchWorkAmount : IWorkAmount
    {
        private float baseCost;

        public ResearchWorkAmount(ResearchProjectDef def)
        {
            Ref = def;
            baseCost = def.baseCost;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => true;

        public void Restore()
        {
            ResearchProjectDef def = (ResearchProjectDef) Ref;
            def.baseCost = baseCost;
        }

        public void Set(float percentOfBaseValue)
        {
            ResearchProjectDef def = (ResearchProjectDef) Ref;
            Loger.Append($"{def.LabelCap} : {baseCost} => ");
            def.baseCost = baseCost * percentOfBaseValue;
            Loger.AppendLine($"{def.baseCost}");
        }
    }

    public class TerrainWorkAmount : IWorkAmount
    {
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
    
    public class ThingWorkAmount : IWorkAmount
    {
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
            workToBuildStat = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild)?.value;
            workToMakeStat = def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake)?.value;
            workToBuildFactor = def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild)?.value;
            workToMakeFactor = def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake)?.value;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => workToBuildStat != null || workToMakeStat != null || workToBuildFactor != null || workToMakeFactor != null;

        public void Restore()
        {
            ThingDef def = (ThingDef) Ref;
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildStat);
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeStat);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildFactor);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeFactor);
        }

        public void Set(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildStat, percentOfBaseValue);
            SetMod(def.statBases?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeStat, percentOfBaseValue);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToBuild), workToBuildFactor, percentOfBaseValue);
            SetMod(def.stuffProps?.statFactors?.FirstOrDefault(x2 => x2.stat == StatDefOf.WorkToMake), workToMakeFactor, percentOfBaseValue);
        }

        public void SetStats(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
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

    public class PlantGrowDays : IWorkAmount
    {
        private float? growDays;

        public PlantGrowDays(ThingDef def)
        {
            Ref = def;
            growDays = def.plant.growDays;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => true;

        public void Restore()
        {
            ThingDef def = (ThingDef) Ref;
            def.plant.growDays = (float) growDays;
        }

        public void Set(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            Loger.Append($"{def.LabelCap} {def.plant.growDays} => ");
            def.plant.growDays = (float) growDays * percentOfBaseValue;
            Loger.AppendLine($"{def.plant.growDays}");
        }
    }

    public class PlantWorkAmount : IWorkAmount
    {
        private float? sowWork;
        private float? harvestWork;

        public PlantWorkAmount(ThingDef def)
        {
            Ref = def;
            sowWork = def.plant.sowWork;
            harvestWork = def.plant.harvestWork;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => true;

        public void Restore()
        {
            ThingDef def = (ThingDef) Ref;
            def.plant.sowWork = (float) sowWork;
            def.plant.harvestWork = (float) harvestWork;
        }

        public void Set(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            Loger.Append($"{def.LabelCap} {def.plant.sowWork} => ");
            def.plant.sowWork = (float) sowWork * percentOfBaseValue;
            Loger.AppendLine($"{def.plant.sowWork}");

            Loger.Append($"{def.LabelCap} {def.plant.harvestWork} => ");
            def.plant.harvestWork = (float) harvestWork * percentOfBaseValue;
            Loger.AppendLine($"{def.plant.harvestWork}");
        }
    }
}
