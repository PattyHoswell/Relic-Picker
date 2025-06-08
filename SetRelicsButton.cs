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
            Destroy(content.transform.Find("Header").gameObject);
            Destroy(content.transform.Find("Ability Health layout").gameObject);
            var pyreHeartName = content.transform.Find("Pyre heart name");
            pyreHeartName.localPosition = new Vector3(24, 0, 0);
            title = pyreHeartName.Find("Pyre heart name label").GetComponent<TextMeshProUGUI>();
            title.text = "Set Starter Relics";

            transform.SetSiblingIndex(1);

            var portrait = content.transform.Find("Frame Portrait")
                                            .Find("Pyre Heart icon")
                                            .GetComponent<Image>();
            CollectableRelicData heavensLight = AllGameManagers.Instance.GetAllGameData()
                                                                        .FindCollectableRelicDataByName("BossCapacity2");
            portrait.sprite = heavensLight.GetIcon();

            var button = content.transform.Find("Pyre Heart Button")
                                          .GetComponent<GameUISelectableButton>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(delegate()
            {
                SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                if (RelicSelectionDialog.Instance == null)
                {
                    /* It shouldn't have reached this.
                     * But in case it did.
                     * Do the long way to get the relic dialog.
                     * 
                     * Don't get RelicSelectionDialog directly from FindObjectOfType because it will return null for some reason.
                     */
                    FindObjectOfType<RunSetupScreen>().transform.Find(nameof(RelicSelectionDialog)).GetComponent<RelicSelectionDialog>().Open();
                }
                else
                {
                    RelicSelectionDialog.Instance.Open();
                }
            });
        }
    }
}
