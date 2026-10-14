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

TShock `/reload` reloads the JSON config without resetting NPC AI styles.

## Configuration

Generated at `tshock/NoDieNPC.json`:

```json
{
  "Freeze": false,
  "GodMode": true
}
```

- **GodMode** defaults to **true** and protects **every active town NPC** against player attacks (including Flymeal and Rotten Eggs), environmental damage, lava, and drowning in water or honey. It combines NPC-level `dontTakeDamage`, `immortal` and `lavaImmune` flags with `NpcStrike` cancellation and a full breath meter maintained by `NpcAIUpdate`. There is no per-damage-type configuration.
- **Important:** Guide and Clothier are **also protected** while GodMode is active. To perform vanilla voodoo boss summons, temporarily run `/npc godmode` to turn protection off, then turn it back on afterward.
- **Freeze** cancels town NPC AI in `NpcAIUpdate` and stops velocity without modifying `aiStyle`. Shimmer-transparent NPCs are left alone to finish their transformation.
- NPC protection uses `NpcStrike`, `NpcSpawn`, `NpcAIUpdate` and initial world reconciliation. Original damage flags are restored on disable or plugin unload. A previous config field `GodModeExemptNpcTypes` is no longer used and can be deleted from old configs.
- **Out-of-world recovery** is performed when that NPC next receives an AI update. `/npc home` is manual.
- `/npc summon` is an **admin action**, not the vanilla town NPC arrival algorithm. It checks world progression, rescued-NPC flags, and purchased pet licenses where available, but it does **not** check every normal arrival condition such as money, inventory, housing, or some NPC-specific rules. The Truffle, Princess and Zoologist conditions are intentionally approximate; town slimes and other special-case NPCs are omitted. Do not use this command if you want strict vanilla progression.

## Building

```sh
dotnet restore src/NoDieNPC.csproj
dotnet build src/NoDieNPC.csproj -c Release --no-restore
```

Copy the resulting `NoDieNPC.dll` into the TShock `ServerPlugins` directory. Requires a compatible version of `TShockAPI.TZ` and .NET 9.

## Continuous integration and deployment

- `.github/workflows/build.yml` checks the .NET 9 build on branch pushes and pull requests.
- `.github/workflows/build-deploy.yml` runs on **any pushed Git tag** or by **workflow_dispatch** in GitHub Actions.
- The deploy workflow builds `NoDieNPC.dll` on Ubuntu, uploads a workflow artifact (kept 14 days), and deploys **only that DLL** using a `[self-hosted, windows, x64]` runner.
- Destination is the GitHub Actions repository variable `PLUGIN_DEPLOY_DIR`; if unset, the Windows runner uses `C:\github-deploy`, consistent with other TerraZ-Team plugins.
- Deployment copies the DLL into the configured directory. It **does not restart TShock**, install the DLL into another server path, or create a GitHub Release. The Windows self-hosted runner must be online and have write access to the directory.

To deploy from a version tag after merging the workflow:

```sh
git tag v1.1.0
git push origin v1.1.0
```

Alternatively, run **Build and deploy plugin** with **Run workflow** on the Actions tab.

See [manual verification scenarios](docs/testing.md) before deploying to a live world.
