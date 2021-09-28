using Detekonai.Core;
using System;
using System.Collections.Generic;

namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializerFactory : INetworkSerializerFactory
	{
		private ITypeConverterRepository converter = new TypeConverterRepository();
		private Dictionary<Type, INetworkSerializer> customSerializers = new Dictionary<Type, INetworkSerializer>();

		public void SetCustomSerializer(Type type, INetworkSerializer serializer)
        {
			customSerializers[type] = serializer;
        }

		public INetworkSerializer Build(Type type)
		{
			if(customSerializers.TryGetValue(type, out INetworkSerializer ser))
            {
				return ser;
            }
			return new DefaultSerializer(type, converter);
		}
	}
}
