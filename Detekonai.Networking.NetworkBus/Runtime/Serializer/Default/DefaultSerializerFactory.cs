using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializerFactory : INetworkSerializerFactory
	{
		private TypeConverterRepository converter = new TypeConverterRepository();
		private Dictionary<Type, INetworkSerializer> serializers = new Dictionary<Type, INetworkSerializer>();
		private readonly Dictionary<uint, INetworkSerializer> serializersByHash = new Dictionary<uint, INetworkSerializer>();

        public IEnumerable<INetworkSerializer> Serializers => serializers.Values;

        public void AddCustomConverter<T>(Action<BinaryBlob, T> writer, Func<BinaryBlob, T> reader)
		{
			converter.AddConverter<T>(writer, reader);
			Build(typeof(T));
		}

        //public DefaultSerializerFactory()
        //{
        //    var types = AppDomain.CurrentDomain.GetAssemblies().Where(x => !IsOmittable(x)).SelectMany(s => s.GetTypes()).Where(p => p.GetCustomAttribute<NetworkSerializableAttribute>() != null && !p.IsAbstract).ToList();
        //    foreach (Type t in types)
        //    {
        //        Build(t);
        //    }
        //}
        private static bool IsOmittable(Assembly assembly)
		{
			string assemblyName = assembly.GetName().Name;
			return StartsWith("System") ||
				StartsWith("Microsoft") ||
				StartsWith("Windows") ||
				StartsWith("Unity") ||
				StartsWith("netstandard");

			bool StartsWith(string value) => assemblyName.StartsWith(value, ignoreCase: false, culture: CultureInfo.CurrentCulture);
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
				serializersByHash[ser.ObjectId] = ser;
			}
			return ser;
		}

        public INetworkSerializer Get(Type type)
        {
			if(serializers.TryGetValue(type, out INetworkSerializer ser))
            {
				return ser;
            }
			return Build(type) ;
		}

        public INetworkSerializer Get(uint id)
        {
			serializersByHash.TryGetValue(id, out INetworkSerializer res);
			return res;
		}
    }
}
