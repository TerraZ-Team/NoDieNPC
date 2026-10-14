using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace NoDieNPC;

[ApiVersion(2, 1)]
public sealed class NoDieNPC : TerrariaPlugin
{
    public override string Name => "NoDieNPC";
    public override Version Version => new(1, 1, 0);
    public override string Author => "iBelarus; TerraZ-Team contributors";
    public override string Description => "Event-driven town NPC protection and administration.";

    private readonly Dictionary<int, ProtectionSnapshot> _protectedNpcs = new();
    private readonly string _configPath = Path.Combine(TShock.SavePath, "NoDieNPC.json");
    private NpcConfig _config = new();
    private Command? _command;

    public NoDieNPC(Main game) : base(game) { }

    public override void Initialize()
    {
        _config = NpcConfig.LoadOrDefault(_configPath);
        _command = new Command("npc.use", NpcCommand, "npc")
        {
            HelpText = "Town NPC control: freeze, godmode, home, summon, status."
        };
        Commands.ChatCommands.Add(_command);

        GeneralHooks.ReloadEvent += OnReload;
        ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
        ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
        ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
        ServerApi.Hooks.NpcAIUpdate.Register(this, OnNpcAI);
        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);

        // Some town NPCs may already exist when this plugin initializes.
        ReconcileActiveNpcs();
    }

    private bool ShouldProtect(NPC npc) =>
        _config.GodMode && npc.townNPC &&
        !_config.GodModeExemptNpcTypes.Contains(npc.type);

    private void OnNpcStrike(NpcStrikeEventArgs args)
    {
        if (ShouldProtect(args.Npc))
            args.Handled = true;
    }

    private void OnNpcSpawn(NpcSpawnEventArgs args)
    {
        if (args.NpcId < 0 || args.NpcId >= Main.maxNPCs)
            return;

        NPC npc = Main.npc[args.NpcId];
        if (npc != null)
            ReconcileProtection(npc);
    }

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        // Do not retain state for a dead NPC's slot.
        if (args.npc != null)
            _protectedNpcs.Remove(args.npc.whoAmI);
    }

    private void OnPostInitialize(EventArgs args) => ReconcileActiveNpcs();

    private void OnNpcAI(NpcAiUpdateEventArgs args)
    {
        NPC npc = args.Npc;
        if (npc == null || !npc.active || !npc.townNPC)
            return;

        // Per-NPC AI event, not an O(maxNPCs) scan on every GameUpdate.
        ReconcileProtection(npc);
        if (IsOutsideWorld(npc))
            TeleportToSpawn(npc);

        // Skipping AI is safer than overwriting aiStyle. Shimmer transformations
        // must continue running their original AI.
        if (_config.Freeze && !npc.CurrentlyShimmerTransparent())
        {
            if (npc.velocity != Vector2.Zero)
            {
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }
            args.Handled = true;
        }
    }

    private void ReconcileProtection(NPC npc)
    {
        int slot = npc.whoAmI;
        if (_protectedNpcs.TryGetValue(slot, out var previous) &&
            (!ReferenceEquals(previous.Npc, npc) || previous.Type != npc.type))
        {
            // A slot or NPC type has been reused: never restore the old values
            // onto an unrelated NPC.
            _protectedNpcs.Remove(slot);
        }

        if (!npc.townNPC || !npc.active || !ShouldProtect(npc))
        {
            RestoreProtection(npc);
            return;
        }

        if (!_protectedNpcs.ContainsKey(slot))
        {
            _protectedNpcs[slot] = new ProtectionSnapshot(
                npc, npc.type, npc.lavaImmune, npc.immortal,
                npc.knockBackResist, npc.takenDamageMultiplier);
            npc.netUpdate = true;
        }

        npc.lavaImmune = true;
        npc.immortal = true;
        npc.knockBackResist = 0f;
        npc.takenDamageMultiplier = 0f;

        // Covers non-player/environmental damage without a global tick loop.
        if (npc.life < npc.lifeMax)
        {
            npc.life = npc.lifeMax;
            npc.netUpdate = true;
        }
    }

    private void RestoreProtection(NPC npc)
    {
        int slot = npc.whoAmI;
        if (!_protectedNpcs.Remove(slot, out var saved))
            return;

        if (!ReferenceEquals(saved.Npc, npc) || saved.Type != npc.type)
            return;

        npc.lavaImmune = saved.LavaImmune;
        npc.immortal = saved.Immortal;
        npc.knockBackResist = saved.KnockBackResist;
        npc.takenDamageMultiplier = saved.TakenDamageMultiplier;
        npc.netUpdate = true;
    }

    private void ReconcileActiveNpcs()
    {
        foreach (NPC npc in Main.npc)
        {
            if (npc == null || !npc.active)
                continue;

            ReconcileProtection(npc);
            if (npc.townNPC && _config.Freeze && !npc.CurrentlyShimmerTransparent())
            {
                if (npc.velocity != Vector2.Zero)
                {
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }
        }
    }

    private void RestoreAll()
    {
        foreach (var saved in _protectedNpcs.Values.ToArray())
        {
            NPC npc = saved.Npc;
            if (npc.active && npc.type == saved.Type &&
                npc.whoAmI >= 0 && npc.whoAmI < Main.maxNPCs &&
                ReferenceEquals(Main.npc[npc.whoAmI], npc))
                RestoreProtection(npc);
        }
        _protectedNpcs.Clear();
    }

    private void OnReload(ReloadEventArgs args)
    {
        if (!NpcConfig.TryLoad(_configPath, out var loaded, out string? error))
        {
            args.Player.SendErrorMessage($"NoDieNPC: config reload failed: {error}");
            return;
        }

        bool wasFrozen = _config.Freeze;
        _config = loaded;
        ReconcileActiveNpcs();
        if (wasFrozen != _config.Freeze)
            MarkTownNpcsForUpdate();
        args.Player.SendSuccessMessage("NoDieNPC: configuration reloaded.");
    }

    private static bool IsOutsideWorld(NPC npc) =>
        !float.IsFinite(npc.position.X) || !float.IsFinite(npc.position.Y) ||
        npc.position.X < 0 || npc.position.Y < 0 ||
        npc.position.X > Main.maxTilesX * 16f - npc.width ||
        npc.position.Y > Main.maxTilesY * 16f - npc.height;

    private static void TeleportToSpawn(NPC npc)
    {
        MoveNpc(npc,
            Main.spawnTileX * 16f - npc.width / 2f,
            (Main.spawnTileY - 3) * 16f);
    }

    private static void TeleportHome(NPC npc)
    {
        if (npc.homeless || npc.homeTileX <= 0 || npc.homeTileY <= 0)
            TeleportToSpawn(npc);
        else
            MoveNpc(npc,
                npc.homeTileX * 16f - npc.width / 2f,
                npc.homeTileY * 16f - npc.height);
    }

    private static void MoveNpc(NPC npc, float x, float y)
    {
        npc.position = new Vector2(
            Math.Clamp(x, 16f, Math.Max(16f, Main.maxTilesX * 16f - npc.width - 16f)),
            Math.Clamp(y, 16f, Math.Max(16f, Main.maxTilesY * 16f - npc.height - 16f)));
        npc.velocity = Vector2.Zero;
        npc.oldPosition = npc.position;
        npc.netUpdate = true;
    }

    private static void MarkTownNpcsForUpdate()
    {
        foreach (NPC npc in Main.npc)
            if (npc != null && npc.active && npc.townNPC)
                npc.netUpdate = true;
    }

    private void NpcCommand(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage("/npc freeze: toggle AI freeze");
            args.Player.SendInfoMessage("/npc godmode: toggle invulnerability");
            args.Player.SendInfoMessage("/npc home: teleport NPCs to their homes/spawn");
            args.Player.SendInfoMessage("/npc summon: summon eligible missing town NPCs");
            args.Player.SendInfoMessage("/npc status: list current settings and eligible NPCs");
            return;
        }

        switch (args.Parameters[0].ToLowerInvariant())
        {
            case "freeze":
                _config.Freeze = !_config.Freeze;
                _config.Save(_configPath);
                ReconcileActiveNpcs();
                MarkTownNpcsForUpdate();
                TSPlayer.All.SendInfoMessage($"NPC freeze: {(_config.Freeze ? "enabled" : "disabled")}.");
                break;

            case "godmode":
                _config.GodMode = !_config.GodMode;
                _config.Save(_configPath);
                ReconcileActiveNpcs();
                TSPlayer.All.SendInfoMessage($"NPC invulnerability: {(_config.GodMode ? "enabled" : "disabled")}.");
                break;

            case "home":
                int home = 0, spawn = 0;
                foreach (NPC npc in Main.npc)
                {
                    if (npc == null || !npc.active || !npc.townNPC)
                        continue;

                    if (npc.homeless || npc.homeTileX <= 0 || npc.homeTileY <= 0)
                    {
                        TeleportToSpawn(npc);
                        spawn++;
                    }
                    else
                    {
                        TeleportHome(npc);
                        home++;
                    }
                }
                args.Player.SendSuccessMessage($"NPCs teleported home: {home}, to spawn: {spawn}.");
                break;

            case "summon":
                NpcSummoning.Summon(args);
                break;

            case "status":
                args.Player.SendInfoMessage($"NPC Freeze={_config.Freeze}, GodMode={_config.GodMode}; excluded NPC IDs: {string.Join(", ", _config.GodModeExemptNpcTypes)}");
                NpcSummoning.Status(args);
                break;

            default:
                args.Player.SendErrorMessage("Unknown subcommand. Use /npc for help.");
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            ServerApi.Hooks.NpcAIUpdate.Deregister(this, OnNpcAI);
            ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
            ServerApi.Hooks.NpcSpawn.Deregister(this, OnNpcSpawn);
            ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
            GeneralHooks.ReloadEvent -= OnReload;
            if (_command != null)
                Commands.ChatCommands.Remove(_command);

            RestoreAll();
            MarkTownNpcsForUpdate();
        }
        base.Dispose(disposing);
    }

    private sealed record ProtectionSnapshot(
        NPC Npc, int Type, bool LavaImmune, bool Immortal,
        float KnockBackResist, float TakenDamageMultiplier);
}
