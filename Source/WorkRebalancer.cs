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
using WorkRebalancer.Patches;

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

            HarmonyInstance h = HarmonyInstance.Create("pirateby.WorkRebalancerMod");
            h.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"[WorkRebalancer] Apply JobDriver_Repair_Patch... Result = {JobDriver_Repair_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply HSK_CollectJobs_Patch... Result = {HSKCollectJobsPatched = HSK_CollectJobs_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply RF_Drill_Patch... Result = {RFDrillJobPatched = RF_Drill_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply JobDriver_MineQuarry_Patch... Result = {HSKMineQuarryPatched = JobDriver_MineQuarry_Patch.Apply(h)}");
        }

        public override string ModIdentifier => "WorkRebalancer";

        public override void Tick(int currentTick)
        {
            base.Tick(currentTick);

            if (Current.ProgramState != ProgramState.Playing)
                return;

            // check every 7 sec
            if ((currentTick % CheckHostileDelay.Value) != 0)
                return;

            // if option off reset to config
            if (!RestoreWhenHostileDetected.Value)
            {
                if (HostileDetected)
                {
                    ApplySettings();
                    HostileDetected = false;
                }
                return;
            }

            bool hostileDetected = Utils.HostileExistsOnMaps();
            if (HostileDetected && !hostileDetected) // detected hostiles rip
            {
                ApplySettings();
                HostileDetected = false;
                if (DebugLog)
                {
                    Log.Message($"[WorkRebalancer] Apply configured settings");
                }
            }
            else if (!HostileDetected && hostileDetected) // detected new hostiles
            {
                ApplySettingsDefaults();
                HostileDetected = true;
                if (DebugLog)
                {
                    Log.Message($"[WorkRebalancer] Apply default 100% settings");
                }
            }
        }

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

            RestoreWhenHostileDetected = modSettingsPack.GetHandle(
                "RestoreWhenHostileDetected",
                "RestoreWhenHostileDetected",
                "Restore all workAmount when hostile detected on any map",
                true);
            CheckHostileDelay = modSettingsPack.GetHandle(
                "CheckHostileDelay",
                "CheckHostileDelay",
                "Check hostile delay in ticks. Default 420 ticks ~= 7 seconds",
                420);
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
            RepairJobAddX = modSettingsPack.GetHandle(
                "RepairJobAddX",
                "RepairJobAddX",
                "Add hitpoints when repair buildings. Default = 1",
                1,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 1000);
            if (HSKCollectJobsPatched)
            {
                PercentOfBaseHSKCollectJobs = modSettingsPack.GetHandle(
                    "PercentOfBaseHSKCollectJobs",
                    "PercentOfBaseHSKCollectJobs",
                    "HSK Collect Jobs: Peat, Clay, Sand, Crushedstone",
                    100,
                    value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            }

            if (RFDrillJobPatched)
            {
                RFDrillJobMultiplier = modSettingsPack.GetHandle(
                    "RFDrillJobMultiplier",
                    "RFDrillJobMultiplier",
                    "Rimfeller Drill Job Multiplier. Default = 1",
                    1f,
                    value => float.TryParse(value, out float num) && num >= 1f && num <= 1000f);
            }
            if (HSKMineQuarryPatched)
            {
                PercentOfBaseHSKMineQuarry = modSettingsPack.GetHandle(
                    "PercentOfBaseHSKMineQuarry",
                    "PercentOfBaseHSKMineQuarry",
                    "HSK Quarry mine. Default = 100",
                    100,
                    value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            }
            DebugLog = modSettingsPack.GetHandle(
                "DebugLog",
                "DebugLog",
                "DebugLog",
                false);


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
                    RestoreWhenHostileDetected.ResetToDefault();
                    CheckHostileDelay.ResetToDefault();
                    PercentOfBaseResearches.ResetToDefault();
                    PercentOfBaseTerrains.ResetToDefault();
                    PercentOfBaseRecipes.ResetToDefault();
                    PercentOfBaseThingStats.ResetToDefault();
                    PercentOfBaseThingFactors.ResetToDefault();
                    PercentOfBasePlantsWork.ResetToDefault();
                    PercentOfBasePlantsGrowDays.ResetToDefault();
                    RepairJobAddX.ResetToDefault();
                    PercentOfBaseHSKCollectJobs.ResetToDefault();
                    RFDrillJobMultiplier.ResetToDefault();
                    PercentOfBaseHSKMineQuarry.ResetToDefault();
                    
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
            //if (RestoreWhenHostileDetected.Value && HostileDetected)
            //    return;

            //Loger.Clear();
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
            //Loger.Save("dumpRebuilder.txt");
        }

        // hostile detected
        public void ApplySettingsDefaults()
        {
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(ResearchWorkAmount)))
            {
                w.Set(1f);
            }
                    
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(TerrainWorkAmount)))
            {
                w.Set(1f);
            }
                    
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(RecipeWorkAmount)))
            {
                w.Set(1f);
            }
                    
            foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
            {
                w.SetStats(1f);
            }
                    
            foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
            {
                w.SetFactors(1f);
            }

            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantWorkAmount)))
            {
                w.Set(1f);
            }

            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantGrowDays)))
            {
                w.Set(1f);
            }
        }

        public SettingHandle<bool> RestoreWhenHostileDetected;
        public SettingHandle<int> CheckHostileDelay;
        public SettingHandle<int> PercentOfBaseResearches;
        public SettingHandle<int> PercentOfBaseTerrains;
        public SettingHandle<int> PercentOfBaseRecipes;
        public SettingHandle<int> PercentOfBaseThingStats;
        public SettingHandle<int> PercentOfBaseThingFactors;
        public SettingHandle<int> PercentOfBasePlantsWork;
        public SettingHandle<int> PercentOfBasePlantsGrowDays;
        public SettingHandle<int> RepairJobAddX;
        public SettingHandle<int> PercentOfBaseHSKCollectJobs;
        public SettingHandle<float> RFDrillJobMultiplier;
        public SettingHandle<int> PercentOfBaseHSKMineQuarry;
        public SettingHandle<bool> DebugLog;

        public bool HSKCollectJobsPatched = false;
        public bool RFDrillJobPatched = false;
        public bool HSKMineQuarryPatched = false;
        public bool HostileDetected = false;
    }
}
