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
    public class BlockEntityBonfire : BlockEntity, IHeatSource
    {
        public double BurningUntilTotalHours;
        public float BurnTimeHours = 0.1F;
        public bool Burning = false;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Burning)
            {
                BurningUntilTotalHours = Math.Min(api.World.Calendar.TotalHours + BurnTimeHours, BurningUntilTotalHours);
            }
            RegisterGameTickListener(OnceASecond, 1000);
            //System.Console.WriteLine("init");
            
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
                //System.Console.WriteLine(Api.World.Calendar.TotalHours + " vs " +  BurningUntilTotalHours);
                if (Api.World.Calendar.TotalHours >= BurningUntilTotalHours)
                {
                    setBlockState("extinct");
                    Burning = false;
                    //System.Console.WriteLine("extinguish");
                    // See if we want to crack the block below us.
                    Block belowBlock = Api.World.BlockAccessor.GetBlock(Pos.DownCopy());
                    System.Console.WriteLine("below block is " + belowBlock.Code.ToString());
                    if (belowBlock.FirstCodePart().Equals("ore"))
                    {
                        AssetLocation crackedOre = belowBlock.CodeWithPart("cracked_ore");
                        crackedOre.Domain = "bonfires";
                        System.Console.WriteLine("cracked ore block is " + crackedOre.ToString() + " valid? " + crackedOre.Valid);
                        if (crackedOre.Valid)
                        {
                            Block crackedBlock = Api.World.GetBlock(crackedOre);
                            Api.World.BlockAccessor.ExchangeBlock(crackedBlock.Id, Pos.DownCopy());
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

        public void ignite()
        {
            BurningUntilTotalHours = Api.World.Calendar.TotalHours + BurnTimeHours;
            Burning = true;
            setBlockState("lit");
            MarkDirty(true);
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return Burning ? 30 : 1;
        }
    }
}