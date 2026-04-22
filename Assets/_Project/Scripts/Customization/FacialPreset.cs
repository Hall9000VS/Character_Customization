using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Character/Facial Preset", fileName = "NewFacialPreset")]
public sealed class FacialPreset : ScriptableObject
{
    public string displayName;
    public List<BlendshapeEntry> blendshapes = new List<BlendshapeEntry>();
    public SkinParameters skin = new SkinParameters();
}

[Serializable]
public sealed class BlendshapeEntry
{
    public string blendshapeName;

    [Range(0f, 100f)]
    public float weight;
}

[Serializable]
public sealed class SkinParameters
{
    [ColorUsage(false)]
    public Color tint = Color.white;

    [Range(0f, 1f)]
    public float darkness = 0.5f;
}
