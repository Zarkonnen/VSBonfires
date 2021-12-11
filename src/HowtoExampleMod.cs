using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo( "Bonfires",
	Description = "Adds Bonfires",
	Website     = "https://github.com/copygirl/howto-example-mod",
	Authors     = new []{ "Zarkonnen" } )]

namespace Bonfires
{
	public class BonfiresMod : ModSystem
	{
		public override void Start(ICoreAPI api)
		{
			//api.RegisterBlockBehaviorClass(InstaTNTBehavior.NAME, typeof(InstaTNTBehavior));
			api.RegisterBlockEntityClass("BlockEntityBonfire", typeof(BlockEntityBonfire));
			api.RegisterBlockClass("BlockBonfire", typeof(BlockBonfire));
		}
		
		public override void StartClientSide(ICoreClientAPI api)
		{
			
		}
		
		public override void StartServerSide(ICoreServerAPI api)
		{
			
		}
	}
}
