# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-02-13

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android/iOS)

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html


---

## 6.3 → 6.6

**Last verified:** 2026-11-12
**Sources:** https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64 · https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity65 · https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66 (Unity's "What's new" pages).
**Coverage limit:** NOT SOURCEABLE — a complete breaking-change list for 6.4–6.6 is not stated on
these pages; they defer to the "Upgrade to Unity 6.x" guides and release notes, which were not
read. Treat this as a floor, not the full list. The last 4,641 characters of the 6.5 page were
not read.

### 6.4
- **PVRTC removed** — cannot be targeted on texture import; use ASTC or ETC.
- **URP Compatibility Mode fully removed** for custom render passes; the `URP_COMPATIBILITY_MODE`
  define is gone. Use Render Graph.
- **Build Profiles platform list hidden by default** for new projects (disable **Hide Classic
  Platforms** in Project Settings > Editor > Build Profiles to show it).
- **`com.unity.modules.physics` can be disabled**; keep it enabled if you use local volumes.
- **New warning** when a class in a `[SerializeReference]` field (or an ancestor) lacks `[Serializable]`.
- **`InstanceID` → `EntityId`** begins (InstanceID methods marked deprecated). Do not cast
  `EntityId` to/from `int`, hash it, parse it from a string, or sort it for creation order.

### 6.5
- **`EntityId` replaces `InstanceID`** (64-bit struct): the obsolete integer APIs now fail to
  compile, including `Object.GetInstanceID`, `Resources.InstanceIDToObject`, `Selection.instanceIDs`.
- **Removed obsolete APIs:** `GameObject.rigidbody`, `GameObject.camera`, `Component.renderer`,
  `GameObject.AddComponent(string)` — use `GetComponent<T>` / `AddComponent<T>`.
- **2D:** `LowLevelPhysics2D` renamed `PhysicsCore2D`, namespace `UnityEngine.LowLevelPhysics2D` →
  `Unity.U2D.Physics`.
- **Android:** minimum API level is now 26; x86-64 removed; Gradle 9.1.0 / AGP 9.0.0 (custom Gradle
  templates and third-party plug-ins may need manual changes); `proguard-android.txt` unsupported
  (use `proguard-android-optimize.txt`); plug-ins must declare a unique namespace; **Hide Navigation
  Bar** replaced by Insets (`Screen.fullScreen` has no effect on Android; `unity.launch-fullscreen`
  manifest entry replaced by `unity.requested-visible-insets` / `unity.system-bars-behavior`).
- **Texture Read/Write** no longer enabled automatically at build time (Particle System Shape and
  Terrain Paint Detail need it set manually, else warning and build failure).
- **iOS/tvOS/macOS:** ReplayKit API removed.
- **Unity.Mathematics** now built in (no package install).

### 6.6
- **New projects: Enter Play Mode Settings default to "Reload Scene only"** (previously Reload
  Domain and Scene). Static state is no longer reset on entering Play Mode — see best-practices.
- **Scripting symbols `UNITY_64` and `DEVELOPMENT_BUILD` deprecated.** NOT SOURCEABLE — the
  replacement symbols are not stated on the What's-new page.
- **Mesh Read/Write** no longer enabled at build time (Particle System Shape module, Terrain Detail
  Mesh, Mesh Collider need it set manually, else warning and build failure).
- **Android:** OpenGL ES 3.0 removed (minimum ES 3.1); Legacy and Round icons removed (Adaptive only);
  `-androidChainedSignalHandlerBehavior` `legacy` option removed.
- **Graph Toolkit:** `port.DataType` is `typeof(Untyped)` instead of `null` for data-less ports.
- New projects use `Library/DataStore` instead of `Library/Artifacts`.
- `PlayerSettings.SetManagedCodeVariant` / `ManagedCodeVariant` added; may require upgrade steps
  (NOT SOURCEABLE — steps not listed).
