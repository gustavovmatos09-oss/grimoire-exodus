# Grimlite Rev integration audit

## Goal

Preserve the Grimoire Exodus application, interface, session model, and existing behavior. Port only proven bot/runtime capabilities from Grimlite Rev where Exodus is missing or weaker.

## Source comparison

Repositories reviewed:
- Grimlite Rev: https://github.com/GentleGanku/GrimliteRev
- Grimoire Exodus: https://github.com/gustavovmatos09-oss/grimoire-exodus

Grimlite Rev is a revision of the original Grimoire. Its README documents the app, game-client, and docs areas, and its source is CC0-1.0. Exodus is already a substantially modified fork with session-scoped state and a Ruffle bridge; do not replace it wholesale with Grimlite files.

## Findings

### Existing Exodus capabilities

- `World.Cells` reads the current map's cell names through the Flash bridge.
- `World.Monsters` reads monster objects from `world.monsters`.
- `World.MonstersInCell(cell)` and `World.VisibleMonster(cell)` filter monsters by cell.
- `World.GetMapCells`, `World.CellInfo(cell)`, `World.AllCellInfo`, and `World.ScanMap()` already expose map/cell scanning hooks.
- `Player.AcceptQuest` and `Player.CompleteQuest` already exist.
- `CmdMapItem` / `CmdMapItem2` already collect a map item when its ID is known.
- The ActionScript `GetMapItems` callback already exists and returns `ID`, `name`, `qID`, `itemName`, and `collectMsg` for clickable items in the current cell. The existing `MapItem` model is in `Grimoire.Game.Data.Map`; it must be reused rather than duplicated.
- `CmdKillFor` already loops until inventory/temporary inventory quantity is reached and includes Ruffle-specific target-progress recovery.

### Confirmed behavior to address

- `CmdKill.ExecuteCore` calls `BotUtilities.MoveToBotCell()`, then waits up to 8 seconds for `World.IsMonsterAvailable(name)` in the current cell. If the monster is not available there, the command returns. It does not search other cells.
- Grimlite Rev's `CmdKillFor` delegates to the same single-cell `CmdKill`; it is not itself a cross-map hunt implementation.
- Grimlite Rev has extra command/UI and game-client changes, but copying its files directly would overwrite Exodus-specific session and Ruffle work.

## Safe implementation sequence

1. Add a dedicated hunt-across-cells command/engine helper to Exodus. Use the existing `World.Monsters` / `World.Cells` data to locate a living matching monster, move to its cell using the existing player movement API, wait for map/cell transition, and invoke the existing kill flow. Add cancellation, bounded retries, and clear logs.
2. Inspect the current ActionScript/Ruffle bridge for map-item enumeration. `CmdMapItem` can only click a known ID; automatic collection needs reliable item metadata (ID, quest ID, item name, cell) from the active map. Do not guess IDs or click unrelated objects.
3. Add quest requirement evaluation against inventory and temporary inventory, then implement the Noobshire story sequence starting at quest 33 (quest 32 is already completed by the player's character).
4. Register any new commands in the project file and command editor/serialization paths. Preserve the existing session-aware architecture.
5. Build Debug and Release for the supported target(s), run tests, and validate in AQW before describing the bot as functional.

## Current status

- First implementation pass added to `CmdKill.ExecuteCore`: when the target is not present in the configured/current bot cell, it searches the current map's monster collection for a living monster with the requested name, moves to that monster's cell, and waits for the target to become available before attacking.
- Added `World.MapItemsInMap` and `World.MapItemsForQuest(int questId)`. These scan all named cells through the existing `CellInfo` callback, reuse the existing `MapItem` DTO, deduplicate IDs, and filter by the map item's quest ID.
- This is item discovery, not collection yet. A safe collector still needs to navigate to each item's cell, click the matching item, and verify inventory/quest progress before retrying.
- No Grimlite UI or session code was copied.
- The cross-cell hunt and all-cell map-item discovery changes have not been compiled or tested in AQW yet. Quest requirement parsing and autonomous story progression remain unimplemented. The existing `Quest.RequiredItems` field describes turn-in items, but the complete server-side objective requirement payload still needs to be traced before a generic quest planner can be implemented safely.
- The branch is intentionally separate from `main` so implementation can be reviewed without risking the working version.


## Update: map-item discovery with cell locations

The discovery layer now exposes `World.MapItemLocationsInMap` and
`World.MapItemLocationsForQuest(int questId)`. Each result contains the
`CellName` and the existing `MapItem` object. This preserves the location
needed by a future collector to enter the correct room before calling
`Player.GetMapItem(item.ID)`.

The existing flattened `World.MapItemsInMap` and `World.MapItemsForQuest`
views remain available for compatibility.

Important limitations:
- This is still discovery, not automatic collection. No map item is clicked by
  these methods.
- Item IDs are scoped by cell in the discovery result; the collector should
  re-read `World.MapItems` after entering a cell and use the live ID from that
  cell before clicking.
- Quest completion should be verified through the live quest state
  (`Player.Quests.CanComplete` / `IsInProgress`) rather than assuming that
  a click succeeded.
- The quest model's `RequiredItems` maps to the server's `turnin` payload;
  it must not be treated as a complete representation of farm objectives.
- Build and in-game validation are still pending.
