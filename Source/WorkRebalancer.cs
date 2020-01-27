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
        public static WorkRebalancerMod Instance { get; private set; }

        private List<IWorkAmount> workDefDatabase;
        private ModSettingsPack modSettingsPack;

        public WorkRebalancerMod()
        {
            Instance = this;
            workDefDatabase = new List<IWorkAmount>();

            HarmonyInstance h = HarmonyInstance.Create("pirateby.WorkRebalancerMod");
            h.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"[WorkRebalancer] Apply JobDriver_Repair_Patch... Result = {JobDriver_Repair_Patch.Apply(h)}");
            //Log.Message($"[WorkRebalancer] Apply JobDriver_Deconstruct_Patch... Result = {JobDriver_Deconstruct_Patch.Apply(h)}");
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
            if ((currentTick % CheckHostileDelay) != 0)
                return;

            // if option off reset to config
            if (!RestoreWhenHostileDetected)
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
            workDefDatabase.Clear();
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

            CheckHostileDelay = modSettingsPack.GetHandle(
                "CheckHostileDelay",
                "CheckHostileDelay".Translate(),
                "CheckHostileDelayDesc".Translate(),
                420);

            RestoreWhenHostileDetected = modSettingsPack.GetHandle(
                "RestoreWhenHostileDetected",
                "RestoreWhenHostileDetected".Translate(),
                "RestoreWhenHostileDetectedDesc".Translate(),
                true);

            // percentes //
            PercentOfBaseResearches = modSettingsPack.GetHandle(
                "PercentOfBaseResearches",
                "PercentOfBaseResearches".Translate(),
                "PercentOfBaseResearchesDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseResearches.OnValueChanged = newVal =>
            {
                foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(ResearchWorkAmount)))
                {
                    w.Set(newVal / 100f);
                }
            };

            PercentOfBaseTerrains = modSettingsPack.GetHandle(
                "PercentOfBaseTerrains",
                "PercentOfBaseTerrains".Translate(),
                "PercentOfBaseTerrainsDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseTerrains.OnValueChanged = newVal =>
            {
                foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(TerrainWorkAmount)))
                {
                    w.Set(newVal / 100f);
                }
            };

            PercentOfBaseRecipes = modSettingsPack.GetHandle(
                "PercentOfBaseRecipes",
                "PercentOfBaseRecipes".Translate(),
                "PercentOfBaseRecipesDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseRecipes.OnValueChanged = newVal =>
            {
                foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(RecipeWorkAmount)))
                {
                    w.Set(newVal / 100f);
                }
            };

            PercentOfBaseThingStats = modSettingsPack.GetHandle(
                "PercentOfBaseThingStats",
                "PercentOfBaseThingStats".Translate(),
                "PercentOfBaseThingStatsDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseThingStats.OnValueChanged = newVal =>
            {
                foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
                {
                    w.SetStats(newVal / 100f);
                }
            };

            PercentOfBaseThingFactors = modSettingsPack.GetHandle(
                "PercentOfBaseThingFactors",
                "PercentOfBaseThingFactors".Translate(),
                "PercentOfBaseThingFactorsDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBaseThingFactors.OnValueChanged = newVal =>
            {
                foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
                {
                    w.SetFactors(newVal / 100f);
                }
            };

            PercentOfBasePlantsWork = modSettingsPack.GetHandle(
                "PercentOfBasePlantsWork",
                "PercentOfBasePlantsWork".Translate(),
                "PercentOfBasePlantsWorkDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBasePlantsWork.OnValueChanged = newVal =>
            {
                foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantWorkAmount)))
                {
                    w.Set(newVal / 100f);
                }
            };

            PercentOfBasePlantsGrowDays = modSettingsPack.GetHandle(
                "PercentOfBasePlantsGrowDays",
                "PercentOfBasePlantsGrowDays".Translate(),
                "PercentOfBasePlantsGrowDaysDesc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            PercentOfBasePlantsGrowDays.OnValueChanged = newVal =>
            {
                foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantGrowDays)))
                {
                    w.Set(newVal / 100f);
                }
            };

            RepairJobAddX = modSettingsPack.GetHandle(
                "RepairJobAddX",
                "RepairJobAddX".Translate(),
                "RepairJobAddXDesc".Translate(),
                1,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 1000);
            if (HSKCollectJobsPatched)
            {
                PercentOfBaseHSKCollectJobs = modSettingsPack.GetHandle(
                    "PercentOfBaseHSKCollectJobs",
                    "PercentOfBaseHSKCollectJobs".Translate(),
                    "PercentOfBaseHSKCollectJobsDesc".Translate(),
                    100,
                    value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            }
            if (RFDrillJobPatched)
            {
                RFDrillJobMultiplier = modSettingsPack.GetHandle(
                    "RFDrillJobMultiplier",
                    "RFDrillJobMultiplier".Translate(),
                    "RFDrillJobMultiplierDesc".Translate(),
                    1f,
                    value => float.TryParse(value, out float num) && num >= 1f && num <= 1000f);
            }
            if (HSKMineQuarryPatched)
            {
                PercentOfBaseHSKMineQuarry = modSettingsPack.GetHandle(
                    "PercentOfBaseHSKMineQuarry",
                    "PercentOfBaseHSKMineQuarry".Translate(),
                    "PercentOfBaseHSKMineQuarryDesc".Translate(),
                    100,
                    value => int.TryParse(value, out int num) && num >= 1 && num <= 100);
            }


            DebugLog = modSettingsPack.GetHandle(
                "DebugLog",
                "DebugLog",
                "DebugLog",
                false);


            //SettingHandle<bool> handleRebalance = modSettingsPack.GetHandle("Rebalance", "RebalanceBtn".Translate(), "RebalanceBtnDesc".Translate(), false);
            //handleRebalance.CustomDrawer = rect =>
            //{
            //    if (Widgets.ButtonText(rect, "RebalanceBtn".Translate())) ApplySettings();
            //    return false;
            //};
            SettingHandle<bool> handleReset = modSettingsPack.GetHandle("Reset", "ResetBtn".Translate(), "ResetBtnDesc".Translate(), false);
            handleReset.CustomDrawer = rect =>
            {
                if (Widgets.ButtonText(rect, "ResetBtn".Translate()))
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
                    PercentOfBaseHSKCollectJobs?.ResetToDefault();
                    RFDrillJobMultiplier?.ResetToDefault();
                    PercentOfBaseHSKMineQuarry?.ResetToDefault();
                    
                    ApplySettings();
                    return true;
                }

                return false;
            };

            
        }

        // hostile detected
        public void ApplySettingsDefaults() => workDefDatabase.ForEach(w => w.Restore());

        public void ApplySettings()
        {
            //if (RestoreWhenHostileDetected && HostileDetected)
            //    return;

            //Loger.Clear();
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(ResearchWorkAmount)))
            {
                w.Set(PercentOfBaseResearches / 100f);
            }
                    
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(TerrainWorkAmount)))
            {
                w.Set(PercentOfBaseTerrains / 100f);
            }
                    
            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(RecipeWorkAmount)))
            {
                w.Set(PercentOfBaseRecipes / 100f);
            }
                    
            foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
            {
                w.SetStats(PercentOfBaseThingStats / 100f);
            }
                    
            foreach (ThingWorkAmount w in workDefDatabase.Where(x => x.GetType() == typeof(ThingWorkAmount)))
            {
                w.SetFactors(PercentOfBaseThingFactors / 100f);
            }

            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantWorkAmount)))
            {
                w.Set(PercentOfBasePlantsWork / 100f);
            }

            foreach (var w in workDefDatabase.Where(x => x.GetType() == typeof(PlantGrowDays)))
            {
                w.Set(PercentOfBasePlantsGrowDays / 100f);
            }
            //Loger.Save("dumpRebuilder.txt");
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

        public bool HSKCollectJobsPatched { get; }
        public bool RFDrillJobPatched { get; }
        public bool HSKMineQuarryPatched { get; }
        public bool HostileDetected { get; private set; }
    }
}
