using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BepInEx.Configuration;

namespace Patty_RelicPicker_MOD
{
    internal class RelicButtonUI : MonoBehaviour
    {
        public GameUISelectableButton Button { get; private set; }
        public bool Chosen { get; private set; }
        public RelicData Data {  get; private set; }
        private Image icon;
        private TextMeshProUGUI titleLabel, descriptionLabel;
        private ColorSwapper selectedColorSwapper;
        private bool initialized;
        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Button = GetComponent<GameUISelectableButton>();
            icon = transform.Find("Icon").GetComponent<Image>();
            titleLabel = transform.Find("Name label").GetComponent<TextMeshProUGUI>();
            descriptionLabel = transform.Find("Description label").GetComponent<TextMeshProUGUI>();
            selectedColorSwapper = transform.Find("Target Graphic").GetComponent<ColorSwapper>();
            Button.onClick = new Button.ButtonClickedEvent();
            Button.onClick.AddListener(delegate ()
            {
                ToggleChosenState(!Chosen);
                if (Chosen)
                {
                    SoundManager.PlaySfxSignal.Dispatch("UI_Click");
                }
                else
                {
                    SoundManager.PlaySfxSignal.Dispatch("UI_Cancel");
                }
            });
        }
        public void Clear()
        {
            gameObject.SetActive(false);
            Data = null;
        }
        public void Set(RelicData data)
        {
            Setup();
            Data = data;
            icon.sprite = data.GetIcon();
            titleLabel.SetTextSafe(data.GetName(), true);
            descriptionLabel.SetTextSafe(data.GetDescription(), true);
            ToggleChosenState(Plugin.Entries[Data].Value);
        }
        public void ToggleChosenState(bool chosen)
        {
            Chosen = chosen;
            Plugin.Entries[Data].Value = Chosen;
            // Swap the chosen palette, so enabled will be highlighted
            var num = (chosen ? 1f : 0.5f);
            titleLabel.color = titleLabel.color.SetAlpha(num);
            descriptionLabel.color = descriptionLabel.color.SetAlpha(num);
            if (selectedColorSwapper != null)
            {
                selectedColorSwapper.SetAlternativeColor(!chosen, 0f, Ease.Unset, 0f);
            }
        }
    }
}
