using Detekonai.Core;
using System;
using System.Collections.Generic;

namespace Detekonai.Networking
{
	public interface INetworkSerializerFactory
	{
		INetworkSerializer Get(Type type);
		INetworkSerializer Get(uint id);
		IEnumerable<INetworkSerializer> Serializers { get; }
	}
}
