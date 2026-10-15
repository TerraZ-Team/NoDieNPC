# NoDieNPC manual test checklist

Test on a disposable world with the exact deployed TShock/TZ server build. A green compile is not a multiplayer gameplay test.

1. Start with no config: ensure `tshock/NoDieNPC.json` contains only `GodMode=true` and `Freeze=false`. Both Guide and Clothier must be protected.
2. With GodMode ON, use Flymeal, Rotten Eggs, ordinary weapons/projectiles and enemy attacks against all town NPCs (including Guide/Clothier). Verify they take **zero damage** and clients agree on health.
3. Submerge town NPCs completely in **lava**, **water** and **honey** for longer than the normal drowning time. Verify no damage or death even while `/npc freeze` is ON.
4. Use `/npc godmode` to turn protection OFF. Ensure NPCs can once again be damaged/drowned/killed, and vanilla Guide/Clothier voodoo boss summon rituals work. Turn GodMode ON again and verify they are protected. Ensure no unrelated NPC inherits a recycled slot's state.
5. Freeze NPCs with `/npc freeze`, then unfreeze: confirm behavior resumes without altered `aiStyle`. Check NPC transformations through shimmer. **Regression:** with freeze enabled, spawn a town NPC several tiles above the ground (`/npc summon`) and use `/npc home` for a homeless NPC. NPCs must fall at vanilla gravity speed and settle normally; they must not hover/fall a pixel at a time.
6. Measure outgoing NPC update packets (ID 23) while a frozen NPC is falling. The freeze handler must not emit one `netUpdate` per tick due to Y velocity; verify this for at least 10 seconds and after repeated `/npc home`, `/npc freeze` toggles.
7. With freeze enabled, NPCs standing on blocks must stay put and not transmit continuous NPC update packets solely due to the freeze hook.
8. Trigger `/reload` with both valid and malformed JSON. Ensure a malformed file leaves the current configuration active and prints an error.
9. Use `/npc home` for housed/homeless NPCs and check client position updates.
10. Run `/npc summon` before and after progression milestones. Verify rescue-gated NPCs cannot be created before their rescue flags, no duplicate town NPCs spawn, and reported counts match.
11. Verify an out-of-world town NPC is recovered at world spawn.
12. Disable GodMode, unload the plugin, and reload it. Check original immunity settings and active NPC AI are usable.
13. Stress test with many clients/NPCs and other plugins that change NPC properties or update AI. Note that another plugin's protection may conflict.
