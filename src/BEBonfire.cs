using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Bonfires
{
    public class BlockEntityBonfire : BlockEntity, IHeatSource
    {
        public double BurningUntilTotalHours;
        public float BurnTimeHours = 4;
        public bool Burning = false;
        Block fireBlock;
        public string startedByPlayerUid;
        ILoadedSound ambientSound;
        long listener;

        static Cuboidf fireCuboid = new Cuboidf(-0.35f, 0, -0.35f, 1.35f, 2.8f, 1.35f);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            fireBlock = Api.World.GetBlock(new AssetLocation("fire"));
            if (fireBlock == null) fireBlock = new Block();

            if (Burning)
            {
                BurningUntilTotalHours = Math.Min(api.World.Calendar.TotalHours + BurnTimeHours, BurningUntilTotalHours);
                initSoundsAndTicking();
            }
        }

        private void initSoundsAndTicking()
        {
            listener = RegisterGameTickListener(OnceASecond, 1000);
            if (ambientSound == null && Api.Side == EnumAppSide.Client)
            {
                ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game:sounds/environment/fire.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 2f
                });

                if (ambientSound != null)
                {
                    System.Console.WriteLine("sound nao");
                    ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
                    ambientSound.Start();
                }
            }
        }

        private void OnceASecond(float dt)
        {
            //System.Console.WriteLine("sec");
            if (Api is ICoreClientAPI)
            {
                return;
            }
            if (Burning)
            {
                Entity[] entities = Api.World.GetEntitiesAround(Pos.ToVec3d().Add(0.5, 0.5, 0.5), 3, 3, (e) => true);
                Vec3d ownPos = Pos.ToVec3d();
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!CollisionTester.AabbIntersect(entity.CollisionBox, entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z, fireCuboid, ownPos)) continue;

                    if (entity.Alive)
                    {
                        entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = fireBlock, SourcePos = ownPos, Type = EnumDamageType.Fire }, 2f);
                    }

                    if (Api.World.Rand.NextDouble() < 0.125)
                    {
                        entity.Ignite();
                    }
                }

                if (Api.World.BlockAccessor.GetBlock(Pos).LiquidCode == "water")
                {
                    killFire();
                    return;
                }
                if (((ICoreServerAPI)Api).Server.Config.AllowFireSpread && 0.2 > Api.World.Rand.NextDouble())
                {
                    TrySpreadFireAllDirs();
                }
                //System.Console.WriteLine(Api.World.Calendar.TotalHours + " vs " +  BurningUntilTotalHours);
                if (Api.World.Calendar.TotalHours >= BurningUntilTotalHours)
                {
                    killFire();
                    // See if we want to crack the blocks around us.
                    foreach (BlockFacing facing in BlockFacing.ALLFACES)
                    {
                        BlockPos npos = Pos.AddCopy(facing);
                        Block belowBlock = Api.World.BlockAccessor.GetBlock(npos);
                        AssetLocation cracked = null;
                        if (belowBlock.FirstCodePart().Equals("ore"))
                        {
                            cracked = belowBlock.CodeWithPart("cracked_ore");
                            cracked.Domain = "bonfires";
                        }
                        else if (belowBlock.FirstCodePart().Equals("rock"))
                        {
                            cracked = belowBlock.CodeWithPart("cracked_rock");
                            cracked.Domain = "bonfires";
                        }
                        if (cracked != null && cracked.Valid)
                        {
                            //System.Console.WriteLine("adj block is " + belowBlock.Code.ToString() + " becomes " + cracked);
                            Block crackedBlock = Api.World.GetBlock(cracked);
                            Api.World.BlockAccessor.ExchangeBlock(crackedBlock.Id, npos);
                        }
                    }
                    
                }
            }
        }

        public void setBlockState(string state)
        {
            AssetLocation loc = Block.CodeWithVariant("burnstate", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;
        }

        public void ignite(string playerUid)
        {
            startedByPlayerUid = playerUid;
            BurningUntilTotalHours = Api.World.Calendar.TotalHours + BurnTimeHours;
            Burning = true;
            setBlockState("lit");
            MarkDirty(true);
            initSoundsAndTicking();
        }

        public void killFire()
        {
            setBlockState("extinct");
            Burning = false;
            ambientSound?.FadeOutAndStop(1);
            UnregisterGameTickListener(listener);
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return Burning ? 30 : 1;
        }

        protected void TrySpreadFireAllDirs()
        {
            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                BlockPos npos = Pos.AddCopy(facing);
                TrySpreadTo(npos);
            }
            for (int up = 2; up <= 5; up++)
            {
                BlockPos npos = Pos.UpCopy(up);
                TrySpreadTo(npos);
            }
        }

        public bool TrySpreadTo(BlockPos pos)
        {
            // 1. Replaceable test
            var block = Api.World.BlockAccessor.GetBlock(pos);
            if (block.Replaceable < 6000) return false;

            BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
            if (be?.GetBehavior<BEBehaviorBurning>() != null) return false;

            // 2. fuel test
            bool hasFuel = false;
            BlockPos npos = null;
            foreach (BlockFacing firefacing in BlockFacing.ALLFACES)
            {
                npos = pos.AddCopy(firefacing);
                block = Api.World.BlockAccessor.GetBlock(npos);
                if (canBurn(npos) && Api.World.BlockAccessor.GetBlockEntity(npos)?.GetBehavior<BEBehaviorBurning>() == null) {
                    hasFuel = true; 
                    break; 
                }
            }
            if (!hasFuel) return false;

            // 3. Land claim test
            IPlayer player = Api.World.PlayerByUid(startedByPlayerUid);            
            if (player != null && Api.World.Claims.TestAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak) != EnumWorldAccessResponse.Granted) {
                return false;
            }

            Api.World.BlockAccessor.SetBlock(fireBlock.BlockId, pos);

            //Api.World.Logger.Error(string.Format("Fire @{0}: Spread to {1}.", Pos, pos));

            BlockEntity befire = Api.World.BlockAccessor.GetBlockEntity(pos);
            befire.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(pos, npos, startedByPlayerUid);

            return true;
        }

        protected bool canBurn(BlockPos pos)
        {
            return 
                OnCanBurn(pos) 
                && Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(pos) != true
            ;
        }

        protected bool OnCanBurn(BlockPos pos)
        {
            Block block = Api.World.BlockAccessor.GetBlock(pos);
            return block?.CombustibleProps != null && block.CombustibleProps.BurnDuration > 0;
        }
    }
}