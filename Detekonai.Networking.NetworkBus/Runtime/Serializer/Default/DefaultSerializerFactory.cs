using Detekonai.Core;
using System;
using System.Collections.Generic;

namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializerFactory : INetworkSerializerFactory
	{
		private TypeConverterRepository converter = new TypeConverterRepository();
		private Dictionary<Type, INetworkSerializer> serializers = new Dictionary<Type, INetworkSerializer>();

		public void AddCustomConverter<T>(Action<BinaryBlob, T> writer, Func<BinaryBlob, T> reader)
		{
			converter.AddConverter<T>(writer, reader);
		}

		public INetworkSerializer Build(Type type)
		{
			if(!serializers.TryGetValue(type, out INetworkSerializer ser))
            {
				if(converter.TryGetConverter(type, out STypeConverter conv))
                {
					ser = new PrimitiveSerializer(type, conv);
                }
				else
                {
					ser = new DefaultSerializer(type, converter, this);
                }
				serializers[type] = ser;
			}
			return ser;
		}

        public INetworkSerializer Get(Type type)
        {
			if(serializers.TryGetValue(type, out INetworkSerializer ser))
            {
				return ser;
            }
			return null;
		}
    }
}
