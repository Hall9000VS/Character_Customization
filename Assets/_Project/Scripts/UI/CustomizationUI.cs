using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CustomizationUI : MonoBehaviour
{
    [SerializeField] private CharacterCustomizationController controller;
    [SerializeField] private List<Button> presetButtons = new List<Button>();
    [SerializeField] private Slider skinDarknessSlider;

    private void Start()
    {
        if (controller == null)
        {
            return;
        }

        var presets = controller.AvailablePresets;
        for (var i = 0; i < presetButtons.Count; i++)
        {
            var button = presetButtons[i];
            if (button == null)
            {
                continue;
            }

            var isActive = i < presets.Count && presets[i] != null;
            button.gameObject.SetActive(isActive);
            button.onClick.RemoveAllListeners();
            if (!isActive)
            {
                continue;
            }

            var index = i;
            button.onClick.AddListener(() => controller.ApplyPreset(index));
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(presets[i].displayName) ? presets[i].name : presets[i].displayName;
            }
        }

        if (skinDarknessSlider == null)
        {
            return;
        }

        var initialPreset = controller.CurrentPreset != null ? controller.CurrentPreset : presets.Count > 0 ? presets[0] : null;
        skinDarknessSlider.SetValueWithoutNotify(initialPreset != null && initialPreset.skin != null ? initialPreset.skin.darkness : skinDarknessSlider.value);
        skinDarknessSlider.onValueChanged.RemoveAllListeners();
        skinDarknessSlider.onValueChanged.AddListener(controller.SetSkinDarkness);
    }
}
