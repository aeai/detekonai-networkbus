using Detekonai.Core;
using System;

namespace Detekonai.Networking
{
	public interface INetworkSerializerFactory
	{
		INetworkSerializer Get(Type type);
		INetworkSerializer Get(uint id);
		INetworkSerializer Build(Type type);
	}
}
