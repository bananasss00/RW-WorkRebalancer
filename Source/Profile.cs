using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using HarmonyLib;
using HugsLib.Settings;
using Verse;

namespace WorkRebalancer
{
    public class Profile
    {
        public static string[] GetAllProfiles()
        {
            return Directory
                .GetFiles(FS.FolderUnderSaveData("WorkRebalancer"), "*.xml", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToArray();
        }
        public void Load(string name)
        {
            string fn = $"{FS.FolderUnderSaveData("WorkRebalancer")}\\{name}";

            var fields = typeof(Profile).GetFields()
                .Where(x => x.FieldType.IsGenericType).ToDictionary(x => x.Name, y => y);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(File.ReadAllText(fn));
            var root = xmlDocument.DocumentElement;
            foreach (XmlNode element in root)
            {
                if (fields.TryGetValue(element.Name, out var fi))
                {
                    var fieldInst = fi.GetValue(this);
                    if (fieldInst != null)
                    {
                        string value = element.Attributes.GetNamedItem("value").Value;
                        Traverse.Create(fieldInst).Property("StringValue").SetValue(value);
                    }
                }
            }

            UpdateHugslibControls();
        }

        public void Save(string name)
        {
            string fn = $"{FS.FolderUnderSaveData("WorkRebalancer")}\\{name}.xml";
            XmlDocument xmlDocument = new XmlDocument();
            
            var settings = xmlDocument.CreateElement("Settings");
            foreach (var fi in typeof(Profile).GetFields().Where(x => x.FieldType.IsGenericType))
            {
                var element = xmlDocument.CreateElement(fi.Name);
                
                var fieldInst = fi.GetValue(this);
                string value = null;
                if (fieldInst != null)
                {
                    value = Traverse.Create(fieldInst).Method("get_Value").GetValue().ToString();
                }

                element.SetAttribute("value", value);
                settings.AppendChild(element);
            }

            xmlDocument.AppendChild(settings);
            xmlDocument.Save(fn);
        }
        public static void Delete(string name)
        {
            string fn = $"{FS.FolderUnderSaveData("WorkRebalancer")}\\{name}";
            File.Delete(fn);
        }

        public void ResetToDefault()
        {
            RestoreWhenHostileDetected?.ResetToDefault();
            CheckHostileDelay?.ResetToDefault();
            ShowInstantMovingIcon?.ResetToDefault();
            ShowFastPawnsTicksIcon?.ResetToDefault();
            ShowFastTimeIcon?.ResetToDefault();
            InstantMovingAutooffOnPause?.ResetToDefault();
            InstantMovingOnlyColonists?.ResetToDefault();
            InstantMovingSmoother?.ResetToDefault();
            FastTimeMultiplier?.ResetToDefault();
            FastPawnsTicksMultiplier?.ResetToDefault();
            PercentOfBaseResearches?.ResetToDefault();
            PercentOfBaseTerrains?.ResetToDefault();
            PercentOfBaseRecipes?.ResetToDefault();
            PercentOfBaseThingStats?.ResetToDefault();
            PercentOfBaseThingFactors?.ResetToDefault();
            PercentOfBasePlantsWork?.ResetToDefault();
            PercentOfBasePlantsGrowDays?.ResetToDefault();
            PercentOfBaseMineJob?.ResetToDefault();
            PercentOfBaseTendPatient?.ResetToDefault();
            DeepScannerJob?.ResetToDefault();
            PercentOfBaseFluffyBreakdowns?.ResetToDefault();
            RepairJobAddX?.ResetToDefault();
            PercentOfBaseHSKCollectJobs?.ResetToDefault();
            AndroidsCraftAddX?.ResetToDefault();
            RFDrillJobMultiplier?.ResetToDefault();
            RFCrudeJobMultiplier?.ResetToDefault();
            RFRefineryJobMultiplier?.ResetToDefault();
            PercentOfBaseHSKMineQuarry?.ResetToDefault();
            PercentOfBaseHSKExtractorsMine?.ResetToDefault();
            SkillLearnMultiplier?.ResetToDefault();
            SkillLearnAllowMax?.ResetToDefault();

            PawnSpeedMultBeforeCutoff?.ResetToDefault();
            PawnSpeedMultAfterCutoff?.ResetToDefault();
            PawnCutoffAge?.ResetToDefault();
            AnimalSpeedMultBeforeCutoff?.ResetToDefault();
            AnimalSpeedMultAfterCutoff?.ResetToDefault();
            AnimalCutoffAge?.ResetToDefault();
            RjwPregnancySpeedMult?.ResetToDefault();
            RjwInsectEggSpeedMult?.ResetToDefault();
            EggHatchSpeedMult?.ResetToDefault();
            EggLayerSpeedMult?.ResetToDefault();

            UpdateHugslibControls();
        }

        public SettingHandle<bool> ShowInstantMovingIcon;
        public SettingHandle<bool> ShowFastPawnsTicksIcon;
        public SettingHandle<bool> ShowFastTimeIcon;
        public SettingHandle<bool> InstantMovingAutooffOnPause;
        public SettingHandle<bool> InstantMovingOnlyColonists;
        public SettingHandle<bool> InstantMovingSmoother;
        public SettingHandle<int> FastTimeMultiplier;
        public SettingHandle<int> FastPawnsTicksMultiplier;
        public SettingHandle<bool> RestoreWhenHostileDetected;
        public SettingHandle<int> CheckHostileDelay;
        public SettingHandle<int> PercentOfBaseResearches;
        public SettingHandle<int> PercentOfBaseTerrains;
        public SettingHandle<int> PercentOfBaseRecipes;
        public SettingHandle<int> PercentOfBaseThingStats;
        public SettingHandle<int> PercentOfBaseThingFactors;
        public SettingHandle<int> PercentOfBasePlantsWork;
        public SettingHandle<int> PercentOfBasePlantsGrowDays;
        public SettingHandle<int> PercentOfBaseMineJob;
        public SettingHandle<int> PercentOfBaseTendPatient;
        public SettingHandle<float> DeepScannerJob;  
        public SettingHandle<int> PercentOfBaseFluffyBreakdowns;
        public SettingHandle<int> RepairJobAddX;
        public SettingHandle<int> PercentOfBaseHSKCollectJobs;
        public SettingHandle<int> AndroidsCraftAddX;
        public SettingHandle<float> RFDrillJobMultiplier;  
        public SettingHandle<float> RFCrudeJobMultiplier;  
        public SettingHandle<float> RFRefineryJobMultiplier;  
        public SettingHandle<int> PercentOfBaseHSKMineQuarry;
        public SettingHandle<int> PercentOfBaseHSKExtractorsMine;
        public SettingHandle<float> SkillLearnMultiplier;
        public SettingHandle<int> SkillLearnAllowMax;

        // Fast aging
        public SettingHandle<int> PawnSpeedMultBeforeCutoff; //Actual value of the pawn speed multiplier before cutoff setting
        public SettingHandle<int> PawnSpeedMultAfterCutoff; //Actual value of the pawn speed multiplier after cutoff setting
        public SettingHandle<int> PawnCutoffAge; //Actual value of the pawn cutoff age setting
        public SettingHandle<int> AnimalSpeedMultBeforeCutoff; //Actual value of the animal speed multiplier before cutoff setting
        public SettingHandle<int> AnimalSpeedMultAfterCutoff; //Actual value of the animal speed multiplier after cutoff setting
        public SettingHandle<int> AnimalCutoffAge; //Actual value of the animal cutoff age setting
        public SettingHandle<float> RjwPregnancySpeedMult;
        public SettingHandle<int> RjwInsectEggSpeedMult;
        public SettingHandle<float> EggHatchSpeedMult;
        public SettingHandle<float> EggLayerSpeedMult;


        /// <summary>
        /// It's rebuild method Dialog_ModSettings.ResetSetting. IDK how update legacy
        ///
        /// Dictionary<SettingHandle, Dialog_ModSettings.HandleControlInfo> handleControlInfo;
        /// 
        /// private void ResetSetting(SettingHandle handle) {
        ///   handle.ResetToDefault();
        ///   this.handleControlInfo[handle] = new Dialog_ModSettings.HandleControlInfo(handle); // private class
        ///   this.settingsHaveChanged = true;
        /// }
        /// 
        /// </summary>
        /// <param name="className"></param>
        private void UpdateHugslibControls()
        {
            var modSettings = Find.WindowStack.WindowOfType<Dialog_ModSettings>();
            if (modSettings == null)
            {
                Log.Error("Can't find hugslib type: Dialog_ModSettings");
                return;
            }
            var dictionary = Traverse.Create(modSettings).Field("handleControlInfo").GetValue();
            if (dictionary == null)
            {
                Log.Error("Can't find handleControlInfo field");
                return;
            }

            var HandleControlInfoType = modSettings.GetType()
                .GetNestedTypes(BindingFlags.NonPublic)
                .FirstOrDefault(x => x.Name.Equals("HandleControlInfo")); // AccessTools.TypeByName("HandleControlInfo");
            if (HandleControlInfoType == null)
            {
                Log.Error("Can't find inner class HandleControlInfo of Dialog_ModSettings");
                return;
            }

            PropertyInfo indexProperty = dictionary.GetType()
                .GetProperties()
                .Single(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(SettingHandle));

            foreach (var fi in typeof(Profile).GetFields().Where(x => x.FieldType.IsGenericType))
            {
                if (fi.GetValue(this) is SettingHandle handle)
                {
                    var newInfo = Activator.CreateInstance(HandleControlInfoType, handle);
                    indexProperty.SetValue(dictionary, newInfo, new object[] {handle});
                }
            }
            Traverse.Create(modSettings).Field("settingsHaveChanged").SetValue(true);
        }
    }
}