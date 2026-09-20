# Team Workflow

This project is shared by three contributors. To avoid merge conflicts, each person should work in their own Unity-owned area and avoid editing another person's scene or prefab without agreeing first.

## Main Rule

Do not build maps or UI through generator scripts. Build them as real Unity scene objects, prefabs, materials, and Canvas assets inside the Unity Editor.

Runtime scripts are still allowed for behavior such as inventory, pickup interaction, input handling, bunker events, and scene transitions.

## Ownership Areas

### Map Owner

Owns:
- `Assets/_Project/Scenes/Scavenge.unity`
- `Assets/_Project/Scenes/Shelter.unity`
- `Assets/_Project/Prefabs/Map/`
- `Assets/_Project/Prefabs/Environment/`
- `Assets/_Project/Materials/Map/`
- Environment blockout objects, room layout, doors, props, bunker entrance placement

Avoid editing:
- UI scenes and UI prefabs
- Story data unless requested
- Shared runtime scripts without coordination

### UI Owner

Owns:
- `Assets/_Project/Scenes/UI/`
- `Assets/_Project/Prefabs/UI/`
- `Assets/_Project/Materials/UI/`
- Canvas, HUD, inventory UI, prompts, menus

Avoid editing:
- Map layout scenes
- Story data unless requested
- Shared runtime scripts without coordination

### Story Owner

Owns:
- `Assets/_Project/Scenes/Story/`
- `Assets/_Project/Data/Story/`
- `Assets/_Project/Prefabs/Story/`
- Dialogue, quest beats, narrative triggers, cutscene placeholders

Avoid editing:
- Map layout scenes
- UI prefabs and UI scenes
- Shared runtime scripts without coordination

## Runtime Scene Structure

The playable build uses these four scenes:

- `MainMenu.unity`: title screen
- `Prologue.unity`: opening text sequence
- `Scavenge.unity`: house interior, pickups, bunker hatch, and the 60-second collection phase
- `Shelter.unity`: bunker operation scene and facilities

The obsolete `Scenes/Map` test, backup, and preview assets were removed on 2026-07-29. Do not recreate the old additive `Prologue_Map` structure unless the team deliberately changes the runtime scene architecture.

## Camera Rule

`Scavenge` and `Shelter` must remain real 3D spaces viewed through a Perspective 2.5D camera. Do not switch either gameplay scene to Orthographic.

## Prefab Rule

Shared objects should become prefabs before repeated use.

Examples:
- `Pickup_Clothes.prefab`
- `Pickup_FirstAidKit.prefab`
- `BunkerHatch.prefab`
- `InteractionPromptUI.prefab`

Prefab owners should be clear from the folder:
- Map prefabs in `Prefabs/Map`
- UI prefabs in `Prefabs/UI`
- Story prefabs in `Prefabs/Story`

## Script Rule

Scripts should only contain reusable behavior. They should not procedurally create final maps or UI layouts.

Allowed:
- `PickupItem`
- `PlayerInventory`
- `BunkerHatch`
- camera follow/setup behavior
- interaction events

Avoid:
- map generator scripts for production content
- UI generator scripts for production UI
- automatic scene rewriting scripts

## Before Editing Shared Files

Ask the team before changing:
- `Assets/_Project/Scenes/Prologue.unity`
- `Assets/_Project/Scripts/Core/`
- `Assets/_Project/Scripts/Interaction/`
- `ProjectSettings/`
- `Packages/manifest.json`

## Suggested Daily Flow

1. Pull latest changes.
2. Work only in your owned folder or scene.
3. Test in Unity.
4. Commit a focused set of files.
5. Tell the team if you touched shared scripts, project settings, or the integration scene.
