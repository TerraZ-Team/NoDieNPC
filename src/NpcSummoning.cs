using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TShockAPI;

namespace NoDieNPC;

internal static class NpcSummoning
{
    // Administrative eligibility, intentionally conservative for rescued NPCs.
    // This is not Terraria's complete automatic housing/spawn algorithm.
    // Avoid summoning pets, Truffle, Princess, etc. without their extra checks.
    private static readonly Dictionary<int, Func<bool>> Candidates = new()
    {
        [NPCID.Guide] = () => true,
        [NPCID.Merchant] = () => true,
        [NPCID.Nurse] = () => true,
        [NPCID.Demolitionist] = () => true,
        [NPCID.DyeTrader] = () => true,
        [NPCID.ArmsDealer] = () => true,
        [NPCID.Dryad] = () => NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3,
        [NPCID.Painter] = () => CountTownNpcs() >= 8,
        [NPCID.GoblinTinkerer] = () => NPC.savedGoblin,
        [NPCID.WitchDoctor] = () => NPC.downedQueenBee,
        [NPCID.Clothier] = () => NPC.downedBoss3,
        [NPCID.Mechanic] = () => NPC.savedMech,
        [NPCID.PartyGirl] = () => CountTownNpcs() >= 14,
        [NPCID.Wizard] = () => NPC.savedWizard,
        [NPCID.TaxCollector] = () => NPC.savedTaxCollector,
        [NPCID.Pirate] = () => NPC.downedPirates,
        [NPCID.Steampunker] = () => NPC.downedMechBossAny,
        [NPCID.Cyborg] = () => NPC.downedPlantBoss,
        [NPCID.Angler] = () => NPC.savedAngler,
        [NPCID.Stylist] = () => NPC.savedStylist,
        [NPCID.Golfer] = () => NPC.savedGolfer,
        [NPCID.DD2Bartender] = () => NPC.savedBartender,
        [NPCID.SantaClaus] = () => NPC.downedFrost && Main.xMas,
    };

    private static int CountTownNpcs() =>
        Main.npc.Count(n => n != null && n.active && n.townNPC);

    private static HashSet<int> GetExistingTypes() =>
        Main.npc.Where(n => n != null && n.active && n.townNPC)
            .Select(n => n.type).ToHashSet();

    internal static void Summon(CommandArgs args)
    {
        HashSet<int> existing = GetExistingTypes();
        int spawned = 0, present = 0, locked = 0, failed = 0;

        foreach (var (type, check) in Candidates)
        {
            if (existing.Contains(type))
            {
                present++;
                continue;
            }

            if (!check())
            {
                locked++;
                continue;
            }

            int index = NPC.NewNPC(
                new EntitySource_DebugCommand(),
                Main.spawnTileX * 16,
                (Main.spawnTileY - 5) * 16 - spawned * 32,
                type);

            if (index < 0 || index >= Main.maxNPCs ||
                Main.npc[index] == null || !Main.npc[index].active ||
                Main.npc[index].type != type)
            {
                failed++;
                continue;
            }

            NPC npc = Main.npc[index];
            npc.velocity = Microsoft.Xna.Framework.Vector2.Zero;
            npc.netUpdate = true;
            npc.netSpam = 0;
            TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", index);
            existing.Add(type);
            spawned++;
        }

        args.Player.SendSuccessMessage(
            $"NPCs spawned: {spawned}; already present: {present}; progression locked: {locked}; failed: {failed}.");
    }

    internal static void Status(CommandArgs args)
    {
        HashSet<int> existing = GetExistingTypes();
        var available = new List<string>();
        var locked = new List<string>();

        foreach (var (type, check) in Candidates)
        {
            string name = Lang.GetNPCNameValue(type);
            if (existing.Contains(type))
                available.Add(name + " (present)");
            else if (check())
                available.Add(name);
            else
                locked.Add(name);
        }

        args.Player.SendInfoMessage($"Known NPC types: {Candidates.Count}; active town NPCs: {CountTownNpcs()}.");
        args.Player.SendInfoMessage("Present/eligible: " + string.Join(", ", available));
        if (locked.Count != 0)
            args.Player.SendInfoMessage("Progression-locked: " + string.Join(", ", locked));
    }
}
