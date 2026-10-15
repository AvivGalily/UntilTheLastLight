# Research laboratory surfaces

The research equipment uses worn brushed steel, chipped sage enamel and rubber materials with separate tangent normals, metallic values, ambient occlusion and variable smoothness. Box-shaped instruments have rounded edges and metre-based UVs; hose UVs follow their arc length. Glass and water use transparent optical-film textures, with local highlights attenuated by transparency so the specimen remains visible.

The lab also includes printed caution/sample labels, an etched monitor face, wet reagent surfaces, paper grain on the anatomical poster and mottled specimen scales. Existing collision and navigation boundaries are retained.

The three generated colour sources are preserved unchanged in `Assets/_Game/Art/Textures/Lab{Steel,Enamel,Rubber}_BaseColor.png`. Built-in imagegen mode, exact prompts, actual dimensions and SHA-256 hashes are recorded in [ImagegenPrompts.json](../ArtSource/Research/Materials/ImagegenPrompts.json). The other maps and equipment markings are deterministic project-authored assets. [Manifest.json](../ArtSource/Research/Materials/Manifest.json) lists the maps and channel packing.

To rebuild the technical maps and reinstall the lab without regenerating the forest or equipment grips:

```powershell
python Tools/Assets/prepare_laboratory_surfaces.py
.\Tools\Invoke-Unity.ps1 -Task ResearchSurfaces
.\Tools\Invoke-Unity.ps1 -Task ResearchCapture
.\Tools\Invoke-Unity.ps1 -Task ResearchSmoke
```

The full Blender preparation pipeline also rebuilds these maps. Retained colour sources are required; running preparation never calls an image service or replaces those sources.

Unity URP captures are saved in `Docs/Screenshots/100-research-laboratory.png` through `103-broken-laboratory-glass.png`, with instrument and sample close-ups in `109-research-instrument-textures.png` and `110-research-sample-textures.png`.

At the material-only revision, project validation, the research gameplay smoke check, 110 EditMode tests and 17 PlayMode tests passed. The Windows player built successfully and remained open for a twelve-second normal startup check. Its separate display automation was incomplete. That historical evidence is in `Logs/ResearchSurfaceVerification.json`. The subsequent [R-09 release encounter](ResearchEncounter.md) adds rigged specimens, combat and the cabin key lock with its own updated validation.
