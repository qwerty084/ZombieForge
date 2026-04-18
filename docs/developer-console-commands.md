# Developer Console Commands (Black Ops 1 & 2)

> Commands relevant to Black Ops 1 and Black Ops 2.
> Source: [Call of Duty Wiki — Developer Console](https://callofduty.fandom.com/wiki/Developer_console)

---

## Toggle Commands

| Command | Effect | Notes |
|---------|--------|-------|
| `god` | Cannot take damage or die. | Dogs can still down the player (no technical death — use `kill` to resume). Flogger and Punji Stakes can still damage the player. |
| `demigod` | Take damage, but cannot die from most things. | Zombies can still kill the player normally. |
| `noclip` | Free movement through anything in any direction. | Weapons cannot be used in this state. |
| `ufo` | Flight mode, much faster than noclip. | Forward/backward movement only. Use lean keys (Q/E) to move vertically. |
| `notarget` | Player is not noticed by any AI. | Zombies move toward the position where the command was entered, then stop. |
| `kill` | Kills the player. | |
| `dropweapon` | Drops the current weapon. | |
| `take all` | Removes all weapons and health. | |
| `take ammo` | Removes all ammunition. | Also removes grenades (lethal and tactical). |
| `take weapons` | Removes all weapons. | |
| `give all` | Gives one of every weapon with max ammo. | Includes Pack-a-Punched variants on Der Riese / some custom maps, plus console-only weapons. |
| `give ammo` | Refills ammo for all currently held weapons. | |
| `map_restart` | Restarts the map without a loading screen. | |

---

## Commands Requiring Arguments

| Command | Effect | Notes |
|---------|--------|-------|
| `friendlyfire_dev_disabled <0\|1>` | Allows shooting friendly NPCs without restarting. | `0` = default (off), `1` = enabled. |
| `cg_thirdperson <0\|1>` | Enables or disables third person camera. | Multiplayer and Zombies only. |
| `player_burstFireCooldown <value>` | Adjusts or removes the burst delay on burst-fire weapons. | Also works on semi-auto weapons to slow fire rate. |
| `cg_draw2D <0\|1>` | Removes all HUD elements. | Useful for screenshots; pair with `cg_drawGun 0` and `noclip`. |
| `cg_drawGun <0\|1>` | Hides the weapon and player model when set to `0`. | |
| `g_gravity <value>` | Adjusts player gravity. | Works well with `jump_height`. |
| `g_speed <value>` | Adjusts player movement speed. | Keep under `1000` to avoid erratic movement / crashes. |
| `cg_laserlight <0\|1>` | Toggles laser light visibility. | |
| `cg_LaserForceOn <0\|1>` | Adds a visible laser pointer to all weapons. | Laser originates from the barrel. |
| `cg_LaserRange <value>` | Sets NPC laser range. | |
| `cg_LaserRangePlayer <value>` | Sets the player's laser range. | |
| `player_meleeRange <value>` | Sets the player's melee range. | Up to `1000` — no lunge, but AI still takes damage. |
| `player_sprintSpeedScale <value>` | Sets the player's sprint speed. | |
| `player_sprintTime <value>` | Sets the player's sprint duration. | Max `12.8`. Works in Campaign, Multiplayer, and Zombies. |
| `player_sustainammo <0\|1>` | Unlimited magazines for all weapons. | Works in multiplayer. |
| `player_ClipSizeMultiplier <value>` | Multiplies clip/magazine size by the entered value. | e.g. `2` doubles the clip. |
| `timescale <value>` | Sets game speed. | Default `1.0` = real time. |
| `cg_fov <value>` | Adjusts field of view / depth perception. | Range: `60`–`160` in singleplayer; `60`–`80` in multiplayer. |
| `jump_height <value>` | Adjusts jump height. | Default is `39`. Works well with `g_gravity`. |
| `r_fullbright <0\|1>` | Disables shading and visual effects; brightens the level. | Can improve performance on low-end PCs. |
| `perk_weapRateMultiplier <value>` | Modifies the Double Tap effect (Zombies & Multiplayer). | Values below `1` increase fire rate. Below `0.038` may cause bolt-action weapons to glitch full-auto. |
| `perk_weapReloadMultiplier <value>` | Modifies Speed Cola / Sleight of Hand effect. | Values below `1` increase reload speed (`0.01` = near-instant). |
| `ragdoll_bullet_force <value>` | Ragdoll force applied by bullets to dead bodies. | Up to `18000`. |
| `ragdoll_explosive_force <value>` | Ragdoll force applied by explosives to dead bodies. | Up to `60000`. |
| `cg_gun_x <value>` | Moves the gun forward or backward. | Positive = forward, negative = backward. Default `0`. |
| `cg_gun_y <value>` | Moves the gun up or down. | Positive = up, negative = down. Default `0`. |
| `cg_gun_z <value>` | Moves the gun left or right. | Positive = right, negative = left. Default `0`. |
| `take <item_name>` | Removes the specified weapon. | Use the console name, not in-game name. Only works for weapons present in the level. |
| `give <item_name>` | Gives the specified weapon. | Use the console name, not in-game name. Only works for weapons present in the level. |

---

## Map Commands

| Command | Effect | Notes |
|---------|--------|-------|
| `map <name>` | Changes the map. | Use the console map name (e.g. `map zombie_theater`). |
| `spmap <name>` | Loads map in singleplayer. | |
| `mpmap <name>` | Loads map in multiplayer. | |
| `devmap <name>` | Loads map in developer mode, enabling console commands. | |
| `spdevmap <name>` | Singleplayer devmap. | |
| `mpdevmap <name>` | Multiplayer devmap. | |

---

## Enabling Console Commands (BO1)

1. Navigate to `C:\Program Files\Steam\SteamApps\common\Call of Duty Black Ops\players\`
2. Open `config.cfg` in a text editor (Notepad, Wordpad, etc.)
3. Search (`Ctrl+F`) for `monkeytoy` — find `seta monkeytoy "0"` and change `0` to `1`
4. Scroll to the very bottom of the file and add your binds (one per line)
5. Save the file, then **set it to Read-Only**: right-click → Properties → check **Read-only**

> ⚠️ Cheats (noclip, god mode, etc.) **do not work online**.

---

## Key Binds

Syntax: `bind <KEY> "<command>"`  
The quotes around the command are required.

**Examples:**

```
bind 5 "say Max ammo!"
bind J "noclip"
bind K "give ammo"
```

---

## Useful Config Tweaks

| Tweak | How |
|-------|-----|
| Show FPS in Zombies / Singleplayer + see host | Search for `cg_drawFPS` in `config.cfg`, change `"0"` to `"1"` |
| Add a clan tag in Zombies | Add a new line: `seta clanName "your clan name"` |
| Replenish ammo on a key | `bind <KEY> "give ammo"` |

---

## Chat Text Color Codes

Prefix text with `^` followed by a color code to colorize chat messages.

| Code | Color |
|------|-------|
| `^1` | Red |
| `^2` | Green |
| `^3` | Yellow |
| `^4` | Blue |
| `^5` | Teal |
| `^6` | Pink |
| `^7` | White |
| `^8` | Grey |
| `^9` | Brown |
| `^0` | Black |
| `^@` | Brown-Orange |
| `^=` | Lighter Blue |
| `^;` | Darker Green |
| `^:` | Darker Red |
| `^>` | Lighter Grey |
| `^<` | Orange |
| `^?` | Purple |

**Example:** `bind 5 "say ^2Max ^7ammo!"` — prints "Max ammo!" with "Max" in green and "ammo!" in white.

---

## Zombies Weapon Names (for `give` / `take`)

Use these names with `give <name>` or `take <name>`.

### Special / Wonder Weapons
| Console Name | Weapon |
|---|---|
| `humangun_zm` | Winter's Howl |
| `humangun_upgraded_zm` | Winter's Fury |
| `ray_gun_zm` | Ray Gun |
| `ray_gun_upgraded_zm` | Porter's X2 Ray Gun |
| `thundergun_zm` | Thundergun |
| `thundergun_upgraded_zm` | Zeus Cannon |
| `freeze_gun_zm` | Winter's Howl (alt) |
| `freeze_gun_upgraded_zm` | Winter's Howl Upgraded (alt) |
| `crossbow_explosive_zm` | Crossbow |
| `crossbow_explosive_upgraded_zm` | Awful Lawton |
| `knife_ballistic_zm` | Ballistic Knife |
| `knife_ballistic_upgraded_zm` | The Krauss Refibrillator |
| `knife_ballistic_bowie_zm` | Bowie Knife |
| `knife_ballistic_bowie_upgraded_zm` | Bowie Knife Upgraded |
| `zombie_cymbal_monkey` | Cymbal Monkey |

### Pistols
| Console Name | Weapon |
|---|---|
| `m1911_zm` | M1911 |
| `m1911_upgraded_zm` | Mustang & Sally |
| `python_zm` | Python |
| `python_upgraded_zm` | Cobra |
| `cz75_zm` | CZ75 |
| `cz75_upgraded_zm` | CZ75 Fully Auto |
| `cz75dw_zm` | CZ75 Dual Wield |
| `cz75dw_upgraded_zm` | CZ75 Dual Wield Upgraded |

### Assault Rifles
| Console Name | Weapon |
|---|---|
| `m14_zm` | M14 |
| `m14_upgraded_zm` | Mnesia |
| `m16_zm` | M16 |
| `m16_gl_upgraded_zm` | Skullcrusher |
| `g11_lps_zm` | G11 |
| `g11_lps_upgraded_zm` | G115 Generator |
| `famas_zm` | FAMAS |
| `famas_upgraded_zm` | G16-GL35 |
| `galil_zm` | Galil |
| `galil_upgraded_zm` | Lamentation |
| `commando_zm` | Commando |
| `commando_upgraded_zm` | Predator |
| `fnfal_zm` | FN FAL |
| `fnfal_upgraded_zm` | EPC WN |

### Submachine Guns
| Console Name | Weapon |
|---|---|
| `ak74u_zm` | AK74u |
| `ak74u_upgraded_zm` | AK74fu2 |
| `mp5k_zm` | MP5K |
| `mp5k_upgraded_zm` | MP115 Kollider |
| `mp40_zm` | MP40 |
| `mp40_upgraded_zm` | The Afterburner |
| `mpl_zm` | MPL |
| `mpl_upgraded_zm` | MPL-LF |
| `pm63_zm` | PM63 |
| `pm63_upgraded_zm` | Tokyo & Rose |
| `spectre_zm` | Spectre |
| `spectre_upgraded_zm` | Phantom |

### Shotguns
| Console Name | Weapon |
|---|---|
| `ithaca_zm` | Stakeout |
| `ithaca_upgraded_zm` | Raid |
| `rottweil72_zm` | Olympia |
| `rottweil72_upgraded_zm` | Hades |
| `spas_zm` | SPAS-12 |
| `spas_upgraded_zm` | SPAZ-24 |
| `hs10_zm` | HS10 |
| `hs10_upgraded_zm` | Typhoid & Mary |

### Light Machine Guns
| Console Name | Weapon |
|---|---|
| `aug_acog_zm` | AUG |
| `aug_acog_mk_upgraded_zm` | AUG ACOG Upgraded |
| `rpk_zm` | RPK |
| `rpk_upgraded_zm` | R115 Resonator |
| `hk21_zm` | HK21 |
| `hk21_upgraded_zm` | H115 Oscillator |

### Sniper Rifles
| Console Name | Weapon |
|---|---|
| `dragunov_zm` | Dragunov |
| `dragunov_upgraded_zm` | D115 Disassembler |
| `l96a1_zm` | L96A1 |
| `l96a1_upgraded_zm` | L115 Isolator |

### Launchers & Special
| Console Name | Weapon |
|---|---|
| `m72_law_zm` | M72 LAW |
| `m72_law_upgraded_zm` | M72 Anarchy |
| `china_lake_zm` | China Lake |
| `china_lake_upgraded_zm` | China Beach |
| `minigun_zm` | Minigun |

### Equipment
| Console Name | Item |
|---|---|
| `frag_grenade_zm` | Frag Grenade |
| `claymore_zm` | Claymore |

---

## Controller Bind Keys

Use these key names with `bind <KEY> "<command>"` for controller support.

| Key Name | Button |
|---|---|
| `DPAD_RIGHT` | D-Pad Right |
| `DPAD_DOWN` | D-Pad Down |
| `DPAD_LEFT` | D-Pad Left |
| `DPAD_UP` | D-Pad Up |
| `BUTTON_RSTICK` | Right Stick Click |
| `BUTTON_LSTICK` | Left Stick Click |
| `BUTTON_B` | B / Circle |
| `BUTTON_Y` | Y / Triangle |
| `BUTTON_X` | X / Square |
| `BUTTON_A` | A / Cross |
| `BUTTON_LSHLDR` | Left Bumper (LB) |
| `BUTTON_RSHLDR` | Right Bumper (RB) |
| `BUTTON_RTRIG` | Right Trigger (RT) |
| `BUTTON_LTRIG` | Left Trigger (LT) |

---

## Map Discovery Flags

These dvars mark maps as discovered/unlocked in the menu.

| Command | Effect |
|---|---|
| `zombiefive_discovered 1` | Unlocks Five in the map select |
| `zombietron_discovered 1` | Unlocks Ascension in the map select |

---

## Extended DVARs Reference (Zombies)

### Player Movement & Physics
| Command | Effect | Notes |
|---|---|---|
| `g_gravity <value>` | Adjusts gravity. | Higher values = more air strafing. |
| `g_speed 400` | Sets player movement speed. | Default ~190. |
| `jump_height 999` | Sets jump height. | Default 39; 999 ≈ 8–10 ft jumps. |
| `player_sprintUnlimited 1` | Unlimited sprint. | |
| `player_sprintSpeedScale <value>` | Multiplies sprint speed. | |
| `phys_gravity 99` | Sets physics gravity for ragdolls. | Low values make zombie corpses float. |

### Player Combat
| Command | Effect | Notes |
|---|---|---|
| `player_meleeRange 999` | Extends melee reach. | |
| `player_meleeWidth 999` | Extends melee hit width. | |
| `player_clipSizeMultiplier 999` | Multiplies magazine size. | |
| `player_sustainAmmo 1` | Unlimited ammo (no reload depletion). | |
| `player_burstFireCooldown 0` | Removes burst fire delay. | Makes M16 and G11 full-auto. |
| `player_lastStandBleedoutTime 400` | Sets bleedout timer in last stand (seconds). | Default is much shorter. |
| `Revive_Trigger_Radius 99999` | Massively increases the radius to trigger a revive. | |

### Perks
| Command | Effect | Notes |
|---|---|---|
| `perk_weapReloadMultiplier 0.001` | Super Speed Cola / Sleight of Hand. | Near-instant reloads. |
| `perk_weapRateMultiplier 0` | Super Double Tap. | Maximum fire rate. |
| `perk_armorVest 0` | Super Juggernog. | Minimum damage taken. |

### AI
| Command | Effect | Notes |
|---|---|---|
| `notarget` | Zombies ignore the player. | They move to where the player entered the command, then stop. |
| `ai_meleeRange 0` | Dogs deal no melee damage. | |
| `ai_disableSpawn` | Disables zombie spawning. | |
| `ai axis delete` | Kills all zombies currently on the map. | |

### Visuals & HUD
| Command | Effect | Notes |
|---|---|---|
| `cg_drawFPS 1` | Shows FPS counter in top-right corner. | Also shows which player is host. |
| `r_fog 0` | Disables fog. | |
| `r_fullbright <0\|1>` | Disables shading; brightens everything. | `toggle r_fullbright 1 0` to bind a toggle. |
| `toggle r_colorMap 1 2 3 0` | Cycles through color map modes (vision effects). | Bind to a key. |
| `cg_fov <value>` | Sets field of view. | Common values: `80` (default), `105` (wide), `65` (narrow). |
| `cg_thirdPerson 1` | Enables third-person camera. | |
| `cg_draw2D 0` | Hides all HUD elements. | |

### Bullet Trails (use together)
| Command | Effect |
|---|---|
| `cg_tracerLength 999` | Sets tracer length. |
| `cg_tracerSpeed 0020` | Sets tracer speed. |
| `cg_tracerWidth 15` | Sets tracer width. |
| `cg_gun_x 7` | Offsets gun position forward. |

### Game World
| Command | Effect | Notes |
|---|---|---|
| `timescale <value>` | Sets game speed. | `1` = normal, `0.1` = slow-mo, `10` = fast. Use `toggle timescale 1 10 .1` to bind a toggle. |
| `magic_chest_movable 0` | Prevents the mystery box from moving. | |
| `toggle g_speed 100 200 500` | Cycles player speed through 100 / 200 / 500. | Bind to a key. |

### Useful Toggle Bind Examples
```
bind J "noclip"
bind U "ufo"
bind G "god"
bind T "notarget"
bind F "toggle r_fullbright 1 0"
bind H "toggle timescale 1 10 .1"
bind N "toggle g_speed 100 200 500"
bind V "toggle r_colorMap 1 2 3 0"
bind P "cg_fov 80"
bind O "cg_thirdPerson"
bind K "ai axis delete"
bind L "give ammo"
```
