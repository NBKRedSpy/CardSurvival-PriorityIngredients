using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PriorityIngredients
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; set; }
        public static ConfigEntry<string> CardPriorityList { get; private set; }

        private void Awake()
        {

            Log = Logger;

            CardPriorityList = Config.Bind("General", nameof(CardPriorityList),
                "LQ_Oil,Stone,StoneSharpened,StoneAxe,AxeFlint,AxeCopper,AxeScrap,AxeSurvival,AxeSurvivalBlunt",
                @"A comma delimited list of card names. Indicates which cards to search for first before any searching for any other compatible cards. A list of cards can be found at https://github.com/NBKRedSpy/CardSurvival-DoNotSteal/blob/master/CardList.txt");

            Config.SettingChanged += Config_SettingChanged;

            Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {

            try
            {
                Plugin.LogInfo($"Reloaded {DateTime.Now}");

                BlueprintConstructionPopup_AutoFill_Patch.LoadCardPrioity();
            }
            catch (System.Exception)
            {
                Plugin.Log.LogError($"Error changing {e.ChangedSetting.Definition.Key} settings");
                throw;
            }

        }


        public static void LogInfo(string text)
        {
            Plugin.Log.LogInfo(text);
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            GameObject searchObject = obj;

            string path = "/" + searchObject.name;
            while (searchObject.transform.parent != null)
            {
                searchObject = searchObject.transform.parent.gameObject;
                path = "/" + searchObject.name + path;
            }
            return path;
        }

    }
}