using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using BepInEx.Configuration;
using System;
using System.Reflection;

using Path = System.IO.Path;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace SylmarDev.MaxQoL
{
	//This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]
	
	//This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	
	//We will be using 3 modules from R2API: ItemAPI to add our item, ItemDropAPI to have our item drop ingame, and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]

    // trying to make client side only
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html

    public class MaxQoL: BaseUnityPlugin
	{
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        
        public const string PluginAuthor = "SylmarDev";
        public const string PluginName = "MaxQoL";
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginVersion = "1.0.0";

        // assets
        public static AssetBundle assets;

        // config file
        //private static ConfigFile cfgFile;
        

        public List<ScrapperLocation> slocations = new List<ScrapperLocation>
        {
            new ScrapperLocation
            {
                Position = new Vector3(-82.1f, -23.7f, -5.2f),
                Rotation = new Vector3(0f, 72.6f, 0f)
            },
            new ScrapperLocation
            {
                Position = new Vector3(-95.1f, -25.2f, -45.2f),
                Rotation = new Vector3(0f, 72.6f, 0f)
            },
            new ScrapperLocation
            {
                Position = new Vector3(-134.1f, -25.4f, -20.2f),
                Rotation = new Vector3(0f, 72.6f, 0f)
            },
            new ScrapperLocation
            {
                Position = new Vector3(-122.1f, -23.7f, -5.2f),
                Rotation = new Vector3(0f, 72.6f, 0f)
            }
        };

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            // load assets (fingers crossed)
            Log.LogInfo("Loading Resources. . .");
            //using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdrianInfo.adrianitems_assets"))
            //{
            //    assets = AssetBundle.LoadFromStream(stream);
            //}

            Log.LogInfo("Assigning hooks. . .");
            // todo
            // instant scrapper and printer
            // scrapper in shop
            On.RoR2.BazaarController.Awake += BazaarController_Awake;

            // ping portals in shop to tell which it is
            On.RoR2.Util.GetBestBodyName += Util_GetBestBodyName;
            // teleporter instantly finishes after boss
            On.RoR2.TeleporterInteraction.UpdateMonstersClear += TeleporterInteraction_UpdateMonstersClear;
            // red chest and newt altars are pinged at the start of the map
            // guarenteed cleansing pool on sanctuary
            // one frog pet
            On.RoR2.FrogController.Pet += FrogController_Pet;


            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        

        private void BazaarController_Awake(On.RoR2.BazaarController.orig_Awake orig, BazaarController self)
        {
            orig(self);
            bool active = NetworkServer.active;
            var i = 0;
            if (active)
            {
                foreach (ScrapperLocation location in slocations)
                {
                    if (i >= PlayerCharacterMasterController.instances.Count)
                    {
                        break;
                    }
                    SpawnScrapper(location);
                    i++;
                }
            }
        }

        private void SpawnScrapper(ScrapperLocation location)
        {
            DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule();
            directorPlacementRule.placementMode = 0;
            SpawnCard spawnCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscScrapper");
            GameObject spawnedInstance = spawnCard.DoSpawn(location.Position, Quaternion.identity, new DirectorSpawnRequest(spawnCard, directorPlacementRule, Run.instance.runRNG)).spawnedInstance;
            spawnedInstance.transform.eulerAngles = location.Rotation;
            NetworkServer.Spawn(spawnedInstance);
        }

        private string Util_GetBestBodyName(On.RoR2.Util.orig_GetBestBodyName orig, GameObject bodyObject)
        {
            var name = bodyObject.name;
            var flag = name.Contains("SeerStation");
            string result = "";

            if (flag)
            {
                SeerStationController component = bodyObject.GetComponent<SeerStationController>();
                SceneIndex networktargetSceneDefIndex = (SceneIndex)component.NetworktargetSceneDefIndex;
                //Log.LogMessage(Language.GetString(SceneCatalog.GetSceneDef(networktargetSceneDefIndex).portalSelectionMessageString));
                var seerText = Language.GetString(SceneCatalog.GetSceneDef(networktargetSceneDefIndex).portalSelectionMessageString).Substring(31);
                result = "A dream of" + seerText.Remove(seerText.Length - 2, 2) + " ";
            } else
            {
                result = orig(bodyObject);
            }
            return result;
        }

        private void TeleporterInteraction_UpdateMonstersClear(On.RoR2.TeleporterInteraction.orig_UpdateMonstersClear orig, TeleporterInteraction self)
        {
            orig(self);
            if (self.monstersCleared && self.holdoutZoneController && self.activationState == TeleporterInteraction.ActivationState.Charging && self.chargeFraction > 0.02f)
            {
                int displayChargePercent = TeleporterInteraction.instance.holdoutZoneController.displayChargePercent;
                float runStopwatch = Run.instance.GetRunStopwatch();
                int num = Math.Min(Util.GetItemCountForTeam(self.holdoutZoneController.chargingTeam, RoR2Content.Items.FocusConvergence.itemIndex, true, true), 3);
                float num2 = (100f - (float)displayChargePercent) / 100f * (TeleporterInteraction.instance.holdoutZoneController.baseChargeDuration / (1f + 0.3f * (float)num));
                num2 = (float)Math.Round(num2, 2);
                float runStopwatch2 = runStopwatch + (float)Math.Round((double)num2, 2);
                Run.instance.SetRunStopwatch(runStopwatch2);
                TeleporterInteraction.instance.holdoutZoneController.FullyChargeHoldoutZone();
            }
        }

        private void FrogController_Pet(On.RoR2.FrogController.orig_Pet orig, FrogController self, Interactor interactor)
        {
            self.maxPets = 1;
            orig(self, interactor);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.LogMessage($"Current Coords: {transform.position}");
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex((ItemIndex) 107), transform.position, transform.forward * 20f);
            }
        }
    }

    public class ScrapperLocation
    {
        public Vector3 Position;
        public Vector3 Rotation;
    }
}
