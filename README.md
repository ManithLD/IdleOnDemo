# IdleOnDemo

IdleOnDemo is a Unity 6 2D RPG / idle-MMO prototype focused on modular gameplay systems, event-driven UI, data-driven content, and scalable scene flow. The demo includes platformer movement, click-to-target combat, auto-attacking, quests, portals, enemy drops, inventory, progression, and HUD feedback systems built around clear ownership boundaries.

## Demo Video

<video src="./IdleOnDemo.mp4" controls width="100%"></video>

[Watch the demo video](./IdleOnDemo.mp4)

## Technical Architecture

The project is organized around persistent services, focused gameplay components, and event-driven communication between runtime systems and UI. Core managers are loaded through a persistent `Systems` prefab, allowing shared systems such as quest tracking, inventory, scene transitions, notifications, and HUD controllers to remain centralized instead of being duplicated across scenes.

Gameplay data is primarily driven through ScriptableObjects. `QuestData` defines quest IDs, objectives, requirements, and rewards, while `ItemData` defines item identity, icons, stackability, and item-specific world pickup prefabs. Runtime state is kept separate from static data through classes such as `QuestRuntimeState`, which lets the same quest definitions remain reusable and clean.

The codebase uses observer-style C# events to keep systems decoupled. Quest updates, inventory changes, player stat changes, enemy health changes, enemy death, and target selection all notify listeners without requiring UI or secondary systems to poll every frame. Combat also uses an interface-based contract through `IDamageable`, allowing `PlayerCombat` to apply damage without depending directly on enemy implementation details.

## Core Systems

### Persistent Bootstrap

`SystemBootstrapper` loads `Resources/Systems.prefab` before the first scene loads. This centralizes managers and persistent UI so scenes can stay focused on level content rather than service setup.

### Scene Transitions

`SceneTransitionManager`, `Portal`, and `SpawnPoint` provide click-triggered portal travel with asynchronous scene loading, fade transitions, persistent player and camera handling, duplicate cleanup, destination spawn points, and quest-gated portal locks.

### Player Controller

`PlayerController` separates frame input from physics movement by reading input in `Update()` and applying Rigidbody2D velocity in `FixedUpdate()`. It includes coyote time, jump release damping, ground-only collision casts, facing control, animation updates, and simulated input support for combat chase behavior.

### Combat

`PlayerTargeting` owns click-to-target selection, while `PlayerCombat` handles pursuit, manual attacks, auto-attacks, sticky auto-targeting, cooldowns, direct `IDamageable` damage, attack-type selection, and per-attack tuning profiles. The combat flow avoids ambiguous hit scans by damaging the selected target directly once the player is in range.

### Enemies

`EnemyController` implements `IDamageable` and manages health, death, rewards, quest progress reporting, roaming behavior, selection state, sprite-facing conventions, and optional flying-enemy behavior. Enemy death events are used by other systems without pushing drop or UI responsibilities into the enemy lifecycle.

### Quests

`QuestManager` tracks quest runtime state in dictionary registries and supports a linear main quest chain. `QuestData` assets define objective IDs, required counts, titles, descriptions, and rewards, while portals, NPCs, and enemy deaths report objective progress into the same central system.

### Inventory and Drops

`InventoryService`, `ItemData`, `EnemyDropTable`, and `ItemPickup` form a data-driven loot pipeline. Enemies roll independent drop entries, spawn item-specific pickup prefabs, and clicked pickups add stackable item quantities into a capped inventory service.

### Progression

`PlayerStats` tracks HP, damage, coins, XP, and level state. It exposes events for HP, XP, level-up, and coin changes so HUD components can update only when relevant data changes.

### UI and Feedback

The HUD includes player progression, inventory slots, currency, navigation buttons, auto-combat toggles, attack-type selection, quest display, loot notifications, enemy health bars, and damage popups. UI components subscribe to gameplay events and encapsulate their own display logic, keeping presentation code separate from gameplay ownership.

## Key Features

- 2D player movement with jumping, coyote time, facing, grounded checks, and combat movement locks.
- Click-to-target enemy selection with a selection indicator.
- Manual combat that chases selected enemies and attacks when in range.
- Auto-attack mode with sticky target selection and Y-axis filtering.
- Four selectable attack types: Default, Slash, Dash, and ThreeSixty.
- Per-attack damage multiplier and cooldown profiles.
- Damage popups with value-based colors and fade animation.
- Enemy patrol and roaming within spawner zones.
- Ground and flying enemy support.
- Enemy health bars and death animations.
- XP gain, level-up loop, HP tracking, coin tracking, and HUD updates.
- Data-driven quest chain with auto-accept and auto-turn-in behavior.
- Quest objectives reported by portals, NPC interactions, and enemy kills.
- Quest UI with animated letter-by-letter reveal for new quests.
- Quest-gated portals with locked and unlocked animation states.
- Async scene transitions with fade and spawn-point relocation.
- Persistent systems prefab bootstrapped before scene load.
- Data-driven enemy item drops.
- Item-specific world pickup prefabs.
- Click-to-loot item pickup behavior.
- Inventory service with stackable items and a 15 unique-slot capacity.
- Inventory UI slot rendering.
- Coin UI.
- Loot notifications for coins and items.
- HUD menu buttons for auto combat, attack menu, inventory menu, and quit action.
- Cinemachine camera bounds rebinding helper for scene transitions.

## Project Structure

```text
Assets/
  Data/
    Items/                  ItemData assets used by drops and inventory.
  Prefabs/
    Combat/                 Damage popup and combat feedback prefabs.
    Enemies/                Enemy prefabs and shared enemy setup.
    Inventory/              Item UI and inventory-related prefabs.
    UI/                     HUD and notification prefabs.
  Resources/
    Systems.prefab          Persistent managers and UI loaded before scene start.
  Scenes/                   Playable Unity scenes and transition destinations.
  Scripts/
    Core/                   Bootstrap, scene transition, interfaces, and shared mechanics.
    Gameplay/
      Combat/               Player targeting, combat logic, and damage popups.
      Enemies/              Enemy controller, selection, health, and behavior scripts.
      Environment/          Portals, spawn points, camera setup, NPCs, and spawners.
      Inventory/            Item data, pickups, drop tables, and inventory service.
      Player/               Player-facing gameplay types such as attack enums.
      Progression/          Player stats, coins, HP, XP, and leveling.
      Quests/               Quest data, runtime state, and quest manager.
    UI/                     HUD controllers, quest UI, inventory UI, and notifications.
```

## Development Approach

The implementation prioritizes clean ownership, low coupling, and scalable iteration. Runtime systems keep authoritative state in one place: quests in `QuestManager`, inventory in `InventoryService`, and player progression in `PlayerStats`. Presentation systems subscribe to events instead of polling, while gameplay systems use focused contracts such as `IDamageable` to avoid unnecessary dependencies.

Performance-conscious choices are used throughout the prototype. Physics movement is applied in `FixedUpdate()`, target and movement checks use layer masks, combat profile lookup uses dictionaries, and components cache references instead of repeatedly resolving them during core loops. Inspector-facing systems also clamp values through `OnValidate`, unsubscribe from events during teardown, and guard against missing references to keep iteration safe in Unity.

## Running the Project

1. Install Unity `6000.4.10f1` or a compatible Unity 6 version.
2. Clone or download the repository.
3. Open the project folder in Unity Hub.
4. Open the main starting scene from `Assets/Scenes/`.
5. Press Play in the Unity Editor.

The persistent systems are loaded automatically from `Assets/Resources/Systems.prefab` at runtime, so scenes do not need duplicate manager objects as long as that prefab remains configured.
