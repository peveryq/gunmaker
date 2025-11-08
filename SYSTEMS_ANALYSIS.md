# Gunmaker Systems Overview — November 2025

## Project Snapshot
- **Scene in focus:** `Assets/_InternalAssets/Scenes/Main.unity`
- **Active gameplay scripts:** 31 C# files across `_InternalAssets/Scrips`
- **Key ScriptableObjects:** `ShopPartConfig.asset`, `PartTypeDefaultSettings.asset`, `WeaponSettings/*`
- **Primary prefabs:** Universal weapon part prefab, shop item tile, weldable weapon bodies, pooled bullet holes
- **Documentation retained:** `SHOP_UI_SETUP_GUIDE.md`, this analysis document

---

## High-Level Architecture

### Core Player Layer
| Feature | Components | Notes |
|---------|------------|-------|
| First-person control | `FirstPersonController`, input actions | Cursor and controller state managed when UI panels open |
| Interaction framework | `InteractionHandler`, `IInteractable` implementers | Shop computer, workbench, pickups share the same entry point |
| Item handling | `ItemPickup`, `WeaponBody`, `WeaponPart` | Reflection is used to bridge private fields in third-party assets |

### Weapon & Workbench Layer
| System | Key Scripts | Highlights |
|--------|-------------|-----------|
| Modular weapon assembly | `WeaponBody`, `WeaponPart`, `PartTypeDefaultSettings` | Runtime swapping of meshes, stat aggregation |
| Welding gameplay | `Blowtorch`, `WeldingSystem`, `WeldingUI` | Event-driven transitions, pooled VFX |
| Ballistics & impact FX | `WeaponController`, `Bullet`, `BulletHoleManager` | Bullet holes pooled for WebGL friendliness |

### Economy & Shop Layer (2025 Update)
| Subsystem | Files | Summary |
|-----------|-------|---------|
| Currency | `MoneySystem` | Singleton ledger with UI binding, reload-safe (no persistence yet) |
| Offering generation | `ShopOfferingGenerator`, `ShopPartConfig` | Two-phase randomisation (visual pass, stat pass) with rarity tiers, price clamps, starter offerings |
| UI & UX | `ShopUI`, `ShopItemTile`, `PurchaseConfirmationUI` | Full-screen modal flow, category scroller, rich stat text, ESC handling |
| Spawning & visuals | `PartSpawner` | Universal prefab, runtime mesh swap, collider recalculation, scope lens child, geometry-centred placement |

---

## Data & Flow Overview
```
Player → ShopComputer (IInteractable)
       → ShopUI.OpenShop()
            ↳ MoneySystem (balance display)
            ↳ ShopOfferingGenerator (category cache of ShopOffering)
                 ↳ ShopPartConfig (tier data, mesh/icon/name pools)
            ↳ ShopItemTile (icon, rarity, refresh button)
            ↳ PurchaseConfirmationUI (stat calculation on demand)
                 ↳ PartSpawner.SpawnPart()
                      ↳ WeaponPart reflection patch
                      ↳ Collider + optional scope lens setup
                      ↳ MoneySystem.Deduct()
```

- **Two-phase randomisation:** Tile generation caches rarity, price, mesh, icon, manufacturer. Stat roll occurs when the player inspects a tile, preventing unnecessary calculations.
- **Starter safety net:** Barrel and magazine categories inject a zero-cost, zero-stat offering (with 8 ammo for magazines) as the first tile.
- **Name localisation prep:** Part type display names now come from `ShopPartConfig.PartTypeConfig.partTypeDisplayName`, enabling future localisation passes.
- **Icon ↔ Mesh pairing:** `PartMeshData` links meshes, sprites, and optional scope lens prefabs to keep visuals and geometry aligned.

---

## WebGL Readiness Assessment
| Area | Status | Notes |
|------|--------|-------|
| Instantiation cost | ✅ Minimal | Universal prefab reused; collider updates performed in-place |
| GC pressure | ✅ Low | Dictionaries cached per offering; no LINQ in hot paths |
| UI performance | ✅ Stable | ScrollRect usage trimmed; category list uses mask-only scrolling (no extra sliders) |
| Physics workload | ✅ Light | No continuous forces on spawned parts; Rigidbody removed for static placement |
| Audio | ✅ | 2D UI sounds (shop) and optional 3D spawn sound; AudioSource reuse |
| Randomisation | ✅ | UnityEngine.Random exclusively; no System.Random ambiguities |
| Pooling | ✅ | Bullet impacts pooled; shop tiles recycled per refresh |

**Operational considerations:**
- Shop resets scroll positions through coroutine double-writes to avoid frame delays in WebGL builds.
- Rich text colour tags tested against TextMeshPro WebGL subset; no unsupported glyphs used.
- Reflection calls run once per spawn; cached `FieldInfo` to avoid per-frame lookups.

---

## Quality Summary
- **Architecture:** Loose coupling between offering generator, UI, and spawner (Mediator pattern via events/callbacks).
- **Robustness:** Null checks on all inspector references; guarded coroutine usage; shop gracefully handles missing config entries.
- **UX polish:** Category buttons enforce selection state, text colour swapping, and scrollable list without visible scrollbars.
- **Testing hooks:** `ShopOfferingGenerator.RemoveOffering` allows deterministic test scenarios for post-purchase refresh.
- **Documentation cleanup:** Legacy temporary markdown files removed; setup guide kept canonical.

### Known Limitations / Next Steps
1. **Persistence** – Money and shop state reset on scene reload. Hook into future save system once defined.
2. **Unlock progression** – Lasers/foregrips are locked via UI only; add data-driven availability when gameplay requires it.
3. **Analytics hooks** – Consider emitting events when purchases occur for telemetry or tutorials.
4. **Localization** – Part names support localisation-ready fragments; remaining UI strings still hardcoded English.

---

## Quick Reference for Future Work
- **Open shop workflow:** Interact with `ShopComputer` → `ShopUI.OpenShop()` disables FPS controller, unlocks cursor.
- **To add new part types:** Extend `ShopPartConfig.PartTypeConfig`, provide mesh/icon/name pools, update enum handling in generator/spawner.
- **Adding new rarity tiers:** Adjust `RarityTier` ranges and ensure price/stat formula accommodates new bounds.
- **Integrating saves:** `MoneySystem` already exposes balance events; serialise `MoneySystem.CurrentMoney` and `ShopOfferingGenerator` caches.

---

## Implementation Appendices

### Appendix A – Shop System Deep Dive
- **Stat formula:** `value = a + ((b - a) * ((price - c) / (d - c)))`, rounded up; recoil values are negated, ammo uses tier-specific ranges (8–12, 13–20, 21–40, 41–70, 71–120).
- **Two-phase randomisation:** Phase 1 (refresh) caches rarity, price, mesh/icon, manufacturer; Phase 2 (modal open) calculates stats and generates names on demand.
- **`PartMeshData`:** couples `Mesh`, `Sprite icon`, optional `lensOverlayPrefab` for scopes. All configured per rarity tier inside `ShopPartConfig`.
- **Name generation:** Combines rarity-specific descriptors from `partNamePool` with `partTypeDisplayName` defined in config (lowercase for localisation readiness).
- **Starter items:** Barrel and magazine categories reserve index 0 for zero-cost offerings (magazines grant 8 ammo baseline).
- **UI behaviour:** Category list uses a scrollbar-less `ScrollRect`; tiles and modal use TextMeshPro rich text for coloured stat deltas; ESC closes modal first then shop.

### Appendix B – Part Spawner & Collider Notes
- Universal prefab workflow: `PartSpawner.SpawnPart()` swaps the mesh, applies stats via reflection, sets generated name/type, and optionally parents scope lens overlays.
- Geometry alignment: world offset computed with `meshFilter.transform.TransformPoint(partMesh.bounds.center)` to place the mesh centre at the spawn point.
- Collider refresh: `UpdateCollider()` supports `BoxCollider`, `MeshCollider`, `CapsuleCollider`, `SphereCollider`; logs a warning if no collider present. Box colliders are recommended for general parts.
- Audio/physics: current build spawns parts without impulse forces for deterministic placement; spawn sound plays through a reusable `AudioSource`.

### Appendix C – Bullet Hole System (FX)
- Pool-based manager (`BulletHoleManager`) maintains a fixed number of decals, eliminating runtime allocations; older decals are recycled with optional fade-out via `MaterialPropertyBlock`.
- Recommended prefab: quad mesh with `Unlit/Transparent` material, alpha texture, `Cull Off`, `ZWrite Off`; keep resolution 256–512px for WebGL.
- Integration: `Bullet` script requests decals through the singleton and falls back gracefully if the manager is absent; impact particles are optional to keep WebGL budgets low.
- Shader guidance: avoid Standard/PBR shaders, normal or height maps for decals to reduce draw calls and texture memory.

### Appendix D – Welding System Essentials
- Blowtorch workflow: interaction equips `Blowtorch`, `WeldingSystem` tracks progress, `WeldingUI` throttles updates to avoid per-frame layout rebuilds.
- Animation cues: torch lerps to weld target in `LateUpdate`, start/working sounds cross-fade, particle effects pooled for sparks.
- Safety checks: `WeaponController` verifies barrel is welded before firing; unwelded barrels eject as a fail-safe.
- Setup recap: ensure weld points and progress thresholds are defined on `WeaponBody`, with default part positions pulled from `PartTypeDefaultSettings`.

---

## Change Log (since initial analysis)
1. Implemented full shop loop (UI, randomisation, purchasing, spawning)
2. Refactored part spawning to universal prefab with runtime mesh and collider update
3. Added scope lens child attachment logic per offering
4. Introduced randomised, rarity-sensitive part naming and rich stat display
5. Ensured category buttons retain selection state and now reside in a scrollable container
6. Cleaned documentation set to a single authoritative setup guide

---

_Last updated: 08 Nov 2025_

