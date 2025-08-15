using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Patty_RelicPicker_MOD
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource { get; private set; }
        internal static Harmony PluginHarmony { get; private set; }
        internal static Dictionary<RelicData, ConfigEntry<bool>> Entries { get; private set; } = new Dictionary<RelicData, ConfigEntry<bool>>();
        internal new static ConfigFile Config { get; private set; }

        internal static ConfigEntry<bool> EnableOnChallenge { get; private set; }
        internal static ConfigEntry<bool> ShowUncollectableRelics { get; private set; }
        internal static ConfigEntry<bool> ShowEnemyRelics { get; private set; }
        internal static ConfigEntry<bool> ShowCovenantRelics { get; private set; }
        internal static ConfigEntry<bool> ShowMutatorRelics { get; private set; }
        internal static ConfigEntry<bool> ShowPyreArtifactRelics { get; private set; }
        internal static ConfigEntry<bool> ShowEndlessMutatorRelics { get; private set; }
        internal static ConfigEntry<bool> ShowEnhancerRelics { get; private set; }

        internal static ConfigEntry<bool> ShowTooltips { get; private set; }

        internal static bool InitializedConfigs { get; private set; }

        #region Translation
        internal static readonly OrderedDictionary ClanNameTranslationTerm = new OrderedDictionary
        {
            { "NonClass", "Clanless" },
            { "ClassData_titleLoc-604d44e6022d1c24-a3e4db5fc0afb9647906b33012f7b6e3-v2", "Banished" },
            { "ClassData_titleLoc-b946b201735c4048-fed615dbf2f84274ab8b72a7f7056fa8-v2", "Pyreborne" },
            { "ClassData_titleLoc-8338ffb122ab2e96-30528e09008d5c74fb51ff909ff75876-v2", "Luna Coven" },
            { "ClassData_titleLoc-9948d88fb75b25c9-d03f152bb38a72748891caa14769abd1-v2", "Underlegion" },
            { "ClassData_titleLoc-d85783c925521680-a6f5d6167ffd9dc4781b19278b89d2e1-v2", "Lazarus League" },
            { "ClassData_titleLoc-eb038694d9e044bb-b152a27f359a4e04cbcc29055c2f836b-v2", "Hellhorned" },
            { "ClassData_titleLoc-f76bea8450f06f67-55d2f9d7591683f4ca58a33311477d92-v2", "Awoken" },
            { "ClassData_titleLoc-37d27dbaadc5f40f-861c056fdeda9814284a85e9b3f034d0-v2", "Stygian Guard" },
            { "ClassData_titleLoc-2e445261f0cc3308-6f37f31f362b3c44e96df0656095657a-v2", "Umbra" },
            { "ClassData_titleLoc-1438fe314ad47795-95d25698eaac978488921909b1239bbc-v2", "Melting Remnant" }
        };

        internal static readonly OrderedDictionary RelicRarityTranslationTerm = new OrderedDictionary()
        {
            { "Compendium_Filter_CardType_All", "All" },
            { "CardRarity_Champion", "Champion" },
            { "CardRarity_Common", "Common" },
            { "CardRarity_Uncommon", "Uncommon" },
            { "CardRarity_Rare", "Rare" }
        };

        #endregion
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

            EnableOnChallenge = Config.Bind<bool>(new ConfigDefinition("Basic", "Enable on Challenge Run"), true,
                                                  new ConfigDescription("Enable to start Challenge modes with the relic you chose in the menu."));

            ShowUncollectableRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Uncollectable Relics"), true,
                                                        new ConfigDescription("Enable to show uncollectable relic (Some can be error). Proceed with caution."));

            ShowEnemyRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Enemy Relics"), false,
                                                new ConfigDescription("Enable to show enemy relic (Some can be error). Proceed with caution."));

            ShowCovenantRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Covenant Relics"), false,
                                                   new ConfigDescription("Enable to show covenant relic (Some can be error). Proceed with caution."));

            ShowMutatorRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Mutator Relics"), false,
                                                  new ConfigDescription("Enable to show mutator relic (Some can be error). Proceed with caution."));

            ShowPyreArtifactRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Pyre Artifacts Relics"), false,
                                                       new ConfigDescription("Enable to show pyre artifacts relic (Some can be error). Proceed with caution."));

            ShowEndlessMutatorRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Endless Mutator Relics"), true,
                                                         new ConfigDescription("Enable to show endless mutator relic (Some can be error). Proceed with caution."));

            ShowEnhancerRelics = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Enhancer Relics"), false,
                                                   new ConfigDescription("Enable to show enhancer relic (Some can be error). Proceed with caution."));

            ShowTooltips = Config.Bind<bool>(new ConfigDefinition("Basic", "Show Tooltips"), true,
                                             new ConfigDescription("Show tooltips when hovering into a relic in the menu."));

        }
        internal static AllGameData GetAllGameData()
        {
            return AllGameManagers.Instance.GetAllGameData();
        }

        internal static void InitializeConfigs()
        {
            if (InitializedConfigs || AllGameManagers.Instance == null)
            {
                return;
            }
            InitializedConfigs = true;
            foreach (RelicData relic in GetAllRelicDatas())
            {
                var definition = new ConfigDefinition("Relic Effects", relic.name);
                Entries[relic] = Config.Bind<bool>(definition, false,
                                        new ConfigDescription(relic.GetDescription(), null, new ConfigurationManagerAttributes
                                        {
                                            Browsable = false
                                        }));
            }
        }
        internal static IEnumerable<RelicData> GetAllRelicDatas()
        {
            AllGameData allGameData = AllGameManagers.Instance.GetAllGameData();
            IEnumerable<RelicData> allRelics;
            if (ShowUncollectableRelics.Value)
            {
                allRelics = allGameData.GetAllCollectableRelicData();
            }
            else
            {
                allRelics = allGameData.CollectAllAccessibleRelicDatas(AllGameManagers.Instance.GetSaveManager());
            }
            allRelics = allRelics.Union(allGameData.GetAllSinsDatas());
            allRelics = allRelics.Union(allGameData.GetAllCovenantDatas());
            allRelics = allRelics.Union(allGameData.GetAllMutatorData());
            allRelics = allRelics.Union(allGameData.GetAllPyreArtifactDatas());
            var endlessMutatorDatas = (List<EndlessMutatorData>)AccessTools.Field(typeof(AllGameData), "endlessMutatorDatas").GetValue(allGameData);
            allRelics = allRelics.Union(endlessMutatorDatas);
            allRelics = allRelics.Union(allGameData.GetAllEnhancerData());
            return allRelics.Where(delegate (RelicData relicData)
            {
                if (relicData == null)
                {
                    return false;
                }
                if (relicData is CollectableRelicData collectableRelicData)
                {
                    return AllGameManagers.Instance.GetSaveManager().IsDlcInstalled(collectableRelicData.GetRequiredDLC());
                }
                if (relicData is MutatorData mutatorData)
                {
                    return AllGameManagers.Instance.GetSaveManager().IsDlcInstalled(mutatorData.GetRequiredDLC());
                }
                return true;
            });
        }
    }
}
