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
    private const int SkinMaterialIndex = 0;

    [SerializeField] private SkinnedMeshRenderer faceMesh;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private List<FacialPreset> presets = new List<FacialPreset>();
    [SerializeField] private float transitionDuration = 0.3f;

    private MaterialPropertyBlock propertyBlock;
    private Coroutine activeTransition;
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

    private void Awake()
    {
        EnsurePropertyBlock();
    }

    private void Start()
    {
        if (presets.Count > 0 && presets[0] != null)
        {
            ApplyPresetInstant(presets[0]);
            return;
        }

        ApplySkinProperties();
    }

    private void OnDisable()
    {
        CancelActiveTransition();
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

        CancelActiveTransition();
        CurrentPreset = preset;

        if (!isActiveAndEnabled || transitionDuration <= 0f)
        {
            ApplyPresetInstant(preset);
            return;
        }

        activeTransition = StartCoroutine(TransitionToPreset(preset));
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
        PreviewPreset(0);
    }

    [ContextMenu("Apply Second Preset")]
    private void ApplySecondPresetFromContext()
    {
        PreviewPreset(1);
    }

    [ContextMenu("Apply Third Preset")]
    private void ApplyThirdPresetFromContext()
    {
        PreviewPreset(2);
    }

    [ContextMenu("Preview Darker Skin")]
    private void PreviewDarkerSkinFromContext()
    {
        SetSkinDarkness(0.8f);
    }

    private void ApplyPresetInstant(FacialPreset preset)
    {
        CancelActiveTransition();
        CurrentPreset = preset;
        ApplyBlendshapes(preset);
        currentSkinTint = GetPresetTint(preset);
        currentSkinDarkness = GetPresetDarkness(preset);
        ApplySkinProperties();
    }

    private IEnumerator TransitionToPreset(FacialPreset preset)
    {
        var targets = CaptureTargets(preset);
        var startTint = currentSkinTint;
        var startDarkness = currentSkinDarkness;
        var targetTint = GetPresetTint(preset);
        var targetDarkness = GetPresetDarkness(preset);
        var elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / transitionDuration);
            ApplyBlendshapeTargets(targets, t);
            currentSkinTint = Color.Lerp(startTint, targetTint, t);
            currentSkinDarkness = Mathf.Lerp(startDarkness, targetDarkness, t);
            ApplySkinProperties();
            yield return null;
        }

        ApplyBlendshapeTargets(targets, 1f);
        currentSkinTint = targetTint;
        currentSkinDarkness = targetDarkness;
        ApplySkinProperties();
        activeTransition = null;
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
        if (bodyRenderer == null && faceMesh == null)
        {
            return;
        }

        EnsurePropertyBlock();
        var effectiveColor = GetEffectiveSkinColor();
        ApplySkinProperties(bodyRenderer, SkinMaterialIndex, effectiveColor);

        if (!ReferenceEquals(faceMesh, bodyRenderer))
        {
            ApplySkinProperties(faceMesh, SkinMaterialIndex, effectiveColor);
        }
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void ApplySkinProperties(Renderer targetRenderer, int materialIndex, Color effectiveColor)
    {
        if (targetRenderer == null || materialIndex < 0 || materialIndex >= targetRenderer.sharedMaterials.Length)
        {
            return;
        }

        propertyBlock.Clear();
        targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
        propertyBlock.SetColor(BaseColorId, effectiveColor);
        propertyBlock.SetFloat(SkinDarknessId, currentSkinDarkness);
        targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
    }

    private Color GetEffectiveSkinColor()
    {
        var lightness = Mathf.Lerp(1f, 0.35f, currentSkinDarkness);
        var effectiveColor = currentSkinTint * lightness;
        effectiveColor.a = currentSkinTint.a;
        return effectiveColor;
    }

    private void PreviewPreset(int index)
    {
        if (index < 0 || index >= presets.Count || presets[index] == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            ApplyPreset(index);
            return;
        }

        ApplyPresetInstant(presets[index]);
    }

    private List<BlendshapeTarget> CaptureTargets(FacialPreset preset)
    {
        var targets = new List<BlendshapeTarget>();
        var mesh = faceMesh != null ? faceMesh.sharedMesh : null;
        if (mesh == null || preset == null || preset.blendshapes == null)
        {
            return targets;
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

            targets.Add(new BlendshapeTarget(index, faceMesh.GetBlendShapeWeight(index), entry.weight));
        }

        return targets;
    }

    private void ApplyBlendshapeTargets(List<BlendshapeTarget> targets, float t)
    {
        if (faceMesh == null)
        {
            return;
        }

        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            faceMesh.SetBlendShapeWeight(target.Index, Mathf.Lerp(target.From, target.To, t));
        }
    }

    private void CancelActiveTransition()
    {
        if (activeTransition == null)
        {
            return;
        }

        StopCoroutine(activeTransition);
        activeTransition = null;
    }

    private static Color GetPresetTint(FacialPreset preset)
    {
        return preset != null && preset.skin != null ? preset.skin.tint : Color.white;
    }

    private static float GetPresetDarkness(FacialPreset preset)
    {
        return preset != null && preset.skin != null ? preset.skin.darkness : 0.5f;
    }

    private readonly struct BlendshapeTarget
    {
        public BlendshapeTarget(int index, float from, float to)
        {
            Index = index;
            From = from;
            To = to;
        }

        public int Index { get; }
        public float From { get; }
        public float To { get; }
    }
}
