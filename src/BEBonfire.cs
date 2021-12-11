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
        public float accumTime;
        public bool burning = true;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnceASecond, 1000);
            System.Console.WriteLine("init");
        }

        private void OnceASecond(float dt)
        {
            System.Console.WriteLine("sec");
            if (Api is ICoreClientAPI)
            {
                return;
            }
            if (burning)
            {
                accumTime += dt;
                System.Console.WriteLine("burn " + accumTime);
                if (accumTime > 5)
                {
                    setBlockState("extinct");
                    burning = false;
                    System.Console.WriteLine("extinguish");
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

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return burning ? 30 : 1;
        }
    }
}