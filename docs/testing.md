# NoDieNPC manual test checklist

Test on a disposable world with the exact deployed TShock/TZ server build. A green compile is not a multiplayer gameplay test.

1. Start with no config: ensure \`tshock/NoDieNPC.json\` is created, \`GodMode=true\` and \`Freeze=false\`; verify Guide/Clothier can still die to vanilla rituals.
2. Spawn a town NPC, attack it, apply environmental damage and lava. Confirm protection (except IDs in \`GodModeExemptNpcTypes\`) and client health synchronization.
3. Use \`/npc godmode\` twice: confirm settings restore, NPCs can be damaged again, and no other NPC types inherit the old slot's state after despawn/reuse.
4. Freeze NPCs with \`/npc freeze\`, then unfreeze: confirm behavior resumes without altered \`aiStyle\`. Check NPC transformations through shimmer.
5. Trigger \`/reload\` with both valid and malformed JSON. Ensure a malformed file leaves the current configuration active and prints an error.
6. Use \`/npc home\` for housed/homeless NPCs and check client position updates.
7. Run \`/npc summon\` before and after progression milestones. Verify rescue-gated NPCs cannot be created before their rescue flags, no duplicate town NPCs spawn, and reported counts match.
8. Verify an out-of-world town NPC is recovered at world spawn.
9. Disable GodMode, unload the plugin, and reload it. Check original immunity settings and active NPC AI are usable.
10. Stress test with many clients/NPCs and other plugins that change NPC properties or update AI. Note that another plugin's protection may conflict.
