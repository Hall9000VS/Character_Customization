using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies facial presets and skin overrides to a character at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterCustomizationController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int SkinDarknessId = Shader.PropertyToID("_SkinDarkness");

    [SerializeField] private SkinnedMeshRenderer faceMesh;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private List<FacialPreset> presets = new List<FacialPreset>();
    [SerializeField] private float transitionDuration = 0.3f;

    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
    private Color currentSkinTint = Color.white;
    private float currentSkinDarkness = 0.5f;

    /// <summary>
    /// Gets the presets currently assigned to this controller.
    /// </summary>
    public IReadOnlyList<FacialPreset> AvailablePresets => presets;

    /// <summary>
    /// Gets the most recently requested preset.
    /// </summary>
    public FacialPreset CurrentPreset { get; private set; }

    private void Start()
    {
        if (presets.Count > 0 && presets[0] != null)
        {
            ApplyPresetInstant(presets[0]);
            return;
        }

        ApplySkinProperties();
    }

    /// <summary>
    /// Applies the preset at the given list index.
    /// </summary>
    /// <param name="index">The preset index in <see cref="AvailablePresets"/>.</param>
    public void ApplyPreset(int index)
    {
        if (index < 0 || index >= presets.Count)
        {
            return;
        }

        ApplyPreset(presets[index]);
    }

    /// <summary>
    /// Applies the given preset using the configured transition duration.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    public void ApplyPreset(FacialPreset preset)
    {
        if (preset == null)
        {
            return;
        }

        ApplyPresetInstant(preset);
    }

    /// <summary>
    /// Sets the current skin darkness override.
    /// </summary>
    /// <param name="value01">A normalized darkness value in the 0..1 range.</param>
    public void SetSkinDarkness(float value01)
    {
        currentSkinDarkness = Mathf.Clamp01(value01);
        ApplySkinProperties();
    }

    /// <summary>
    /// Sets the current skin tint override.
    /// </summary>
    /// <param name="color">The tint color to apply through a material property block.</param>
    public void SetSkinTint(Color color)
    {
        currentSkinTint = color;
        ApplySkinProperties();
    }

    [ContextMenu("Apply First Preset")]
    private void ApplyFirstPresetFromContext()
    {
        ApplyPreset(0);
    }

    [ContextMenu("Apply Second Preset")]
    private void ApplySecondPresetFromContext()
    {
        ApplyPreset(1);
    }

    [ContextMenu("Apply Third Preset")]
    private void ApplyThirdPresetFromContext()
    {
        ApplyPreset(2);
    }

    [ContextMenu("Preview Darker Skin")]
    private void PreviewDarkerSkinFromContext()
    {
        SetSkinDarkness(0.8f);
    }

    private void ApplyPresetInstant(FacialPreset preset)
    {
        CurrentPreset = preset;
        ApplyBlendshapes(preset);
        currentSkinTint = GetPresetTint(preset);
        currentSkinDarkness = GetPresetDarkness(preset);
        ApplySkinProperties();
    }

    private void ApplyBlendshapes(FacialPreset preset)
    {
        var mesh = faceMesh != null ? faceMesh.sharedMesh : null;
        if (mesh == null || preset == null || preset.blendshapes == null)
        {
            return;
        }

        for (var i = 0; i < preset.blendshapes.Count; i++)
        {
            var entry = preset.blendshapes[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.blendshapeName))
            {
                continue;
            }

            var index = mesh.GetBlendShapeIndex(entry.blendshapeName);
            if (index < 0)
            {
                continue;
            }

            faceMesh.SetBlendShapeWeight(index, entry.weight);
        }
    }

    private void ApplySkinProperties()
    {
        if (bodyRenderer == null)
        {
            return;
        }

        bodyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, currentSkinTint);
        propertyBlock.SetFloat(SkinDarknessId, currentSkinDarkness);
        bodyRenderer.SetPropertyBlock(propertyBlock);
    }

    private static Color GetPresetTint(FacialPreset preset)
    {
        return preset != null && preset.skin != null ? preset.skin.tint : Color.white;
    }

    private static float GetPresetDarkness(FacialPreset preset)
    {
        return preset != null && preset.skin != null ? preset.skin.darkness : 0.5f;
    }
}
