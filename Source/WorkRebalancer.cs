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
        
        /// <summary>
        /// Get from workDefDatabase all T : IWorkAmount types and apply percent value
        /// </summary>
        /// <typeparam name="T">IWorkAmount type</typeparam>
        /// <param name="value">percent value</param>
        /// <param name="customAction">if defined: customAction(workAmount, percentValue)</param>
        public void ApplySetting<T>(int value, Action<T, float> customAction = null) where T : IWorkAmount 
        {
            float percent = value / 100f;
            foreach (T w in workDefDatabase.Where(x => x.GetType() == typeof(T)))
            {
                if (customAction == null) w.Set(percent); else customAction(w, percent);
            }
        }

        public void CreateWorkAmountSetting<T>(ref SettingHandle<int> settingHandle, string settingName, Action<T, float> customAction = null) where T : IWorkAmount 
        {
            settingHandle = modSettingsPack.GetHandle(
                settingName,
                settingName.Translate(),
                $"{settingName}Desc".Translate(),
                100,
                value => int.TryParse(value, out int num) && num >= 1 && num <= 100);

            settingHandle.OnValueChanged = newVal =>
            {
                ApplySetting(newVal, customAction);
            };
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
            CreateWorkAmountSetting<ResearchWorkAmount>(ref PercentOfBaseResearches, "PercentOfBaseResearches");
            CreateWorkAmountSetting<TerrainWorkAmount>(ref PercentOfBaseTerrains, "PercentOfBaseTerrains");
            CreateWorkAmountSetting<RecipeWorkAmount>(ref PercentOfBaseRecipes, "PercentOfBaseRecipes");
            CreateWorkAmountSetting<ThingWorkAmount>(ref PercentOfBaseThingStats, "PercentOfBaseThingStats", (w, p) => w.SetStats(p));
            CreateWorkAmountSetting<ThingWorkAmount>(ref PercentOfBaseThingFactors, "PercentOfBaseThingFactors", (w, p) => w.SetFactors(p));
            CreateWorkAmountSetting<PlantWorkAmount>(ref PercentOfBasePlantsWork, "PercentOfBasePlantsWork");
            CreateWorkAmountSetting<PlantGrowDays>(ref PercentOfBasePlantsGrowDays, "PercentOfBasePlantsGrowDays");

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
            ApplySetting<ResearchWorkAmount>(PercentOfBaseResearches);
            ApplySetting<TerrainWorkAmount>(PercentOfBaseTerrains);
            ApplySetting<RecipeWorkAmount>(PercentOfBaseRecipes);
            ApplySetting<ThingWorkAmount>(PercentOfBaseThingStats, (w, p) => w.SetStats(p));
            ApplySetting<ThingWorkAmount>(PercentOfBaseThingFactors, (w, p) => w.SetFactors(p));
            ApplySetting<PlantWorkAmount>(PercentOfBasePlantsWork);
            ApplySetting<PlantGrowDays>(PercentOfBasePlantsGrowDays);
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
