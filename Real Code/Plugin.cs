using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using RockCompany.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GameNetcodeStuff;

namespace RockCompany
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class RockCompanyBase : BaseUnityPlugin
    {
        private const string modGUID = "Jaydesonia.RockCompany";
        private const string modName = "Rock Company";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static RockCompanyBase Instance;

        internal ManualLogSource mls;
        public static AssetBundle MyCustomAssets;
        private Throwable script;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            mls.LogInfo("The mod has awoke :)");

            harmony.PatchAll(typeof(RockCompanyBase));

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MyCustomAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "mymodbundle/mymodassets"));
            if (MyCustomAssets == null)
            {
                mls.LogError("Failed to load custom assets."); // ManualLogSource for your plugin
                return;
            }

            //Spawn chance, set to 1000 for testing, 60 for not
            int iRarity = 60;
            Item customRock = MyCustomAssets.LoadAsset<Item>("Rock.asset");
            script = customRock.spawnPrefab.AddComponent<Throwable>();
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.isInFactory = true;
            script.itemProperties = customRock;

            customRock.minValue = 14;
            customRock.maxValue = 28;
            LethalLib.Modules.Utilities.FixMixerGroups(customRock.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(customRock.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(customRock, iRarity, LethalLib.Modules.Levels.LevelTypes.All);
            //LethalLib.Modules.Items.RegisterShopItem(customRock, 0);
        }

    }
}
