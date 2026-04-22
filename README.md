# Facial Customization Demo
Data-driven facial customization foundation for Unity.

## Demo
[YouTube link — coming]

<!-- GIF: docs/preview.gif -->

## Architecture
- ScriptableObject presets keep facial blendshape targets and skin parameters in assets instead of hardcoded branches.
- MaterialPropertyBlock skin variation updates renderer properties without instancing materials at runtime.
- A small controller API exposes preset switching and skin controls behind a single integration point.

## Setup
1. Open the project in Unity 2022.3 LTS with URP.
2. Download the free "1 toon teen" asset by JBGarraza from the Unity Asset Store.
3. Import the character into the project. The asset folder is ignored from git and should not be committed.
4. Add the character to your demo scene and assign the face mesh and body renderer on `CharacterCustomizationController`.
5. Create 3 preset assets with `Create > Character > Facial Preset`.
6. Create your UI canvas with 3 preset buttons and 1 skin darkness slider, then wire it to `CustomizationUI`.
7. Press Play.

## Project Structure
```text
Assets/
  _Project/
    Scripts/
      Customization/
        FacialPreset.cs
        CharacterCustomizationController.cs
      UI/
        CustomizationUI.cs
    Presets/
    Materials/
    Scenes/
```

## Why This Matters
This architecture is asset-agnostic: presets store blendshape names as data, while the runtime controller only depends on a `SkinnedMeshRenderer` and a renderer for skin properties. That makes the same pattern portable to DAZ/Genesis 8, MetaHuman, or any future character that exposes compatible blendshapes.

## Tech
Unity 2022.3 LTS, URP, C#, zero runtime dependencies.
