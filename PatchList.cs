using HarmonyLib;
using ShinyShoe;
using ShinyShoe.Loading;
using System.Linq;

namespace Patty_RelicPicker_MOD
{
    internal class PatchList
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ShinyShoe.AppManager), "DoesThisBuildReportErrors")]
        public static void DisableErrorReportingPatch(ref bool __result)
        {
            __result = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.StartGame))]
        public static void StartGame(RunType runType)
        {
            if (runType == RunType.MalickaChallenge &&
                Plugin.EnableOnChallenge.Value == false)
            {
                return;
            }
            foreach (var entry in Plugin.Entries.OrderBy(pair => pair.Key.GetName()))
            {
                if (entry.Value.Value == false)
                {
                    continue;
                }
                if (entry.Key is SinsData &&
                    Plugin.ShowEnemyRelics.Value == false)
                {
                    continue;
                }
                if (entry.Key is CovenantData &&
                    Plugin.ShowCovenantRelics.Value == false)
                {
                    continue;
                }
                if (entry.Key is MutatorData &&
                    Plugin.ShowMutatorRelics.Value == false)
                {
                    continue;
                }
                if (entry.Key is PyreArtifactData &&
                    Plugin.ShowPyreArtifactRelics.Value == false)
                {
                    continue;
                }
                if (entry.Key is EndlessMutatorData &&
                    Plugin.ShowEndlessMutatorRelics.Value == false)
                {
                    continue;
                }
                if (entry.Key is EnhancerData &&
                    Plugin.ShowEnhancerRelics.Value == false)
                {
                    continue;
                }
                AllGameManagers.Instance.GetSaveManager().AddRelic(entry.Key);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(LoadScreen), "StartLoadingScreen")]
        public static void StartLoadingScreen(LoadScreen __instance, ref ScreenManager.ScreenActiveCallback ___screenActiveCallback)
        {
            Plugin.InitializeConfigs();
            if (__instance.name == ScreenName.RunSetup)
            {
                ___screenActiveCallback += delegate (IScreen screen)
                {
                    var runSetupScreen = (RunSetupScreen)screen;
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
