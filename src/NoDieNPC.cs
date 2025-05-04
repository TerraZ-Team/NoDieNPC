using Terraria;
using TerrariaApi.Server;

namespace NoDieNPC
{
    [ApiVersion(2, 1)]
    public class NoDieNPC : TerrariaPlugin {
        public override string Name => "NoDieNPC";
        public override Version Version => new (1, 0, 0);
        public override string Author => "iBelarus";
        public override string Description => "The plugin makes towny NPCs immortal.";

        public NoDieNPC(Main game) : base(game) { }

        public override void Initialize() {
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
        }

        private void OnNpcStrike(NpcStrikeEventArgs args) {
            if (args.Npc.townNPC)
                args.Handled = true;
        }

        private void OnPostInitialize(EventArgs args) {
            foreach (var npc in Main.npc)
            {
                if (npc != null && npc.active &&  npc.townNPC)
                {
                    npc.lavaImmune = true;
                    npc.immortal = true;
                    npc.defense = int.MaxValue;
                    npc.life = npc.lifeMax;
                }
            }
        }

        private void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            NPC npc = Main.npc[args.NpcId];
            if (npc != null && npc.townNPC)
            {
                npc.lavaImmune = true;
                npc.immortal = true;
                npc.defense = int.MaxValue;
                npc.life = npc.lifeMax;
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NpcSpawn.Deregister(this, OnNpcSpawn);
            }
            base.Dispose(disposing);
        }
    }
}