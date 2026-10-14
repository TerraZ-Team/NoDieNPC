# NoDieNPC manual test checklist

Test on a disposable world with the exact deployed TShock/TZ server build. A green compile is not a multiplayer gameplay test.

1. Start with no config: ensure `tshock/NoDieNPC.json` contains only `GodMode=true` and `Freeze=false`. Both Guide and Clothier must be protected.
2. With GodMode ON, use Flymeal, Rotten Eggs, ordinary weapons/projectiles and enemy attacks against all town NPCs (including Guide/Clothier). Verify they take **zero damage** and clients agree on health.
3. Submerge town NPCs completely in **lava**, **water** and **honey** for longer than the normal drowning time. Verify no damage or death even while `/npc freeze` is ON.
5. Use `/npc godmode` to turn protection OFF. Ensure NPCs can once again be damaged/drowned/killed, and vanilla Guide/Clothier voodoo boss summon rituals work. Turn GodMode ON again and verify they are protected. Ensure no unrelated NPC inherits a recycled slot's state.
5. Freeze NPCs with `/npc freeze`, then unfreeze: confirm behavior resumes without altered `aiStyle`. Check NPC transformations through shimmer.
6. Trigger `/reload` with both valid and malformed JSON. Ensure a malformed file leaves the current configuration active and prints an error.
7. Use `/npc home` for housed/homeless NPCs and check client position updates.
8. Run `/npc summon` before and after progression milestones. Verify rescue-gated NPCs cannot be created before their rescue flags, no duplicate town NPCs spawn, and reported counts match.
9. Verify an out-of-world town NPC is recovered at world spawn.
11. Disable GodMode, unload the plugin, and reload it. Check original immunity settings and active NPC AI are usable.
11. Stress test with many clients/NPCs and other plugins that change NPC properties or update AI. Note that another plugin's protection may conflict.
