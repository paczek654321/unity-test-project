using Unity.NetCode;

[UnityEngine.Scripting.Preserve]
public class AutoConnectBootstrap: ClientServerBootstrap
{
	public override bool Initialize(string defaultWorldName)
   {
		AutoConnectPort = 7979;
    	base.Initialize(defaultWorldName);
		return true;
   }
}