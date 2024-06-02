using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections.Generic;
using EntityStates;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using EntityStates.Duplicator;


namespace SylmarDev.KannasQoL
{
    [BepInDependency(DirectorAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]

    public class KannasQoL: BaseUnityPlugin
	{      
        public const string PluginAuthor = "SylmarDev";
        public const string PluginName = "KannasQualityofLife";
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginVersion = "0.2.1";

        // assets
        public static AssetBundle assets;

        // config file
        //private static ConfigFile cfgFile;

        public static float zeroInitialDelayDuration;
        public static float zeroTimeBetweenStartAndDropDroplet;


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

            new KannasConfig().Init(Paths.ConfigPath);

            // load assets (fingers crossed)
            Log.LogInfo("Loading Resources. . .");
            //using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdrianInfo.adrianitems_assets"))
            //{
            //    assets = AssetBundle.LoadFromStream(stream);
            //}

            Log.LogInfo("Assigning hooks. . .");

            // instant scrapper and printer
            if (KannasConfig.enableFastScrapper.Value)
            {
                On.RoR2.Stage.Start += Stage_Start;
                On.EntityStates.Duplicator.Duplicating.BeginCooking += Duplicating_BeginCooking;

                IL.EntityStates.Duplicator.Duplicating.FixedUpdate += (il) =>
                {
                    ILCursor ilcursor = new ILCursor(il);
                    ILCursor ilcursor2 = ilcursor;
                    Func<Instruction, bool>[] array = new Func<Instruction, bool>[9];
                    array[0] = ((Instruction x) => ILPatternMatchingExt.MatchCallOrCallvirt<EntityState>(x, "get_fixedAge"));
                    array[1] = ((Instruction x) => ILPatternMatchingExt.MatchLdsfld(x, typeof(Duplicating).GetField("initialDelayDuration")));
                    array[2] = ((Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Blt_Un_S));
                    array[3] = ((Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Ldarg_0));
                    array[4] = ((Instruction x) => ILPatternMatchingExt.MatchCallOrCallvirt<Duplicating>(x, "BeginCooking"));
                    array[5] = ((Instruction x) => ILPatternMatchingExt.Match(x, OpCodes.Ldarg_0));
                    array[6] = ((Instruction x) => ILPatternMatchingExt.MatchCallOrCallvirt<EntityState>(x, "get_fixedAge"));
                    array[7] = ((Instruction x) => ILPatternMatchingExt.MatchLdsfld(x, typeof(Duplicating).GetField("initialDelayDuration")));
                    array[8] = ((Instruction x) => ILPatternMatchingExt.MatchLdsfld(x, typeof(Duplicating).GetField("timeBetweenStartAndDropDroplet")));
                    bool flag = ilcursor2.TryGotoNext(array);
                    if (flag)
                    {
                        ilcursor.Index++;
                        ilcursor.Remove();
                        ilcursor.Emit<KannasQoL>(OpCodes.Ldsfld, "zeroInitialDelayDuration");
                        ilcursor.Index += 5;
                        ilcursor.Remove();
                        ilcursor.Emit<KannasQoL>(OpCodes.Ldsfld, "zeroInitialDelayDuration");
                        ilcursor.Remove();
                        ilcursor.Emit<KannasQoL>(OpCodes.Ldsfld, "zeroTimeBetweenStartAndDropDroplet");
                    }
                    else
                    {
                        Log.LogError("Printer couldn't shortcut");
                    }
                };

                On.EntityStates.Duplicator.Duplicating.DropDroplet += Duplicating_DropDroplet;
            }

            // scrapper in shop
            if (KannasConfig.enableBazaarScrapper.Value) On.RoR2.BazaarController.Awake += BazaarController_Awake;

            // ping lunar seers in shop to tell which it is
            if (KannasConfig.enableSeerPing.Value) On.RoR2.Util.GetBestBodyName += Util_GetBestBodyName;

            // teleporter instantly finishes after boss
            if (KannasConfig.enableInstaTeleporter.Value) On.RoR2.TeleporterInteraction.UpdateMonstersClear += TeleporterInteraction_UpdateMonstersClear;

            // red chest and newt altars and preon chest are pinged at the start of the map
            // wip

            //// guarenteed cleansing pool on sanctuary
            if (KannasConfig.enableCleansingPool.Value) On.RoR2.Stage.Start += Stage_Start_CleansingPool;

            // one frog pet
            if (KannasConfig.enableSingleFrog.Value) On.RoR2.FrogController.Pet += FrogController_Pet;
            if (KannasConfig.frogStatueCost.Value != 1) On.RoR2.PurchaseInteraction.Awake += PurchaseInteraction_Awake;


            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        private void Stage_Start(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);

            EntityStates.Scrapper.WaitToBeginScrapping.duration = 0f;
            EntityStates.Scrapper.Scrapping.duration = 0f;
            EntityStates.Scrapper.ScrappingToIdle.duration = 0f;

            // probably obsolete but why chance it
            Duplicating.initialDelayDuration = 0.01f;
            Duplicating.timeBetweenStartAndDropDroplet = 0.01f;
        }

        private void Duplicating_BeginCooking(On.EntityStates.Duplicator.Duplicating.orig_BeginCooking orig, EntityStates.Duplicator.Duplicating self)
        {
            bool flag = !NetworkServer.active;
            if (flag)
            {
                orig(self);
            }
        }

        private void Duplicating_DropDroplet(On.EntityStates.Duplicator.Duplicating.orig_DropDroplet orig, EntityStates.Duplicator.Duplicating self)
        {
            orig(self);
            bool active = NetworkServer.active;
            if (active)
            {
                self.outer.GetComponent<PurchaseInteraction>().Networkavailable = true;
            }
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
            string result;
            if (flag)
            {
                SeerStationController component = bodyObject.GetComponent<SeerStationController>();
                SceneIndex networktargetSceneDefIndex = (SceneIndex)component.NetworktargetSceneDefIndex;
                //Log.LogMessage(Language.GetString(SceneCatalog.GetSceneDef(networktargetSceneDefIndex).portalSelectionMessageString));
                var seerText = Language.GetString(SceneCatalog.GetSceneDef(networktargetSceneDefIndex).portalSelectionMessageString).Substring(31).Trim();
                Log.LogMessage(seerText);
                result = $"A dream of {seerText.Split('<')[0]} ";
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
                float num2 = (100f - displayChargePercent) / 100f * (TeleporterInteraction.instance.holdoutZoneController.baseChargeDuration / (1f + 0.3f * num));
                num2 = (float)Math.Round(num2, 2);
                float runStopwatch2 = runStopwatch + (float)Math.Round((double)num2, 2);
                Run.instance.SetRunStopwatch(runStopwatch2);
                TeleporterInteraction.instance.holdoutZoneController.FullyChargeHoldoutZone();
            }
        }

        private void Stage_Start_CleansingPool(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            if (SceneInfo.instance.sceneDef.baseSceneName == "ancientloft")
            {
                //Log.LogMessage("again, it, it just works");
                DirectorPlacementRule directorPlacementRule = new();
                directorPlacementRule.placementMode = 0;
                SpawnCard spawnCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineCleanse");
                Vector3 loc = new Vector3(-68.0f, 40.5f, 6.3f);
                GameObject spawnedInstance = spawnCard.DoSpawn(loc, Quaternion.identity, new DirectorSpawnRequest(spawnCard, directorPlacementRule, Run.instance.runRNG)).spawnedInstance;
                spawnedInstance.transform.eulerAngles = new Vector3(0f, 0f, 0f);
                NetworkServer.Spawn(spawnedInstance);
            }
        }

        private void FrogController_Pet(On.RoR2.FrogController.orig_Pet orig, FrogController self, Interactor interactor)
        {
            self.maxPets = 1;
            Log.LogInfo(self.purchaseInteraction.name);
            orig(self, interactor);
        }

        private void PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            orig(self);
            AdjustFrogPrice(self);
        }

        private static void AdjustFrogPrice(PurchaseInteraction self)
        {
            if (self.name.StartsWith("FrogInteractable"))
            {
                self.cost = KannasConfig.frogStatueCost.Value;
                if (KannasConfig.frogStatueCost.Value == 0) self.costType = CostTypeIndex.None;
            }
        }

        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.F2))
        //    {
        //        var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
        //        Log.LogMessage($"Current Coords: {transform.position}");
        //        Log.LogMessage(SceneInfo.instance.sceneDef.baseSceneName);
        //    }

        //    if (Input.GetKeyDown(KeyCode.F3))
        //    {
        //        var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
        //        Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
        //        PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex((ItemIndex) 107), transform.position, transform.forward * 20f);
        //    }
        //}
    }

    public class ScrapperLocation
    {
        public Vector3 Position;
        public Vector3 Rotation;
    }
}
