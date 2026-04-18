# Black Ops 1 Memory Map

Reference data for Black Ops 1 Zombies reverse engineering used by ZombieForge and BlackOpsMonitor.

---

## Key Hook Addresses

- SCR_NOTIFY_ID_ADDR hook point: `0x483E2F` (Scr_NotifyNum detour site)
- SL_ConvertToString: `0x687530` (resolves string IDs to char*)
- STRING_TABLE_BASE (scrStringGlob): `0x03067C00` (array of 2 pointers, one per script instance)
- VM_Notify original: `0x8A87C0`
- VM_Notify hook sites: `0x41D2E5` and `0x8AB798`

## SL_ConvertToString Disassembly

Confirmed via x32dbg.

```text
00687530  mov eax, [esp+4]           ; eax = stringValue
00687534  test eax, eax
00687536  je 0068754B                ; if 0, return NULL
00687538  mov ecx, [esp+8]           ; ecx = inst (0 or 1)
0068753C  mov edx, [ecx*4+3067C00]   ; edx = scrStringGlob[inst]
00687543  shl eax, 4                 ; stringValue * 16 (entry size)
00687546  lea eax, [edx+eax+4]       ; string data at offset +4
0068754A  ret
```

Lookup formula:

`tableBase = *(char**)(0x03067C00 + inst * 4); return tableBase + (id * 16) + 4;`

## Hook Mechanism

- At `0x483E2F`: 5-byte E9 JMP detour into Scr_NotifyNum. EDX register holds notification string ID (bit 0 = flag, EDX >> 8 = string table index)
- At `0x41D2E5` and `0x8AB798`: 4-byte CALL patches into VM_Notify handler. Calls SL_ConvertToString to resolve string, then strcmp against registered event names.

## Player State Memory

- Base: `0x01C08B40` (29395776), Stride: 7464 bytes per player
- Points offset: +7048
- Kills offset: +7052
- Downs offset: +7076
- Headshots offset: +7084
- Name offset: +6968
- Noclip offset: +7180

## Game Function Addresses

- Cbuf_AddText: `0x0049B930`
- SV_GameSendServerCommand: `0x005441F0`
- Dvar_FindVar: `0x005AEA10`
- Scr_RegisterNotifyHandler: `0x00661400`
- Scr_GetString: `0x00567CB0`
- G_GetWeaponIndexForName: `0x005C2740`
- BG_GetWeaponName: `0x00450180`

## Entity Memory

- Base: `0x01A7E5F8`, Stride: 844 bytes
- Health offset: +388
- MaxHealth offset: +392
- Flags offset: +368

## DLL-Registered Script Events

- chest_accessed
- leverDown
- switch_activated
- powerup_grabbed
- power_on
- safe_restart
- saferestart

## Global Data

- Server/Level Time: `0x0286D014` (42389524)
