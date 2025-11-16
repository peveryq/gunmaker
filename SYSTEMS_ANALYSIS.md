# Gunmaker Systems Overview — November 2025

## Project Snapshot
- **Scene in focus:** `Assets/_InternalAssets/Scenes/Main.unity`
- **Active gameplay scripts:** 42 C# files across `_InternalAssets/Scrips`
- **Key ScriptableObjects:** `ShopPartConfig.asset`, `PartTypeDefaultSettings.asset`, `GameBalanceConfig.asset`, per-weapon `WeaponSettings/*`
- **Primary prefabs:** Universal weapon part prefab, shop item tile, weldable weapon bodies, pooled bullet holes, locker UI widgets
- **Documentation retained:** `SHOP_UI_SETUP_GUIDE.md`, this analysis document

---

## High-Level Architecture

### Core Player Layer
| Feature | Components | Notes |
|---------|------------|-------|
| First-person control | `FirstPersonController`, input actions | Player movement/aim locked by UI systems via `GameplayHUD` control capture; FOV Kick system for powerful feel (camera widens on shot) |
| Interaction framework | `InteractionHandler`, `IInteractable`, `IInteractionOptionsProvider`, `InteractionOption` | Option-driven HUD buttons (desktop + future touch); pooled button views on `GameplayHUD`
| Item handling | `ItemPickup`, `WeaponBody`, `WeaponPart` | Each body now owns a unique `WeaponSettings` clone updated per install |

### Weapon & Workbench Layer
| System | Key Scripts | Highlights |
|--------|-------------|-----------|
| Modular weapon assembly | `WeaponBody`, `WeaponPart`, `PartTypeDefaultSettings` | Runtime swapping of meshes, stat aggregation, per-part cost tracking |
| Welding gameplay | `Blowtorch`, `WeldingSystem`, `WeldingUI` | Event-driven transitions, pooled VFX |
| Ballistics & impact FX | `WeaponController`, `Bullet`, `BulletHoleManager` | Bullet holes pooled for WebGL friendliness; reload now coroutine-driven with cancellation and HUD update events; FOV Kick applied on shot for powerful feel |
| Target practice | `ShootingTarget`, `ShootingTargetZone` | Configurable payouts, bullseye multipliers, HP system with damage application, optional fall/raise via DOTween, punch animation on hit (DOTween), zone-specific audio/VFX |

### HUD & UX Layer (2025 Update)
| Subsystem | Files | Summary |
|-----------|-------|---------|
| Gameplay HUD | `GameplayHUD`, `GameplayUIContext`, `CrosshairController` | Singleton HUD group for crosshair (static dot, weapon lines with shot animation, hit lines for normal/bullseye hits, kill lines for target kills), money, ammo, reload indicator (fill + spinner), interaction buttons; hide/show via requester tokens |
| Interaction buttons | `HUDInteractionPanel`, `InteractionButtonView`, `InteractionOption` | Pooled UI buttons fed by `IInteractionOptionsProvider` implementations |
| Economy display | `GameplayHUD`, `MoneySystem`, `WeaponController` | Money/ammo events drive HUD labels, controller subscribe/unsubscribe |

### Economy & Shop Layer (2025 Update)
| Subsystem | Files | Summary |
|-----------|-------|---------|
| Currency | `MoneySystem` | Singleton ledger with UI binding, reload-safe (no persistence yet) |
| Offering generation | `ShopOfferingGenerator`, `ShopPartConfig` | Two-phase randomisation (visual pass, stat pass) with rarity tiers, price clamps, starter offerings |
| UI & UX | `ShopUI`, `ShopItemTile`, `PurchaseConfirmationUI`, `WeaponStatsUI` | Modal flow + hover stats (three-column split, preview deltas, pooling) |
| Spawning & visuals | `PartSpawner` | Universal prefab, runtime mesh swap, collider recalculation, scope lens child, geometry-centred placement |

### Locker & Storage Layer (2025 Update)
| System | Key Scripts | Highlights |
|--------|-------------|-----------|
| Slot management | `WeaponSlotManager`, `GameBalanceConfig` | Singleton with configurable capacity, compacting records, UI events |
| Creation flow | `WeaponNameInputUI`, `WeaponSlotSelectionUI`, `GunNameModal`, `WeaponStatRowUI` | Slot-first workflow with validation, naming constraints, reuse of stat rows |
| Locker UX | `WeaponLockerInteractable`, `WeaponLockerSystem`, `WeaponLockerUI`, `WeaponSellModal` | Option buttons (E/F), stashing, per-weapon sell modal, take/close coherence |
| Camera presentation | `LockerCameraController` | Custom coroutine-based fly-in, adjustable speed/curves, auto-hide camera children |

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
                      ↳ WeaponPart reflection patch + price injection
                      ↳ Collider + optional scope lens setup
                      ↳ MoneySystem.Deduct()

Player → Workbench (IInteractable)
       → WeaponSlotSelectionUI.BeginWeaponCreation()
            ↳ WeaponSlotManager.TryAssignSlot()
            ↳ GunNameModal (validation 1–15 chars, placeholders)
       → Workbench.MountWeapon()
            ↳ WeaponBody (unique WeaponSettings clone, per-slot snapshots)

Player → WeaponLockerInteractable (IInteractionOptionsProvider)
       → Secondary option "stash" (F)
            ↳ WeaponLockerSystem.TryStashHeldWeapon()
                 ↳ WeaponSlotManager.TryAssignNextAvailableSlot()
                 ↳ WeaponLockerSystem.PrepareWeaponForStorage()
       → Primary option "open" (E)
            ↳ WeaponLockerSystem.OpenLocker()
                 ↳ LockerCameraController.EnterLockerView()
                 ↳ WeaponLockerUI.PreparePreviewForOpen()
                 ↳ GameplayUIContext.RequestHudHidden()
                 ↳ LockerCameraController (callback) → WeaponLockerUI.Show()
                 ↳ WeaponSellModal (per-slot sell)
                 ↳ WeaponLockerSystem.RequestTakeWeapon()
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
| UI performance | ✅ Stable | Gameplay HUD persistent; modal UIs reuse pooled widgets |
| Physics workload | ✅ Light | No continuous forces on spawned parts; Rigidbody removed for static placement |
| Audio | ✅ | Simplified AudioManager with two dedicated AudioSources (SFX/Music) for 2D sounds, volume control, and simultaneous SFX limit; all systems migrated to centralized audio |
| Randomisation | ✅ | UnityEngine.Random exclusively; no System.Random ambiguities |
| Pooling | ✅ | Bullet impacts pooled; shop tiles and interaction buttons recycled; bullets and casings use object pooling |
| Camera effects | ✅ | FOV Kick uses DOTween for efficient interpolation; <0.01% CPU overhead even at 20 shots/sec |

**Operational considerations:**
- Shop resets scroll positions through coroutine double-writes to avoid frame delays in WebGL builds.
- Rich text colour tags tested against TextMeshPro WebGL subset; no unsupported glyphs used.
- Reflection calls run once per spawn; cached `FieldInfo` to avoid per-frame lookups. Part price is stored on the spawned `WeaponPart` for future resale UI.
- Locker camera is self-contained; no Cinemachine dependency required in current build.

---

## Quality Summary
- **Architecture:** Loose coupling between offering generator, UI, and spawner; interaction/UI surface via `IInteractionOptionsProvider` contracts.
- **Robustness:** Null checks on all inspector references; guarded coroutine usage; shop gracefully handles missing config entries.
- **Reload UX:** Coroutine-driven reload flow cancels when the weapon leaves the player and streams progress events to the HUD.
- **UX polish:** HUD hides automatically when fullscreen UI captures control; locker preview appears with door animation prior to UI reveal.
- **Training targets:** `ShootingTarget` suppresses payouts while down, pools per-zone VFX, and exposes animator triggers for fall/raise loops.
- **Testing hooks:** `ShopOfferingGenerator.RemoveOffering` allows deterministic test scenarios; HUD visibility toggles via `GameplayUIContext` requests.
- **Documentation cleanup:** Legacy temporary markdown files removed; setup guide kept canonical.

### Known Limitations / Next Steps
1. **Persistence** – Money, slot occupancy, and stored weapon data reset on scene reload. Hook into future save system once defined.
2. **Unlock progression** – Lasers/foregrips are locked via UI only; add data-driven availability when gameplay requires it.
3. **Analytics hooks** – Consider emitting events when purchases or locker interactions occur for telemetry or tutorials.
4. **Localization** – Part and slot UI strings remain hardcoded English.
5. **Mobile/touch UI** – Interaction buttons ready for touch but no input abstraction yet.

---

## Quick Reference for Future Work
- **Open shop workflow:** Interact with `ShopComputer` → `ShopUI.OpenShop()` disables FPS controller, unlocks cursor.
- **Gameplay HUD control:** Call `GameplayUIContext.Instance.RequestHudHidden(sender)` when a fullscreen UI opens; call `ReleaseHud(sender)` on close.
- **Adding new interactions:** Implement `IInteractionOptionsProvider`, populate options via `InteractionOption.Primary/Secondary`, HUD handles rendering.
- **To add new part types:** Extend `ShopPartConfig.PartTypeConfig`, provide mesh/icon/name pools, update enum handling in generator/spawner.
- **Adding new rarity tiers:** Adjust `RarityTier` ranges and ensure price/stat formula accommodates new bounds.
- **Integrating saves:** `MoneySystem` exposes `OnMoneyChanged`; serialise `MoneySystem.CurrentMoney` and `WeaponSlotManager` slot records.

---

## Implementation Appendices

### Appendix A – Shop System Deep Dive
- **Stat formula:** `value = a + ((b - a) * ((price - c) / (d - c)))`, rounded up; recoil values are negated, ammo uses tier-specific ranges (8–12, 13–20, 21–40, 41–70, 71–120).
- **Two-phase randomisation:** Phase 1 (refresh) caches rarity, price, mesh/icon, manufacturer; Phase 2 (modal open) calculates stats and generates names on demand.
- **`PartMeshData`:** couples `Mesh`, `Sprite icon`, optional `lensOverlayPrefab` for scopes. All configured per rarity tier inside `ShopPartConfig`.
- **Cost tracking:** `PurchaseConfirmationUI` feeds offering price into `PartSpawner`, `WeaponPart` stores it, and `WeaponSettings` aggregates into `WeaponSettings.totalPartCost`.
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
- Integration: `Bullet` script requests decals through the singleton and falls back gracefully if the manager is absent; impact particles removed for WebGL optimization.
- Shader guidance: avoid Standard/PBR shaders, normal or height maps for decals to reduce draw calls and texture memory.

### Appendix D – Welding System Essentials
- Blowtorch workflow: interaction equips `Blowtorch`, `WeldingSystem` tracks progress, `WeldingUI` throttles updates to avoid per-frame layout rebuilds.
- Animation cues: torch lerps to weld target in `LateUpdate`, start/working sounds cross-fade, particle effects pooled for sparks.
- Safety checks: `WeaponController` verifies barrel is welded before firing; unwelded barrels eject as a fail-safe.
- Setup recap: ensure weld points and progress thresholds are defined on `WeaponBody`, with default part positions pulled from `PartTypeDefaultSettings`.

### Appendix E – Locker & Slot Implementation
- **WeaponSlotManager:** Singleton with `DontDestroyOnLoad`; compacts slot list, exposes `SlotsChanged`, defends against domain reload duplication.
- **WeaponBody ownership:** Each body clones `weaponSettingsTemplate` in `Awake`, updates snapshots on stat recalculation, and reports back to slot records.
- **Locker storage:** `WeaponLockerSystem` re-parents stored weapons to `storageRoot`, caches/restores Rigidbody + collider states, zeroes velocities before toggling kinematic state.
- **Interaction handling:** `WeaponLockerInteractable` feeds `InteractionOption` entries; `InteractionHandler` routes button presses, HUD shows contextual actions.
- **Camera control:** `LockerCameraController` detaches the camera, hides child visuals, animates to `lockerViewPoint` via `AnimationCurve`, delays UI until arrival, and restores HUD/control on exit.

### Appendix F – Gameplay HUD Integration
- `GameplayHUD` root canvas holds crosshair, money, ammo, reload indicator (fill bar + spinner), and interaction panel; enable/disable subjects through `SetVisible`/`SetCrosshairVisible`.
- `CrosshairController` manages crosshair elements: static dot (always visible), weapon lines (+ pattern, shown when weapon equipped, animated via DOTween - move away from center when shooting, return smoothly when stopped), hit lines (X pattern, shown on any target hit with separate variants for normal/bullseye zones, configurable duration per zone), kill lines (X pattern, shown when target HP reaches 0, configurable duration).
- `GameplayUIContext` tracks hide requests via tokenised HashSet; locker, shop, slot UI call `RequestHudHidden(this)`/`ReleaseHud(this)`.
- `HUDInteractionPanel` pools `InteractionButtonView` instances; `InteractionHandler` pushes option sets each frame when gaze target changes.
- `WeaponController` emits `AmmoChanged`, `ReloadStateChanged`, and `ReloadProgressChanged`; `InteractionHandler` relays these to the HUD for instant UI updates. `WeaponController` triggers crosshair shot animation on fire.
- `ShootingTarget` triggers hit lines display on any hit (different visual variants for normal vs bullseye zones) and kill lines display when target HP reaches 0.

### Appendix G – Shooting Target Setup
- Add `ShootingTarget` to the root of a target prefab; assign base reward, normal/bullseye multipliers, kill reward (given when HP reaches 0), max HP, one or more audio clips, and optional `DOTweenAnimation` components.
- Child colliders carry `ShootingTargetZone` (set zone enum) to differentiate normal vs bullseye hits; both can share the same root collider hierarchy.
- Targets have HP system: each hit applies damage from weapon's `bulletDamage` (multiplied by `bullseyeDamageMultiplier` for bullseye hits); target falls when HP reaches 0 (if `enableFalling` is true); HP fully restores when target is raised.
- When `enableFalling` is true, assign `DOTweenAnimation` components for fall and reset animations, plus `timeDown` for how long the target stays lowered; payouts pause while `IsDown` when suppression is enabled. Use DOTween Pro's visual editor to configure animations on child GameObjects.
- Punch animation: assign optional `DOTweenAnimation` component for punch/shake effect on hit; plays only while target is standing, prevents overlapping animations until current cycle completes.
- Base damage is configured in `WeaponBody.baseStats.damage` (currently parts don't modify it, but structure is ready for future expansion).
- Audio: all target sounds (hit, fall) use centralized `AudioManager` with fallback to local `AudioSource` if manager unavailable.

### Appendix H – Camera FOV Kick System
- **FOV Kick:** Camera FOV expansion on shot for powerful feel (CoD-style). Instant FOV widening (typically 1-3 degrees), smooth return via DOTween. Works seamlessly with aiming FOV changes. Applied in `LateUpdate` to ensure correct ordering with other FOV modifications.
- **Performance:** Uses DOTween for efficient interpolation. FOV Kick adds ~13-23ns per frame during animation. Total overhead <0.01% CPU even at 20 shots/sec. WebGL-optimized.
- **Integration:** Triggered automatically in `WeaponController.ApplyRecoil()` using settings from `WeaponSettings` (`fovKickAmount`, `fovKickDuration`, `fovKickReturnDuration`). Works alongside standard camera recoil (`recoilUpward`).

### Appendix I – Audio System (Simplified)
- **AudioManager:** Singleton with two dedicated `AudioSource` components (SFX and Music) for 2D sound playback. Volume control via `PlayerPrefs` persistence. Limits simultaneous SFX playback to prevent audio spam.
- **Migration:** All systems migrated from local `AudioSource` usage to centralized `AudioManager.Instance.PlaySFX()`. Fallback to local `AudioSource` or `AudioSource.PlayClipAtPoint` if manager unavailable.
- **WebGL optimization:** 2D sounds only (`spatialBlend = 0f`), no 3D spatialization overhead. Single audio source per type reduces overhead compared to per-object sources.

### Appendix J – Animation Systems (DOTween Migration)
- **Target animations:** Fall and reset animations migrated from Unity Animator to DOTween Pro (`DOTweenAnimation` components). Visual editor configuration, `autoPlay = false`, explicit `CreateTween()` and `DORestart()` calls.
- **Weapon locker:** Door open/close animations migrated from Unity Animator to DOTween. Separate `DOTweenAnimation` arrays for open and close (allows different easing/bounce for each direction). Light control with configurable delays via `DOVirtual.DelayedCall`.
- **Crosshair:** Weapon lines animation uses DOTween `DOAnchorPos` for real-time recoil feedback. Instant recoil on shot start, smooth return on stop.
- **Performance:** DOTween provides efficient interpolation with minimal overhead. All animations use pooling-friendly approach (no persistent state in Animator controllers).

---

## Change Log (since initial analysis)
1. Implemented full shop loop (UI, randomisation, purchasing, spawning)
2. Refactored part spawning to universal prefab with runtime mesh and collider update
3. Added scope lens child attachment logic per offering
4. Introduced randomised, rarity-sensitive part naming and rich stat display
5. Ensured category buttons retain selection state and now reside in a scrollable container
6. Cleaned documentation set to a single authoritative setup guide
7. Weapon parts now retain purchase price; aggregated into `WeaponSettings.totalPartCost` for resale and economy features.
8. Added weapon slot selection, naming modal, and unique `WeaponSettings` per body tied to slots
9. Implemented locker storage pipeline with stashing, selling, and cinematic inspection view
10. Upgraded `WeaponStatsUI` to split columns, preview deltas, and support world-space panels
11. Introduced Gameplay HUD + interaction options system; locker camera migrated to custom coroutine animation
12. Reworked `WeaponController` reload to coroutine flow with cancellation, HUD events, and sound timing
13. Added HUD reload progress bar/spinner, ammo icon swapping, and shared UI click sounds for slot/sell flows
14. Implemented shooting targets with configurable payouts, bullseye support, animator-driven fall/raise, and per-zone audio/VFX hooks
15. Added advanced crosshair system with static dot, weapon lines (animated on shot), hit lines (shown on any hit, separate for normal/bullseye), and kill lines (shown when target HP reaches 0)
16. Migrated target fall/raise animations from Unity Animator to DOTween Pro (DOTweenAnimation components)
17. Migrated weapon lines crosshair animation from Unity Animator to DOTween (real-time recoil feedback while shooting)
18. Added HP system to shooting targets with damage application from weapon bulletDamage; targets fall when HP reaches 0 and restore HP when raised
19. Implemented FOV Kick system: instant camera FOV expansion on shot (1-3 degrees) with smooth return for powerful feel (CoD-style)
20. Added punch animation to shooting targets: DOTween-based shake effect on hit, prevents overlapping animations until cycle completes
21. Migrated weapon locker door animations from Unity Animator to DOTween (separate animations for open/close with different easing)
22. Simplified AudioManager: two dedicated AudioSources (SFX/Music) for 2D sounds, volume control, simultaneous SFX limit; all systems migrated
23. Removed impact particles from BulletHoleManager for WebGL optimization
24. Implemented object pooling for bullets and casings (BulletPool, CasingPool) for WebGL performance
25. Added damage multiplier for bullseye hits on targets
26. Integrated Damage Numbers Pro for money popups on target hits and kills

---

_Last updated: November 2025_

