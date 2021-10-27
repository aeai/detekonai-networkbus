using Detekonai.Core;
using System;
using System.Collections.Generic;

namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializerFactory : INetworkSerializerFactory
	{
		private ITypeConverterRepository converter = new TypeConverterRepository();
		private Dictionary<Type, INetworkSerializer> serializers = new Dictionary<Type, INetworkSerializer>();

		public void SetCustomSerializer(Type type, INetworkSerializer serializer)
        {
			serializers[type] = serializer;
        }

		public INetworkSerializer Build(Type type)
		{
			if(!serializers.TryGetValue(type, out INetworkSerializer ser))
            {
				ser = new DefaultSerializer(type, converter, this);
				serializers[type] = ser;
			}
			return ser;
		}
	}
}
