using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using ShinyShoe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Patty_RelicPicker_MOD
{
    internal class RelicSelectionDialog : MonoBehaviour
    {
        internal static readonly Color BGColor = new Color(0, 0, 0, 0.902f);

        ScreenDialog dialog;
        ScrollRect scrollRect;
        RelicButtonUI layout;
        Button closeButton;
        List<RelicButtonUI> relicButtons = new List<RelicButtonUI>();
        TextMeshProUGUI title, warning;

        internal Image bg;
        internal static RelicSelectionDialog Instance { get; set; }
        bool initialized, initializedOptions;

        internal static string allClanName;
        internal static string selectedCustom, selectedFactionString;
        internal static ClassData selectedFaction;
        internal static CollectableRarity selectedRarity = CollectableRarity.Common;

        internal readonly HashSet<string> FactionNames = new HashSet<string>();
        internal readonly HashSet<string> CustomNames = new HashSet<string>();
        internal readonly OrderedDictionary RelicFactionOptions = new OrderedDictionary();
        internal readonly OrderedDictionary RelicRarityOptions = new OrderedDictionary();
        internal readonly OrderedDictionary CustomOptions = new OrderedDictionary();

        internal GameUISelectableDropdown dropdownPrefab;
        internal GameUISelectableDropdown factionDropdown;
        internal GameUISelectableDropdown rarityDropdown;
        internal GameUISelectableDropdown customDropdown;


        internal RelicInfoUI tooltipProvider;
        internal UIElementTooltipContainer tooltipContainer;
        internal RectTransform tooltipParent;
        internal RelicButtonUI focusedRelicButton;

        internal Transform preview;

        void Awake()
        {
            Setup();
        }

        void OnEnable()
        {
            Open();
        }

        void OnDisable()
        {
            Close();
        }

        internal void Open()
        {
            Setup();

            if (!initializedOptions)
            {
                initializedOptions = true;
                StartCoroutine(CreateDropdownList());
                StartCoroutine(CreateCustomDropdown());
            }
            else
            {
                ResetOrder();
            }

            // Sets the menu on top
            transform.SetAsLastSibling();
            dialog.SetActive(true, gameObject);
        }

        internal void ResetOrder()
        {
            // Order by enabled, then by name
            relicButtons = relicButtons.OrderByDescending(relicButton => Plugin.Entries[relicButton.Data].Value)
                                       .ThenBy(relicButton => relicButton.Data.Cheat_GetNameEnglish()).ToList();
            for (int i = 0; i < relicButtons.Count; i++)
            {
                RelicButtonUI relicButton = relicButtons[i];
                relicButton.ToggleChosenState(Plugin.Entries[relicButton.Data].Value);
                relicButton.transform.SetSiblingIndex(i);
            }
        }
        internal void Close()
        {
            Setup();
            dialog.SetActive(false, gameObject);
        }

        // Many failsafe check in case the menu is trying to open without being initialized
        internal void Setup()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;

            bg = transform.Find("Dialog Overlay").GetComponent<Image>();
            InitializeComponents();
            SetupDialogText();
            CreateTemplateButton();
            SetupCloseButton();
            GenerateRelicEntry();
            SetupOptions();
            SetupResetButton(FindObjectOfType<UIFooter>());
            SetupTooltip();
        }

        internal void SetupTooltip()
        {
            if (!Plugin.ShowTooltips.Value)
            {
                return;
            }
            if (tooltipParent != null)
            {
                return;
            }
            tooltipParent = new GameObject("Tooltips").AddComponent<RectTransform>();
            tooltipParent.transform.SetParent(transform, false);
            tooltipParent.transform.SetAsLastSibling();
            tooltipParent.gameObject.layer = LayerMask.NameToLayer("UI");

            var provider = FindObjectOfType<RelicInfoUI>(true);
            if (provider != null)
            {
                tooltipProvider = Instantiate(provider, tooltipParent.transform);
                tooltipProvider.name = "Tooltip Provider";
                tooltipProvider.gameObject.SetActive(false);
                tooltipProvider.SetStatusEffectManager(AllGameManagers.Instance.GetStatusEffectManager());
                tooltipProvider.SetTooltipSide(TooltipSide.Right);
            }

            var container = FindObjectOfType<UIElementTooltipContainer>(true);
            if (container != null)
            {
                tooltipContainer = Instantiate(container, tooltipParent.transform);
                tooltipContainer.name = "Tooltip Container";
            }
        }

        private void SetupResetButton(UIFooter uiFooter)
        {
            Button resetButton = Instantiate(closeButton, transform);
            resetButton.name = "Reset Button";
            resetButton.transform.localPosition = new Vector2(0, -460);

            DestroyImmediate(resetButton.targetGraphic.gameObject);
            DestroyImmediate(resetButton.transform.Find("Image close icon").gameObject);

            var starterLabel = uiFooter.transform.Find("Swap Champion Button/Label").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI resetLabel = Instantiate(starterLabel, resetButton.transform);
            DestroyImmediate(resetLabel.GetComponent<Localize>());

            resetLabel.name = "Label";
            resetLabel.fontSizeMin = resetLabel.fontSizeMax;
            resetLabel.fontSize = resetLabel.fontSizeMax;
            resetLabel.text = "Reset Current Page";

            resetButton.targetGraphic = Instantiate(
                uiFooter.transform.Find("Swap Champion Button/Target Graphic").GetComponent<Image>(),
                resetButton.transform
            );
            resetButton.targetGraphic.name = "Target Graphic";
            resetButton.targetGraphic.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 112);
            resetButton.targetGraphic.transform.SetAsFirstSibling();

            resetButton.onClick = new Button.ButtonClickedEvent();
            resetButton.onClick.AddListener(() =>
            {
                SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                foreach (var button in relicButtons)
                {
                    button.ToggleChosenState(false);
                }
                ResetOrder();
            });

            RectTransform resetButtonHitboxTr = resetButton.transform.Find("Hitbox Invis").GetComponent<RectTransform>();
            resetButtonHitboxTr.localRotation = Quaternion.identity;
            resetButtonHitboxTr.sizeDelta = new Vector2(280, 0);

            var resetAll = Instantiate(resetButton, transform);
            resetAll.name = "Reset All Button";
            resetAll.transform.localPosition = new Vector2(-500, -460);
            resetAll.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = "Reset All";

            resetAll.onClick = new Button.ButtonClickedEvent();
            resetAll.onClick.AddListener(() =>
            {
                SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                foreach (var entry in Plugin.Entries)
                {
                    entry.Value.Value = false;
                }
                foreach (var button in relicButtons)
                {
                    button.ToggleChosenState(false);
                }
                ResetOrder();
            });

            var runSetupScreen = (RunSetupScreen)AllGameManagers.Instance.GetScreenManager().GetScreen(ScreenName.RunSetup);
            var mutatorSelectionDialog = (MutatorSelectionDialog)AccessTools.Field(typeof(RunSetupScreen), "mutatorSelectionDialog")
                                                                            .GetValue(runSetupScreen);
            var clonedDialog = Instantiate(mutatorSelectionDialog, mutatorSelectionDialog.transform.parent.parent);
            var modOptionDialog = clonedDialog.gameObject.AddComponent<ModOptionDialog>();
            modOptionDialog.relicSelectionDialog = this;
            modOptionDialog.Setup();

            var advanced = Instantiate(resetButton, transform);
            advanced.name = "Advanced Button";
            advanced.transform.localPosition = new Vector2(500, -460);
            advanced.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = "Options";

            advanced.onClick = new Button.ButtonClickedEvent();
            advanced.onClick.AddListener(() =>
            {
                SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                bg.color = new Color(0, 0, 0, 0.502f);
                modOptionDialog.Open();

                factionDropdown.Close();
                rarityDropdown.Close();
                customDropdown.Close();
            });
        }

        private void SetupOptions()
        {
            var orderedClassTerms = Plugin.ClanNameTranslationTerm.Keys.Cast<string>().ToList();
            var orderedClass = Plugin.ClanNameTranslationTerm.Values.Cast<string>().ToList();

            allClanName = LocalizationManager.GetTranslation(Plugin.RelicRarityTranslationTerm.Keys.Cast<string>().First());
            FactionNames.Add(allClanName);
            RelicFactionOptions[allClanName] = null;

            var clanlessName = LocalizationManager.GetTranslation(orderedClassTerms.First());
            RelicFactionOptions[clanlessName] = null;
            FactionNames.Add(clanlessName);

            var allClassDatas = Plugin.GetAllGameData().GetAllClassDatas();
            var orderedVanillaFactions = allClassDatas.Where(data => orderedClass.Contains(data.Cheat_GetNameEnglish()))
                                                      .OrderBy(data => orderedClass.IndexOf(data.Cheat_GetNameEnglish()));

            var orderedCustomFactions = allClassDatas.Where(data => !orderedClass.Contains(data.Cheat_GetNameEnglish()))
                                                     .OrderBy(data => data.GetTitle());

            foreach (var classData in orderedVanillaFactions.Union(orderedCustomFactions))
            {
                var title = classData.GetTitle();
                FactionNames.Add(title);
                RelicFactionOptions[title] = classData;
            }

            foreach (DictionaryEntry relicRarity in Plugin.RelicRarityTranslationTerm)
            {
                if (!LocalizationManager.TryGetTranslation((string)relicRarity.Key, out string translatedRelicRarity))
                {
                    translatedRelicRarity = (string)relicRarity.Value;
                }
                switch (relicRarity.Value)
                {
                    case "All":
                        RelicRarityOptions[translatedRelicRarity] = (CollectableRarity)(-9999);
                        break;
                    case "Champion":
                        RelicRarityOptions[translatedRelicRarity] = CollectableRarity.Champion;
                        break;
                    case "Common":
                        RelicRarityOptions[translatedRelicRarity] = CollectableRarity.Common;
                        break;
                    case "Uncommon":
                        RelicRarityOptions[translatedRelicRarity] = CollectableRarity.Uncommon;
                        break;
                    case "Rare":
                        RelicRarityOptions[translatedRelicRarity] = CollectableRarity.Rare;
                        break;
                }
            }

            SetupCustomOptions();
        }

        internal void SetupCustomOptions()
        {
            CustomNames.Add("None");
            RelicFactionOptions["None"] = null;

            if (Plugin.ShowEnemyRelics.Value)
            {
                var title = "Enemy";
                CustomNames.Add(title);
                CustomOptions[title] = null;
            }

            if (Plugin.ShowCovenantRelics.Value)
            {
                var title = "Covenant";
                CustomNames.Add(title);
                CustomOptions[title] = null;
            }

            if (Plugin.ShowMutatorRelics.Value)
            {
                var title = "Mutator";
                CustomNames.Add(title);
                CustomOptions[title] = null;
            }


            if (Plugin.ShowPyreArtifactRelics.Value)
            {
                var title = "Pyre Heart";
                CustomNames.Add(title);
                CustomOptions[title] = null;
            }

            if (Plugin.ShowEndlessMutatorRelics.Value)
            {
                var title = "Endless Mutator";
                CustomNames.Add(title);
                CustomOptions[title] = null;
            }

            if (Plugin.ShowEnhancerRelics.Value)
            {
                var title = "Enhancer";
                CustomNames.Add(title);
                CustomOptions[title] = null;
            }
        }

        internal void ResetMenu()
        {
            Destroy(customDropdown.gameObject);
            CustomNames.Clear();
            CustomOptions.Clear();
            SetupCustomOptions();
            StartCoroutine(CreateCustomDropdown());
            LoadRelics(selectedRarity, selectedFaction);
        }

        GameUISelectableDropdown CreateDropdownWithOptions(Transform parent,
                                                           OrderedDictionary translatedTerms = null,
                                                           HashSet<string> customOptions = null)
        {
            var dropdown = Instantiate(dropdownPrefab, parent);
            using (GenericPools.GetList(out List<string> translatedOptions))
            {
                if (translatedTerms != null)
                {
                    foreach (DictionaryEntry option in translatedTerms)
                    {
                        string translatedName;
                        if (!LocalizationManager.TryGetTranslation((string)option.Key, out translatedName))
                        {
                            translatedName = (string)option.Value;
                        }
                        translatedOptions.Add(translatedName);
                    }
                }
                if (customOptions != null)
                {
                    translatedOptions.AddRange(customOptions);
                }
                translatedOptions.RemoveDuplicates();
                dropdown.SetOptions(translatedOptions);
            }
            dropdown.onClick.AddListener(delegate ()
            {
                InputManager.Inst.TryGetSignaledInputMapping(InputManager.Controls.Submit, out CoreInputControlMapping mapping);
                dropdown.ApplyScreenInput(mapping, dropdown, InputManager.Controls.Submit);
            });
            return dropdown;
        }

        void SetupDropdownButtonListeners(GameUISelectableDropdown targetDropdown)
        {
            foreach (var button in targetDropdown.GetComponentsInChildren<GameUISelectableButton>(true))
            {
                if (button == targetDropdown)
                {
                    continue;
                }
                button.onClick.AddListener(delegate ()
                {
                    InputManager.Inst.TryGetSignaledInputMapping(InputManager.Controls.Submit, out CoreInputControlMapping mapping);
                    targetDropdown.ApplyScreenInput(mapping, button, InputManager.Controls.Submit);
                });
            }
        }

        IEnumerator CreateDropdownList()
        {
            foreach (Transform transform in preview)
            {
                Destroy(transform.gameObject);
            }
            DestroyImmediate(preview.GetComponent<HorizontalLayoutGroup>());
            preview.gameObject.AddComponent<VerticalLayoutGroup>();
            if (dropdownPrefab == null)
            {
                dropdownPrefab = FindObjectOfType<GameUISelectableDropdown>(true);
            }

            factionDropdown = CreateDropdownWithOptions(preview, customOptions: FactionNames);
            rarityDropdown = CreateDropdownWithOptions(preview, Plugin.RelicRarityTranslationTerm);

            // Honestly there should be a better way of doing this than having to write it manually
            // But I'm not gonna bother spending more time on it as there will only be like 3 dropdown anyways
            factionDropdown.onClick.AddListener(() =>
            {
                rarityDropdown.Close();
                customDropdown.Close();
            });

            rarityDropdown.onClick.AddListener(() =>
            {
                factionDropdown.Close();
                customDropdown.Close();
            });

            yield return null;
            SetupDropdownButtonListeners(factionDropdown);
            SetupDropdownButtonListeners(rarityDropdown);

            factionDropdown.optionChosenSignal.AddListener(delegate (int index, string optionName)
            {
                if (selectedFactionString == optionName)
                {
                    return;
                }
                selectedFactionString = optionName;
                selectedFaction = (ClassData)RelicFactionOptions[optionName];
                LoadRelics(selectedRarity, selectedFaction, selectedCustom);
            });

            rarityDropdown.optionChosenSignal.AddListener(delegate (int index, string optionName)
            {
                if (selectedRarity == (CollectableRarity)RelicRarityOptions[optionName])
                {
                    return;
                }
                selectedRarity = (CollectableRarity)RelicRarityOptions[optionName];
                LoadRelics(selectedRarity, selectedFaction, selectedCustom);
            });

            factionDropdown.SetIndex(1);
            rarityDropdown.SetIndex(0);
            selectedFaction = (ClassData)RelicFactionOptions[1];
            selectedRarity = (CollectableRarity)RelicRarityOptions[0];

            LoadRelics(selectedRarity, selectedFaction);
            preview.gameObject.SetActive(true);
        }

        IEnumerator CreateCustomDropdown()
        {
            if (dropdownPrefab == null)
            {
                dropdownPrefab = FindObjectOfType<GameUISelectableDropdown>(true);
            }
            customDropdown = CreateDropdownWithOptions(preview, customOptions: CustomNames);

            customDropdown.onClick.AddListener(() =>
            {
                factionDropdown.Close();
                rarityDropdown.Close();
            });

            yield return null;
            SetupDropdownButtonListeners(customDropdown);
            customDropdown.optionChosenSignal.AddListener(delegate (int index, string optionName)
            {
                if (selectedCustom == optionName)
                {
                    return;
                }
                selectedCustom = optionName;
                factionDropdown.interactable = optionName == "None";
                LoadRelics(selectedRarity, selectedFaction, selectedCustom);
            });
            customDropdown.SetIndex(0);
            selectedCustom = "None";

        }
        private void InitializeComponents()
        {
            Instance = this;
            gameObject.name = nameof(RelicSelectionDialog);
            dialog = GetComponentInChildren<ScreenDialog>();
            scrollRect = GetComponentInChildren<ScrollRect>();
            preview = dialog.transform.Find("Content/Info and Preview/Mutators preview");

            Transform originalCloseButton = transform.Find("Dialog/CloseButton");
            DestroyImmediate(originalCloseButton.GetComponent<GameUISelectableButton>());
        }

        private void SetupDialogText()
        {
            Transform instructions = dialog.transform.Find("Content/Info and Preview/Instructions");

            title = instructions.Find("Instructions label").GetComponent<TextMeshProUGUI>();
            DestroyImmediate(title.GetComponent<Localize>());
            title.text = "Choose any relics to customize your run.";

            warning = instructions.Find("Warning layout/Warning label").GetComponent<TextMeshProUGUI>();
            DestroyImmediate(warning.GetComponent<Localize>());
            warning.text = "Not every relic here has been tested, enabled relic will be sorted at the top. Re-open the menu to sort it";
        }

        private void CreateTemplateButton()
        {
            layout = scrollRect.content.GetChild(0).gameObject.AddComponent<RelicButtonUI>();

            var sampleRelic = Plugin.GetAllRelicDatas().FirstOrDefault(relic =>
                !string.IsNullOrWhiteSpace(relic.Cheat_GetNameEnglish()));

            layout.Set(sampleRelic);
            layout.gameObject.SetActive(false);
        }

        private void SetupCloseButton()
        {
            closeButton = transform.Find("Dialog/CloseButton").gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeButton.transform.Find("Target Graphic").GetComponent<Image>();
            closeButton.gameObject.SetActive(true);

            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(() =>
            {
                SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                Close();
            });
        }

        private void GenerateRelicEntry()
        {
            foreach (RelicData relic in Plugin.GetAllRelicDatas()
                .OrderBy(relic => relic.Cheat_GetNameEnglish()))
            {
                if (string.IsNullOrWhiteSpace(relic.Cheat_GetNameEnglish()))
                {
                    continue;
                }
                if (!Plugin.Entries.TryGetValue(relic, out var entry))
                {
                    CreateRelicConfigEntry(relic);
                }
            }
        }
        private void LoadRelics(CollectableRarity rarity, ClassData faction, string selectedCustomOption = "")
        {
            IEnumerable<RelicData> relics = Plugin.GetAllRelicDatas();

            if (selectedCustomOption == "Enemy" &&
                Plugin.ShowEnemyRelics.Value)
            {
                relics = relics.Where(relicData => relicData is SinsData);
            }
            else if (selectedCustomOption == "Covenant" &&
                     Plugin.ShowCovenantRelics.Value)
            {
                relics = relics.Where(relicData => relicData is CovenantData);
            }
            else if (selectedCustomOption == "Mutator" &&
                     Plugin.ShowMutatorRelics.Value)
            {
                relics = relics.Where(relicData => relicData is MutatorData);
            }
            else if (selectedCustomOption == "Pyre Heart" &&
                     Plugin.ShowPyreArtifactRelics.Value)
            {
                relics = relics.Where(relicData => relicData is PyreArtifactData);
            }
            else if (selectedCustomOption == "Endless Mutator" &&
                     Plugin.ShowEndlessMutatorRelics.Value)
            {
                relics = relics.Where(relicData => relicData is EndlessMutatorData);
            }
            else if (selectedCustomOption == "Enhancer" &&
                     Plugin.ShowEnhancerRelics.Value)
            {
                relics = relics.Where(relicData => relicData is EnhancerData);
            }
            else
            {
                relics = relics.Where(delegate (RelicData relicData)
                {
                    if (relicData is CollectableRelicData collectableRelicData)
                    {
                        return selectedFactionString == allClanName ? true : collectableRelicData.GetLinkedClass() == faction;
                    }
                    return false;
                });
            }

            // -9999 is value for All
            if ((int)rarity != -9999)
            {
                relics = relics.Where(delegate (RelicData relicData)
                {
                    if (relicData is CollectableRelicData collectableRelicData)
                    {
                        return collectableRelicData.GetRarity() == rarity;
                    }
                    return false;
                });
            }
            CreateRelicButton(relics);
        }

        private void CreateRelicConfigEntry(RelicData relic)
        {
            var definition = new ConfigDefinition("Relic Effects", relic.name);
            Plugin.Entries[relic] = Plugin.Config.Bind<bool>(
                definition,
                false,
                new ConfigDescription(
                    relic.GetDescription(),
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }
                )
            );
        }

        private void CreateRelicButton(IEnumerable<RelicData> relics)
        {
            foreach (var relic in relicButtons)
            {
                DestroyImmediate(relic.gameObject);
            }
            relicButtons.Clear();

            foreach (var relic in relics)
            {
                if (!Plugin.Entries.TryGetValue(relic, out _))
                {
                    CreateRelicConfigEntry(relic);
                }
                try
                {
                    var relicButton = Instantiate(layout, layout.transform.parent);
                    relicButton.Set(relic);
                    relicButton.gameObject.SetActive(true);
                    relicButtons.Add(relicButton);
                }
                catch (Exception ex)
                {
                    Plugin.LogSource.LogError((ex.InnerException ?? ex).Message);
                }
            }

            ResetOrder();
        }


        void LateUpdate()
        {
            UpdateTooltipPosition();
        }

        void UpdateTooltipPosition()
        {
            if (focusedRelicButton == null)
            {
                return;
            }
            var offset = new Vector3(-120, -260);
            tooltipParent.transform.position = Input.mousePosition + offset;
            tooltipContainer.ForceUpdateLayoutAndPosition();
        }

        internal void SetHoveredRelic(RelicButtonUI relicButton)
        {
            if (relicButton == null || !Plugin.ShowTooltips.Value)
            {
                focusedRelicButton = null;
                if (tooltipContainer != null)
                {
                    tooltipContainer.DisableTooltips();
                }
                return;
            }
            if (tooltipContainer == null ||
                tooltipProvider == null)
            {
                return;
            }
            focusedRelicButton = relicButton;
            tooltipProvider.Set(focusedRelicButton.Data);
            tooltipContainer.ShowTooltipsForProvider(tooltipProvider, UIElementTooltipManager.ForcedDuration.Persistent);
            tooltipProvider.gameObject.SetActive(false);
            UpdateTooltipPosition();
        }
    }
}
