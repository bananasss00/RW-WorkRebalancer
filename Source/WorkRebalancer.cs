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
            workDefDatabase.AddRange(RecipeWorkAmount.GetAll());
            workDefDatabase.AddRange(ThingWorkAmount.GetAll());
            workDefDatabase.AddRange(TerrainWorkAmount.GetAll());
            workDefDatabase.AddRange(ResearchWorkAmount.GetAll());
            workDefDatabase.AddRange(PlantGrowDays.GetAll());
            workDefDatabase.AddRange(PlantWorkAmount.GetAll());

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
}
