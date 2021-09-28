using System;

namespace Detekonai.Networking
{
	public interface INetworkSerializerFactory
	{
		INetworkSerializer Build(Type type);
	}
}
