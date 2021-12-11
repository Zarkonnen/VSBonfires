using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Bonfires
{
    public class BlockBonfire : Block
    {
        public int Stage { get {
            switch (LastCodePart())
                {
                    case "construct1":
                        return 1;
                    case "construct2":
                        return 2;
                    case "construct3":
                        return 3;
                }
                return 4;
        } }

        public string NextStageCodePart
        {
            get
            {
                switch (LastCodePart())
                {
                    case "construct1":
                        return "construct2";
                    case "construct2":
                        return "construct3";
                    case "construct3":
                        return "cold";
                }
                return "cold";
            }
        }

        public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            BlockEntityBonfire bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBonfire;
            if (bef != null && bef.Burning) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            BlockEntityBonfire bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBonfire;
            if (bef != null && !bef.Burning)
            {
                bef.ignite((byEntity as EntityPlayer)?.PlayerUID);
            }

            handling = EnumHandling.PreventDefault;
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int stage = Stage;
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            if (stage < 4 && stack?.Collectible.Attributes?.IsTrue("firepitConstructable") == true && byPlayer.InventoryManager.ActiveHotbarSlot.StackSize >= 8)
            {
                BlockPos pos = blockSel.Position;
                Block block = world.GetBlock(CodeWithParts(NextStageCodePart));
                world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
                world.BlockAccessor.MarkBlockDirty(pos);
                if (block.Sounds != null) world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, byPlayer);
                if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(8);
                }
                return true;
            }

            return false;
        }

    }
}