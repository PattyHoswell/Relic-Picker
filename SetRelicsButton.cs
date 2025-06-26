using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Patty_RelicPicker_MOD
{
    internal class SetRelicsButton : MonoBehaviour
    {
        GameUISelectableWithNavigation content;
        TextMeshProUGUI title;

        private void Start()
        {
            content = transform.Find("Content").GetComponent<GameUISelectableWithNavigation>();

            RemoveUnneededUIElements();
            SetupTitleDisplay();
            transform.SetSiblingIndex(1);
            SetupRelicPortrait();
            SetupSelectionButton();
        }

        private void RemoveUnneededUIElements()
        {
            Destroy(content.transform.Find("Header").gameObject);
            Destroy(content.transform.Find("Ability Health layout").gameObject);
        }

        private void SetupTitleDisplay()
        {
            Transform titleContainer = content.transform.Find("Pyre heart name");
            titleContainer.localPosition = new Vector3(24, 0, 0);
            title = titleContainer.Find("Pyre heart name label").GetComponent<TextMeshProUGUI>();
            title.text = "Set Starter Relics";
        }

        private void SetupRelicPortrait()
        {
            Image portraitImage = content.transform.Find("Frame Portrait/Pyre Heart icon")
                                                   .GetComponent<Image>();
            CollectableRelicData heavensLightRelic = AllGameManagers.Instance
                                                                    .GetAllGameData()
                                                                    .FindCollectableRelicDataByName("BossCapacity2");
            portraitImage.sprite = heavensLightRelic.GetIcon();
        }

        private void SetupSelectionButton()
        {
            GameUISelectableButton selectionButton = content.transform.Find("Pyre Heart Button")
                                                                     .GetComponent<GameUISelectableButton>();
            selectionButton.onClick = new Button.ButtonClickedEvent();
            selectionButton.onClick.AddListener(OnSelectionButtonClicked);
        }

        private void OnSelectionButtonClicked()
        {
            SoundManager.PlaySfxSignal.Dispatch("UI_Click");
            if (RelicSelectionDialog.Instance == null)
            {
                RelicSelectionDialog.Instance = FindObjectOfType<RunSetupScreen>()
                                               .transform.Find(nameof(RelicSelectionDialog))
                                               .GetComponent<RelicSelectionDialog>();
            }
            RelicSelectionDialog.Instance.enabled = true;
            RelicSelectionDialog.Instance.gameObject.SetActive(true);
        }
    }
}
