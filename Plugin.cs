using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patty_RelicPicker_MOD
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource { get; private set; }
        internal static Harmony PluginHarmony { get; private set; }
        internal static HashSet<CollectableRelicData> PickedRelics { get; private set; } = new HashSet<CollectableRelicData>();
        internal static Dictionary<CollectableRelicData, ConfigEntry<bool>> Entries { get; private set; } = new Dictionary<CollectableRelicData, ConfigEntry<bool>>();
        internal new static ConfigFile Config { get; private set; }
        internal static ConfigEntry<bool> EnableOnChallenge { get; private set; }
        internal static bool InitializedConfigs { get; private set; }
        void Awake()
        {
            Config = base.Config;
            LogSource = Logger;
            try
            {
                PluginHarmony = Harmony.CreateAndPatchAll(typeof(PatchList), PluginInfo.GUID);
            }
            catch (HarmonyException ex)
            {
                LogSource.LogError((ex.InnerException ?? ex).Message); 
            }

            EnableOnChallenge = Config.Bind<bool>(new ConfigDefinition("Challenge Run", "Enable on Challenge Run"), true,
                                                  new ConfigDescription("Turn this on to start Challenge modes with the relic you chose in the menu."));

        }

        internal static void InitializeConfigs()
        {
            if (InitializedConfigs || AllGameManagers.Instance == null)
            {
                return;
            }
            InitializedConfigs = true;
            foreach (CollectableRelicData relic in GetAllRelicDatas())
            {
                var definition = new ConfigDefinition("Relic Effects", relic.name);
                Entries[relic] = Config.Bind<bool>(definition, false,
                                        new ConfigDescription(relic.GetDescription(), null, new ConfigurationManagerAttributes
                                        {
                                            Browsable = false
                                        }));
            }
        }
        internal static IEnumerable<CollectableRelicData> GetAllRelicDatas()
        {
            AllGameData allGameData = AllGameManagers.Instance.GetAllGameData();
            IReadOnlyList<CollectableRelicData> allRelics = allGameData.GetAllCollectableRelicData();
            return allRelics.Where(relic => relic != null &&
            AllGameManagers.Instance.GetSaveManager().IsDlcInstalled(relic.GetRequiredDLC()));
        }
    }
}
