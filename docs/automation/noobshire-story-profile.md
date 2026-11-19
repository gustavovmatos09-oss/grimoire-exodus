# Noobshire story profile

This profile is intentionally limited to Captain Rolith's Noobshire story chain. It must not accept unrelated quests merely because they are available.

## Progression

The user has already completed quest 32 (Defend the Rats), so the first target is quest 33.

| Quest ID | Quest | Verified objective | Automation rule |
|---:|---|---|---|
| 33 | Rats in the Cradle | Collect 6 Baby Rats by clicking the golden map rats; collect 1 Horc Cheese from Horc Noob | Use map-item discovery/clicking for Baby Rats and monster farming for Horc Cheese |
| 35 | Short in Spoons | Collect 5 Spoons from Kittarian Mouse Eater | Farm the named monster |
| 36 | Short in Forks | Collect 5 Forks from Kittarian Mouse Flayer | Farm the named monster |
| 37 | Letter Interception | Collect Horc's Letter from Horc Noob | Farm the named monster |
| 38 | Not a Noob | Collect Horc Trainer Horns from Horc Trainer | Farm the named monster |
| 39 | Missing King | Click the moving Rat Leader map item near Rolith; collect Rat King Clue from Horc Noob | Re-scan map-item locations if the item moves, then farm the named monster |
| 40 | Take Down | Domesticated Rat from Horc Trainer | Skip: Legend/member-only |

Quest 34 is not part of this sequence and must not be treated as a quest to accept.

## Acceptance and progression rules

1. If a target quest is already in progress, resume its objective work; do not attempt to accept it again.
2. Otherwise, load its quest metadata and check the live AQW availability state before accepting.
3. Never accept a member-only quest.
4. If a target quest is unavailable or locked, skip it and inspect only the next quest ID in this profile. Do not substitute unrelated side quests.
5. Complete/turn in only after the objective requirements are actually satisfied according to the game client.
6. Do not infer kill/click objectives from the quest's `turnin` property. `turnin` is the turn-in item list; `reqd` is a separate prerequisite/validation list. The story profile above is explicit objective metadata.
7. Map items are not normal monster drops. The collector must discover the item's current cell, move there, re-read the live map item ID, and click it. Rat Leader can move, so locations must be re-scanned when needed.

## Source verification

The objective descriptions and member restriction were cross-checked against the official AQW Noobshire guide and the Rolith quest reference:

- https://www.aq.com/lore/guides/noobshireguide
- https://aqwwiki.wikidot.com/rolith-s-quests

This document records objective metadata; it does not claim that an autonomous Noobshire runner is already implemented or tested in-game.
