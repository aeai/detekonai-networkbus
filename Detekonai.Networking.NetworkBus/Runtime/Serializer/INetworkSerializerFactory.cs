using System;

namespace Detekonai.Networking
{
	public interface INetworkSerializerFactory
	{
		INetworkSerializer Get(Type type);
		INetworkSerializer Build(Type type);
	}
}
