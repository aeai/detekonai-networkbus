using Detekonai.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializer : INetworkSerializer
	{
		private List<IPropertySerializer> serializers = new List<IPropertySerializer>();
		private ITypeConverterRepository typeRepo;
		private Func<object> factory;
		private uint hash;

		public uint MessageId => hash;

		public Type SerializedType { get; private set; }

        public int RequiredSize { get; private set; }

		public DefaultSerializer(Type type, ITypeConverterRepository typeConverterRepo)
		{
			typeRepo = typeConverterRepo;
			SerializedType = type;
			Initialize(type);
		}

		private void Initialize(Type t)
		{

			var props = t.GetProperties().Where(p => p.IsDefined(typeof(NetworkSerializableAttribute))).OrderBy(x => x.GetCustomAttribute<NetworkSerializableAttribute>().Name);
			byte[] data = new byte[512];
			NetworkEventAttribute nab = t.GetCustomAttribute<NetworkEventAttribute>();
			RequiredSize = nab.SizeRequirement;
			string fn = nab.Name != null ? nab.Name : t.Name;
			uint len = (uint)System.Text.Encoding.UTF8.GetBytes(fn, 0, fn.Length, data, 0);
			hash = MurmurHash3.Hash(data, len, 19850922);

			factory = Expression.Lambda<Func<System.Object>>(
				Expression.New(t.GetConstructor(Type.EmptyTypes))
			).Compile();

			foreach(PropertyInfo prop in props)
			{
				MethodInfo getter = prop.GetGetMethod();
				MethodInfo setter = prop.GetSetMethod();
				var getterType = typeof(Func<,>).MakeGenericType(t, getter.ReturnType);
				var setterType = typeof(Action<,>).MakeGenericType(t, getter.ReturnType);
				var getterDelegate = Delegate.CreateDelegate(getterType, null, getter);
				var setterDelegate = Delegate.CreateDelegate(setterType, null, setter);

				var ser = typeof(PropertySerializer<,>).MakeGenericType(t, getter.ReturnType);
				if(typeRepo.TryGetConverter(getter.ReturnType, out STypeConverter conv))
				{
					serializers.Add((IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, conv.writer, conv.reader }));
				}
			}
		}

		public BaseMessage Deserialize(BinaryBlob blob)
		{
			System.Object ob = factory();
			foreach(IPropertySerializer d in serializers)
			{
				d.Deserialize(ob, blob);
			}
			return (BaseMessage)ob;
		}

		public void Serialize(BinaryBlob blob, BaseMessage ob)
		{
			blob.AddUInt(hash);
			foreach(IPropertySerializer d in serializers)
			{
				d.Serialize(ob, blob);
			}
		}
	}
}
