using Unity.NetCode;

[UnityEngine.Scripting.Preserve]
public class AutoConnectBootstrap: ClientServerBootstrap
{
	public override bool Initialize(string defaultWorldName)
   {
		AutoConnectPort = 7979;
    	base.Initialize(defaultWorldName);
    	ServerWorld?.EntityManager.CreateSingleton(new ClientServerTickRate{ SimulationTickRate = 30 });
		return true;
   }
}