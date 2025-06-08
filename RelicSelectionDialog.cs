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
            Instance = this;
            name = nameof(RelicSelectionDialog);
            dialog = GetComponentInChildren<ScreenDialog>();
            scrollRect = GetComponentInChildren<ScrollRect>();
            Transform ogCloseTr = transform.Find("Dialog").Find("CloseButton");
            DestroyImmediate(ogCloseTr.GetComponent<GameUISelectableButton>());

            var instructions = dialog.transform.Find("Content")
                                               .Find("Info and Preview")
                                               .Find("Instructions");

            title = instructions.Find("Instructions label")
                                .GetComponent<TextMeshProUGUI>();
            DestroyImmediate(title.GetComponent<Localize>());

            warning = instructions.Find("Warning layout")
                                  .Find("Warning label")
                                  .GetComponent<TextMeshProUGUI>();
            DestroyImmediate(warning.GetComponent<Localize>());

            title.text = "Choose any relics to customize your run.";
            warning.text = "Not every relic here has been tested, enabled relic will be sorted at the top. Re-open the menu to sort it";

            layout = scrollRect.content.GetChild(0).gameObject.AddComponent<RelicButtonUI>();
            layout.Set(AllGameManagers.Instance.GetAllGameData().GetAllCollectableRelicData()
                                                                .FirstOrDefault(relic => 
                                                                                !string.IsNullOrWhiteSpace(relic.name)));
            layout.gameObject.SetActive(false);

            closeButton = ogCloseTr.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeButton.transform.Find("Target Graphic").GetComponent<Image>();
            closeButton.gameObject.SetActive(true);
            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(delegate()
            {
                SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                Close();
            });

            foreach (CollectableRelicData relic in AllGameManagers.Instance.GetAllGameData()
                                                                           .GetAllCollectableRelicData()
                                                                           .OrderBy(relic => relic.Cheat_GetNameEnglish()))
            {
                if (string.IsNullOrWhiteSpace(relic.Cheat_GetNameEnglish()))
                {
                    continue;
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
        }
    }
}
