using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ShinyShoe;
using ShinyShoe.Loading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Patty_RelicPicker_MOD
{
    internal class PatchList
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ShinyShoe.AppManager), "DoesThisBuildReportErrors")]
        public static void DisableErrorReportingPatch(ref bool __result)
        {
            __result = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AllGameManagers), "Start")]
        public static void SetupConfigs()
        {
            foreach (CollectableRelicData relic in AllGameManagers.Instance.GetAllGameData().GetAllCollectableRelicData())
            {
                var definition = new ConfigDefinition("Relic Effects", relic.name);
                Plugin.Entries[relic] = Plugin.Config.Bind<bool>(definition, false,
                                        new ConfigDescription(relic.GetDescription(), null, new ConfigurationManagerAttributes
                                        {
                                            Browsable = false
                                        }));
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.StartGame))]
        public static void StartGame(RunType runType)
        {
            if (runType == RunType.MalickaChallenge &&
                (bool)Plugin.EnableOnChallenge.BoxedValue == false)
            {
                return;
            }
            foreach (KeyValuePair<CollectableRelicData, ConfigEntry<bool>> pickedRelic in Plugin.Entries)
            {
                if (pickedRelic.Value.Value == false)
                {
                    continue;
                }
                CheatManager.Command_AddArtifact(pickedRelic.Key.name);
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(LoadScreen), "StartLoadingScreen")]
        public static void StartLoadingScreen(LoadScreen __instance, ref ScreenManager.ScreenActiveCallback ___screenActiveCallback)
        {
            if (__instance.name == ScreenName.RunSetup)
            {
                ___screenActiveCallback += delegate (IScreen screen)
                {
                    var runSetupScreen = UnityEngine.Object.FindObjectOfType<RunSetupScreen>();
                    var mutatorSelectionDialog = (MutatorSelectionDialog)AccessTools.Field(typeof(RunSetupScreen), "mutatorSelectionDialog")
                                                                                    .GetValue(runSetupScreen);
                    var pyreHeartButton = (GameUISelectableButton)AccessTools.Field(typeof(RunSetupScreen), "pyreHeartButton")
                                                                             .GetValue(runSetupScreen);
                    if (mutatorSelectionDialog == null ||
                        pyreHeartButton == null)
                    {
                        Plugin.LogSource.LogError("Waaaaaaaat");
                        return;
                    }
                    var clonedDialog = UnityEngine.Object.Instantiate(mutatorSelectionDialog, mutatorSelectionDialog.transform.parent.parent);
                    RelicSelectionDialog.Instance = clonedDialog.gameObject.AddComponent<RelicSelectionDialog>();
                    RelicSelectionDialog.Instance.name = nameof(RelicSelectionDialog);
                    RelicSelectionDialog.Instance.Setup();
                    UnityEngine.Object.DestroyImmediate(clonedDialog.gameObject.GetComponent<MutatorSelectionDialog>());

                    var clonedPyreHeartButton = UnityEngine.Object.Instantiate(pyreHeartButton.transform.parent.parent,
                                                                               pyreHeartButton.transform.parent.parent.parent);
                    clonedPyreHeartButton.gameObject.AddComponent<SetRelicsButton>();
                    UnityEngine.Object.DestroyImmediate(clonedPyreHeartButton.gameObject.GetComponent<PyreHeartInfoUI>());
                };
            }
        }
    }
}
