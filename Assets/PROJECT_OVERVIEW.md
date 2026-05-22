# Quacksire Project Overview

## 1. Project Structure

`Assets/Scene` contains the current gameplay scenes, including `Game Play.unity`.

`Assets/Scenes` is prepared for future organized scene migration. The active project still uses `Assets/Scene` to avoid breaking existing scene references.

`Assets/Scripts` contains existing gameplay scripts such as movement, health, combat, enemy chase, and death handling.

`Assets/Scripts/UI` contains UI-specific systems. `GenshinHudController.cs` builds and updates the Genshin-inspired HUD, minimap, HP bar, stamina bar, action buttons, and marker overlays.

`MobileCameraJoystick.cs` reads the invisible right-side Android camera/look zone. It works beside the invisible left movement zone and does not replace PC mouse camera input.

`Assets/Scripts/Camera` contains camera-specific systems. `GenshinThirdPersonCamera.cs` controls the smooth third-person exploration camera.

`Assets/Scripts/Player` is prepared for future player-only script organization. Existing player scripts were left in their original locations to preserve scene references.

`Assets/Prefabs/UI` contains reusable UI prefab assets. `GenshinHUD.prefab` is a reusable holder for the HUD controller.

`Assets/Materials/UI` is prepared for future UI materials.

`Assets/Animations/UI` is prepared for future UI animation clips.

`Assets/Resources` contains runtime-loadable project assets, including the enemy prefab.

## 2. Health System

`HealthSystem.cs` remains the source of truth for HP. It stores `maxHealth`, `currentHealth`, exposes `Damage`, `Heal`, `SetHealth`, and `SetMaxHealth`, and triggers `OnDeath` when health reaches zero.

The player still has `HealthSystem` attached in `Game Play.unity`. The new HUD reads `CurrentHealth` and `MaxHealth` from that component instead of replacing gameplay health logic.

`GenshinHudController.cs` controls the player-facing HP display. It shows HP text, a teal gradient HP bar, and a delayed gold damage trail. This gives damage feedback without changing how damage is applied.

`WorldHealthBar.cs` is now enemy-focused. It avoids showing a bar on the player and displays slim enemy health bars with a delayed damage trail.

## 3. Camera System

`GenshinThirdPersonCamera.cs` is attached to the existing main `Camera`.

The camera follows the player from a slightly elevated third-person angle. It supports mouse orbit, touch-drag orbit on the right side of the screen, smooth follow lag, scroll-wheel zoom, FOV widening during sprint, and spherecast collision handling.

Android camera orbit is supported through `MobileCameraJoystick.cs`. The current scene has it attached to `CombatSystems`, and `GenshinThirdPersonCamera` reads it through the `cameraJoystick` field. Only touches beginning on the right 60% of the screen rotate the camera, and touches that begin over UI controls are ignored by the look zone.

At runtime the camera disables Cinemachine behaviours so the custom camera and Cinemachine do not fight over the same camera transform. The Cinemachine objects remain in the scene for future reuse.

Important fields to tune:

- `distance`, `minDistance`, `maxDistance`: camera follow distance and zoom limits.
- `pitch`, `minPitch`, `maxPitch`: vertical camera angle.
- `mouseSensitivityX`, `mouseSensitivityY`: camera rotation sensitivity.
- `positionSmoothTime`: follow softness.
- `collisionRadius`, `collisionPadding`: wall and terrain camera collision feel.

## 4. Player Controller

`MobileJoystickPlayerMovement.cs` now supports both joystick and keyboard input. Joystick remains active for mobile, while WASD and arrow keys work for PC testing.

Android movement uses an invisible left touch zone covering 40% of the screen. A touch that begins there moves the player only. A touch that begins on the right 60% rotates the camera only. Touches that begin over UI controls, including the minimap, are ignored by movement so HUD buttons remain accessible.

Movement is camera-relative, so pressing forward moves in the direction the camera is facing. Sprinting is enabled with `LeftShift` on PC or by pushing the joystick strongly past the sprint threshold.

The controller exposes `MoveInput`, `IsSprinting`, `IsMoving`, `CurrentMoveSpeed`, and `AnimationSpeed`.

`PlayerAnimationController.cs` now reads `AnimationSpeed` from movement, so walk and sprint values feed the existing animator `Speed` float more cleanly.

## 5. Minimap System

`GenshinHudController.cs` creates the minimap at runtime under the existing Canvas.

The minimap uses a dedicated orthographic camera named `GenshinMiniMapCamera`, rendering to a small `RenderTexture`. The minimap is circular, top-left anchored, masked, bordered, and rotates with the player direction. Tap the minimap to expand it into a larger centered map, then tap it again to return to the compact view.

The player marker stays centered. Enemy markers use the `Enemy` tag. Objective-style markers are supported through the `Objective`, `Quest`, and `Waypoint` tags if those tags exist in the project.

To modify minimap behavior:

- Change `minimapSize` for UI size.
- Change `minimapWorldRadius` for zoom.
- Change `minimapCameraHeight` for render height.
- Change `minimapCullingMask` to hide/show layers.
- Add tagged objects with `Enemy`, `Objective`, `Quest`, or `Waypoint` to display markers.

## 6. UI System

The HUD is generated by `GenshinHudController.cs` under the existing scene Canvas. It creates:

- Circular top-left minimap.
- Bottom-center character HP and stamina panel.
- HP text display.
- Character icon placeholder with optional sprite override.
- Top-right menu buttons.
- Bottom-right ability buttons.
- Right-side character status list.

The UI uses generated sprites for circles, rounded panels, diamonds, triangles, and gradient bars. This keeps the project self-contained and avoids copying reference assets.

The HUD fades in at runtime through a `CanvasGroup`. Health and stamina values animate smoothly every frame.

## 7. How To Edit The Game

Change UI colors by editing the serialized colors on `GenshinHudController` in the `CombatSystems` GameObject.

Add abilities by expanding `BuildAbilityButtons` in `GenshinHudController.cs`, or by wiring the generated `Button` components to gameplay actions.

Add new characters by replacing the right-side party labels in `BuildPartyStatus`, or by converting that section to read from a party data list.

Modify camera sensitivity on the main `Camera` component `GenshinThirdPersonCamera`: edit `mouseSensitivityX` and `mouseSensitivityY`.

Change minimap size on `GenshinHudController`: edit `minimapSize`.

Change minimap zoom on `GenshinHudController`: edit `minimapWorldRadius`.

Change health values on the player `HealthSystem`: edit `maxHealth` and `currentHealth`.

Add enemies by spawning or placing objects tagged `Enemy` with `EnemyHealth`, `WorldHealthBar`, and enemy movement logic. `CombatDirector.cs` already spawns the runtime enemy with health, bar, chase AI, and animator setup.

Change movement speed on `MobileJoystickPlayerMovement`: edit `moveSpeed` and `sprintSpeed`.

## 8. Performance Optimization

The minimap render texture is 256x256 to keep GPU cost low. The minimap camera uses an orthographic camera and a configurable culling mask so nonessential layers can be excluded.

HUD sprites are generated at runtime once, then reused by UI Images. Health and stamina use simple lerps and Image fill values instead of expensive layout rebuilds.

Enemy and objective markers refresh on a timer rather than searching every frame. Marker positions update every frame only for already tracked objects.

For better performance as the world grows:

- Put minimap-only icons or terrain on dedicated layers.
- Exclude UI and high-detail VFX from `minimapCullingMask`.
- Avoid adding expensive scripts to every marker.
- Keep enemy marker refresh intervals above 0.5 seconds.
- Use object pooling for enemies and projectiles once combat density increases.

## 9. Future Improvements

Inventory system: add item data, pickups, storage, and UI tabs.

Quest system: add quest states, objectives, waypoints, rewards, and minimap integration.

Multiplayer: add networked movement, synced combat, and authority rules.

Skill system: connect the generated ability buttons to cooldowns, elemental attacks, and animations.

Dialogue system: add character portraits, branching dialogue, and quest triggers.

Open world streaming: split terrain and objects into streamed regions for larger maps.

Save/load system: persist health, player position, inventory, quests, and unlocked abilities.
