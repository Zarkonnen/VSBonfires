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
        WorldInteraction[] interactions;
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

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            interactions = ObjectCacheUtil.GetOrCreate(api, "bonfireInteractions-"+Stage, () =>
            {
                List<ItemStack> canIgniteStacks = new List<ItemStack>();
                List<ItemStack> fuelStacks = new List<ItemStack>();

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    //string firstCodePart = obj.FirstCodePart();

                    if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>() || obj is ItemFirestarter)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(api as ICoreClientAPI);
                        if (stacks != null) canIgniteStacks.AddRange(stacks);
                    }
                    if (obj is Item && (obj as Item).Code.Path.Equals("firewood") == true)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(api as ICoreClientAPI);
                        if (stacks != null) fuelStacks.AddRange(stacks);
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-ignite",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            Block bf = api.World.BlockAccessor.GetBlock(bs.Position);
                            if (bf.LastCodePart().Equals("cold"))
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "bonfires:blockhelp-bonfire-fuel",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = fuelStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            Block bf = api.World.BlockAccessor.GetBlock(bs.Position);
                            if (bf.LastCodePart().StartsWith("construct"))
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
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

            if (stage < 4 && stack?.Collectible.Code.Path.Equals("firewood") == true && byPlayer.InventoryManager.ActiveHotbarSlot.StackSize >= 8)
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