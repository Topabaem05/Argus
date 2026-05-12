# MiniBot Mixamo Motion Imports

Place local Mixamo FBX files under:

```txt
Assets/Project/Resources/Animations/Mixamo/Raw/
```

That directory is intentionally ignored by Git. The implementation uses semantic motion slots and Unity-side importer/controller code so raw third-party animation files do not have to be redistributed from this repository.

The supported local clip set and runtime mapping are documented in:

- `docs/unity/MIXAMO_SMOOTH_MOTION_EVALUATION.md`
- `docs/unity/MIXAMO_MOTION_SYSTEM.md`
