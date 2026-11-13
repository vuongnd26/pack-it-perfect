# Unity 6.3 LTS — Deprecated APIs

**Last verified:** 2026-02-13

Quick lookup table for deprecated APIs and their replacements.
Format: **Don't use X** → **Use Y instead**

---

## Input

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Input.GetKey()` | `Keyboard.current[Key.X].isPressed` | New Input System |
| `Input.GetKeyDown()` | `Keyboard.current[Key.X].wasPressedThisFrame` | New Input System |
| `Input.GetMouseButton()` | `Mouse.current.leftButton.isPressed` | New Input System |
| `Input.GetAxis()` | `InputAction` callbacks | New Input System |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` | New Input System |

**Migration:** Install `com.unity.inputsystem` package.

---

## UI

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Canvas` (UGUI) | `UIDocument` (UI Toolkit) | UI Toolkit is now production-ready |
| `Text` component | `TextMeshPro` or UI Toolkit `Label` | Better rendering, fewer draw calls |
| `Image` component | UI Toolkit `VisualElement` with background | More flexible styling |

**Migration:** UGUI still works, but UI Toolkit is recommended for new projects.

---

## DOTS/Entities

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `ComponentSystem` | `ISystem` (unmanaged) | Entities 1.0+ complete rewrite |
| `JobComponentSystem` | `ISystem` with `IJobEntity` | Burst-compatible |
| `GameObjectEntity` | Pure ECS workflow | No GameObject conversion |
| `EntityManager.CreateEntity()` (old signature) | `EntityManager.CreateEntity(EntityArchetype)` | Explicit archetype |
| `ComponentDataFromEntity<T>` | `ComponentLookup<T>` | Entities 1.0+ rename |

**Migration:** See Entities package migration guide. Major refactor required.

---

## Rendering

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CommandBuffer.DrawMesh()` | RenderGraph API | URP/HDRP render passes |
| `OnPreRender()` / `OnPostRender()` | `RenderPipelineManager` callbacks | SRP compatibility |
| `Camera.SetReplacementShader()` | Custom render pass | Not supported in SRP |

---

## Physics

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Physics.RaycastAll()` | `Physics.RaycastNonAlloc()` | Avoid GC allocations |
| `Rigidbody.velocity` (direct write) | `Rigidbody.AddForce()` | Better physics stability |

---

## Asset Loading

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Resources.Load()` | Addressables | Better memory control, async loading |
| Synchronous asset loading | `Addressables.LoadAssetAsync()` | Non-blocking |

---

## Animation

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Animation component | Animator Controller | Mecanim system |
| `Animation.Play()` | `Animator.Play()` | State machine control |

---

## Particles

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Particle System | Visual Effect Graph | GPU-accelerated, more performant |

---

## Scripting

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |

---

## Platform-Specific

### WebGL
| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| WebGL 1.0 | WebGL 2.0 or WebGPU | Unity 6+ defaults to WebGPU |

---

## Quick Migration Patterns

### Input Example
```csharp
// ❌ Deprecated
if (Input.GetKeyDown(KeyCode.Space)) {
    Jump();
}

// ✅ New Input System
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) {
    Jump();
}
```

### Asset Loading Example
```csharp
// ❌ Deprecated
var prefab = Resources.Load<GameObject>("Enemies/Goblin");

// ✅ Addressables
var handle = Addressables.LoadAssetAsync<GameObject>("Enemies/Goblin");
await handle.Task;
var prefab = handle.Result;
```

### UI Example
```csharp
// ❌ Deprecated (UGUI)
GetComponent<Text>().text = "Score: 100";

// ✅ TextMeshPro
GetComponent<TextMeshProUGUI>().text = "Score: 100";

// ✅ UI Toolkit
rootVisualElement.Q<Label>("score-label").text = "Score: 100";
```

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/deprecated-features.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Migration.html


---

## 6.3 → 6.6

**Last verified:** 2026-11-12
**Sources:** https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64 · https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity65 · https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66.
**Coverage limit:** only what the What's-new pages state; the upgrade guides were not read.

| Don't use | Use instead | Notes |
|-----------|-------------|-------|
| `Object.GetInstanceID()`, `Resources.InstanceIDToObject`, `Selection.instanceIDs` | `EntityId` | Deprecated 6.4, compile errors in 6.5. `EntityId` is not an `int`. |
| `GameObject.rigidbody`, `GameObject.camera`, `Component.renderer` | `GetComponent<T>()` | Removed 6.5 |
| `GameObject.AddComponent(string)` | `AddComponent<T>()` | Removed 6.5 |
| `UnityEngine.LowLevelPhysics2D` / `LowLevelPhysics2D` | `Unity.U2D.Physics` / `PhysicsCore2D` | Renamed 6.5 |
| URP Compatibility Mode / `URP_COMPATIBILITY_MODE` | Render Graph custom passes | Removed 6.4 |
| PVRTC textures | ASTC or ETC | Removed 6.4 |
| Built-In Render Pipeline | URP | Deprecated 6.5; supported through the 6.7 LTS lifecycle |
| Dynamic batching | — | Deprecated 6.5, obsolete in 6.6 (replacement NOT SOURCEABLE) |
| `AndroidAssetPacks.RequestToUseMobileDataAsync` | `AndroidAssetPacks.ShowConfirmationDialogAsync` | Deprecated 6.5 |
| **Hide Navigation Bar** player setting, `unity.launch-fullscreen` | Insets (`unity.requested-visible-insets`, `unity.system-bars-behavior`) | 6.5, Android |
| OpenGL ES 3.0, Legacy/Round Android icons | ES 3.1 minimum, Adaptive icons | Removed 6.6 |
| `UNITY_64`, `DEVELOPMENT_BUILD` symbols | NOT SOURCEABLE | Deprecated 6.6; replacement not stated |
| Progressive CPU Light Baker | Compute Light Baker or Progressive GPU Light Baker | Deprecated 6.6 |
| Managed ECS components, Entities Hierarchy window, Entities Journaling | Unmanaged structs with `UnityObjectRef<T>`; see 6.6 page | Deprecated 6.5/6.6 (not used by this project) |
