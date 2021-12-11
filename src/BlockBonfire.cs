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
                    case "construct4":
                        return 4;
                }
                return 5;
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
                        return "construct4";
                    case "construct4":
                        return "cold";
                }
                return "cold";
            }
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            int stage = Stage;
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            if (stage < 5 && stack?.Collectible.Attributes?.IsTrue("firepitConstructable") == true)
            {
                BlockPos pos = blockSel.Position;
                Block block = world.GetBlock(CodeWithParts(NextStageCodePart));
                world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
                world.BlockAccessor.MarkBlockDirty(pos);
                if (block.Sounds != null) world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, byPlayer);
                if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                }
                return true;
            }

            return false;
        }

    }
}