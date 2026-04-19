# Game Events — Semantics Reference

This document describes the **meaning and behavior** of every game event currently captured by the BlackOpsMonitor → ZombieForge IPC pipeline.

For the low-level IPC contract (shared memory layout, ring-buffer protocol, enum numeric values) see [ipc-protocol.md](ipc-protocol.md).

---

## Common fields

Every event slot carries three fields:

| Field            | Type  | Meaning |
|------------------|-------|---------|
| `eventType`      | `int` | One of the `GameEventType` values below. |
| `eventTimestamp` | `int` | Server/level time in milliseconds at the moment the notify fired. Sourced from `LEVEL_TIME_ADDR` (`0x0286D014`). |
| `eventValue`     | `int` | Extra payload. Currently **always `0`** — no event populates this field yet. Reserved for future use (e.g. player slot, powerup ID). |

---

## Captured events

### `StartOfRound` (1)

| | |
|---|---|
| **Notify string** | `start_of_round` |
| **Fires when** | The game script signals the beginning of a new zombie round — after the brief intermission and just before zombies begin spawning. |
| **eventValue** | 0 (unused) |
| **Notes** | Fires once per round, including dog rounds. On dog rounds you will receive both `DogRound` and `StartOfRound`. |

---

### `EndOfRound` (2)

| | |
|---|---|
| **Notify string** | `end_of_round` |
| **Fires when** | The last zombie (or dog) of the current round is killed and the round-end sequence begins. |
| **eventValue** | 0 (unused) |
| **Notes** | Followed shortly by `StartOfRound` for the next round. The two can fire very close together in time. |

---

### `PowerupGrabbed` (3)

| | |
|---|---|
| **Notify string** | `powerup_grabbed` |
| **Fires when** | A player picks up a powerup drop (e.g. Nuke, Insta-Kill, Double Points, Max Ammo, Carpenter, Death Machine). |
| **eventValue** | 0 (unused — powerup type not yet forwarded) |
| **Notes** | Can burst: if multiple powerups are grabbed in quick succession each grab fires its own event. The specific powerup type is not exposed through the IPC payload yet. |

---

### `DogRound` (4)

| | |
|---|---|
| **Notify string** | `dog_round_starting` |
| **Fires when** | A hellhound (dog) round is about to begin — before dogs start spawning. |
| **eventValue** | 0 (unused) |
| **Notes** | Dog rounds are special rounds that replace the normal zombie wave. `StartOfRound` also fires for the same round. Dog rounds typically occur every 4–5 rounds after round 5. |

---

### `PowerOn` (5)

| | |
|---|---|
| **Notify string** | `power_on` |
| **Fires when** | The map's power switch is activated (e.g. the power lever on Kino der Toten / Five). |
| **eventValue** | 0 (unused) |
| **Notes** | Fires once per game session. On maps without power (e.g. Nacht der Untoten) this event never fires. |

---

### `EndGame` (6)

| | |
|---|---|
| **Notify string** | `end_game` |
| **Fires when** | The game session ends — typically when all players are downed simultaneously (game over). |
| **eventValue** | 0 (unused) |
| **Notes** | Signals that stats collection for the current session is complete. A new `StartOfRound` after this indicates a fresh game has begun. |

---

### `PerkPurchased` (7)

| | |
|---|---|
| **Notify string** | `perk_bought` |
| **Fires when** | A player buys a perk from a perk machine (e.g. Juggernog, Speed Cola, Double Tap, Quick Revive). |
| **eventValue** | 0 (unused — perk identity and player slot not yet forwarded) |
| **Notes** | Fires once per perk purchase. The specific perk and buying player are not exposed through the IPC payload yet. |

---

## Known-but-uncaptured notify strings

The following notify strings are registered by the game script and observed during reverse engineering but are **not currently forwarded** over IPC. They are candidates for future events.

| Notify string      | Observed meaning |
|--------------------|-----------------|
| `chest_accessed`   | Player opened the mystery box. |
| `leverDown`        | A lever/switch was pulled (map-specific). |
| `switch_activated` | A trigger switch was activated (map-specific). |
| `safe_restart`     | Safe restart sequence initiated. |
| `saferestart`      | Alias for `safe_restart` (duplicate string seen in script). |

To capture any of these, add entries to the `g_events[]` table in `BlackOpsMonitor/Hook.cpp`, add the corresponding value to both `GameEventType` enums, and update `ipc-protocol.md` per the extension rules described there.
