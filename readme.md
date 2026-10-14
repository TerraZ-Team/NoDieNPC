# NoDieNPC

Server-side management of **town NPCs** for TShock / TSAPI (.NET 9). The plugin uses **per-NPC events** rather than iterating every NPC from GameUpdate.

## Commands

All commands require the permission **npc.use**.

| Command | Action |
| --- | --- |
| /npc | Show help |
| /npc freeze | Toggle town NPC AI freeze |
| /npc godmode | Toggle damage protection |
| /npc home | Return housed NPCs home and homeless NPCs to world spawn |
| /npc summon | Spawn missing town NPCs from a curated candidate list when requirements match |
| /npc status | Show current modes and eligible / blocked NPC types |

TShock \`/reload\` reloads the JSON config without resetting NPC AI styles.

## Configuration

Generated at \`tshock/NoDieNPC.json\`:

\`\`\`json
{
  "Freeze": false,
  "GodMode": true,
  "GodModeExemptNpcTypes": [22, 54]
}
\`\`\`

- **GodMode** defaults to **true**, retaining the old TerraZ-Team behavior.
- NPC **22 (Guide)** and **54 (Clothier)** are excluded by default because their deaths are needed for vanilla summon rituals. Remove IDs from the exemption list if you intentionally want to protect them.
- **Freeze** cancels town NPC AI in \`NpcAIUpdate\` and stops velocity without modifying \`aiStyle\`. Shimmer-transparent NPCs are left alone to finish their transformation.
- **GodMode** uses \`NpcStrike\`, \`NpcSpawn\`, \`NpcAIUpdate\` and initial world reconciliation. Original immunity/damage settings are restored when it is disabled and when the plugin unloads.
- **Out-of-world recovery** is performed when that NPC next receives an AI update. \`/npc home\` is manual.
- \`/npc summon\` is an **admin action**, not the vanilla town NPC arrival algorithm. It checks world progression and rescue flags for listed NPCs, but it does **not** check every normal arrival condition such as money, inventory, housing, or some NPC-specific rules. Pets/Princess/Truffle and other special-case NPCs are intentionally omitted. Do not use this command if you want strict vanilla progression.

## Building

\`\`\`sh
dotnet restore src/NoDieNPC.csproj
dotnet build src/NoDieNPC.csproj -c Release --no-restore
\`\`\`

Copy the resulting \`NoDieNPC.dll\` into the TShock \`ServerPlugins\` directory. Requires a compatible version of \`TShockAPI.TZ\` and .NET 9.

See [manual verification scenarios](docs/testing.md) before deploying to a live world.
