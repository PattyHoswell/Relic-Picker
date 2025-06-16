using BepInEx;
using DG.Tweening.Core.Easing;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx.Configuration;
using TMPro;
using I2.Loc;

namespace Patty_RelicPicker_MOD
{
    internal class RelicSelectionDialog : MonoBehaviour
    {
        ScreenDialog dialog;
        ScrollRect scrollRect;
        RelicButtonUI layout;
        Button closeButton;
        List<RelicButtonUI> relicButtons = new List<RelicButtonUI>();
        TextMeshProUGUI title, warning;
        internal static RelicSelectionDialog Instance {  get; set; }
        bool initialized;
        
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

            // Order by enabled, then by name
            relicButtons = relicButtons.OrderByDescending(relicButton => (bool)Plugin.Entries[relicButton.RelicData].BoxedValue)
                                       .ThenBy(relicButton => relicButton.RelicData.Cheat_GetNameEnglish()).ToList();
            for (int i = 0; i < relicButtons.Count; i++)
            {
                RelicButtonUI relicButton = relicButtons[i];
                relicButton.ToggleChosenState((bool)Plugin.Entries[relicButton.RelicData].BoxedValue);
                relicButton.transform.SetSiblingIndex(i);
            }

            // Sets the menu on top
            transform.SetAsLastSibling();
            dialog.SetActive(true, gameObject);
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

            InitializeComponents();
            SetupDialogText();
            CreateTemplateButton();
            SetupCloseButton();
            GenerateRelicButtons();
        }

        private void InitializeComponents()
        {
            Instance = this;
            gameObject.name = nameof(RelicSelectionDialog);
            dialog = GetComponentInChildren<ScreenDialog>();
            scrollRect = GetComponentInChildren<ScrollRect>();

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

        private void GenerateRelicButtons()
        {
            foreach (CollectableRelicData relic in Plugin.GetAllRelicDatas()
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

                CreateRelicButton(relic);
            }
        }

        private void CreateRelicConfigEntry(CollectableRelicData relic)
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

        private void CreateRelicButton(CollectableRelicData relic)
        {
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
    }
}
