using Detekonai.Core;
using Detekonai.Core.Common;
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

		public uint ObjectId { get; private set; }

		public Type SerializedType { get; private set; }

		public int RequiredSize { get; private set; } = 0;

		public DefaultSerializer(Type type, ITypeConverterRepository typeConverterRepo, INetworkSerializerFactory factory)
		{
			typeRepo = typeConverterRepo;
			SerializedType = type;
			Initialize(type, factory);
		}

		private void Initialize(Type t, INetworkSerializerFactory serFactory)
		{
			var props = t.GetProperties().Where(p => p.IsDefined(typeof(NetworkSerializablePropertyAttribute))).OrderBy(x => x.GetCustomAttribute<NetworkSerializablePropertyAttribute>().Name);
			byte[] data = new byte[512];
			NetworkSerializableAttribute nab = t.GetCustomAttribute<NetworkSerializableAttribute>();
			string fn = t.Name;
			RequiredSize = nab.SizeRequirement;
			fn = nab.Name != null ? nab.Name : t.Name;
			uint len = (uint)System.Text.Encoding.UTF8.GetBytes(fn, 0, fn.Length, data, 0);
			ObjectId = MurmurHash3.Hash(data, len, 19850922);

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

				(Type,Type) dicType = GetDictionaryNetworkSerializableType(getter.ReturnType);
				if (dicType.Item1 != null && dicType.Item2 != null)
				{
					var ser = typeof(DictionaryPropertySerializer<,,,>).MakeGenericType(t, getter.ReturnType, dicType.Item1, dicType.Item2);
					serializers.Add((IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, serFactory.Build(dicType.Item1), serFactory.Build(dicType.Item2) }));
				}
				else
				{
					Type colType = GetCollectionNetworkSerializableType(getter.ReturnType);
					if (colType != null)
					{
						var ser = typeof(EnumerablePropertySerializer<,,>).MakeGenericType(t, getter.ReturnType, colType);
						INetworkSerializer pser = serFactory.Build(colType);
						serializers.Add((IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, pser }));
					}
					else
					{
						IPropertySerializer ser = FindSerializer(t, getter.ReturnType, getterDelegate, setterDelegate, serFactory);
						if (ser != null)
						{
							serializers.Add(ser);
						}
					}
				}
			}
		}

		private static (Type,Type) GetDictionaryNetworkSerializableType(Type type)
		{
			if (typeof(IDictionary).IsAssignableFrom(type))
			{
				Type[] tp = type.GenericTypeArguments;
				if (tp.Length == 2)
				{
					return (tp[0],tp[1]);
				}
			}
			return (null,null);
		}

		private static Type GetCollectionNetworkSerializableType(Type type)
		{
			if (typeof(ICollection).IsAssignableFrom(type))
			{
				Type[] tp = type.GenericTypeArguments;
				if (tp.Length == 1 && tp[0].GetCustomAttribute<NetworkSerializableAttribute>() != null)
				{
					return tp[0];
				}
			}
			return null;
		}

		private IPropertySerializer FindSerializer(Type ownerType, Type type, Delegate getterDelegate, Delegate setterDelegate, INetworkSerializerFactory serFactory) 
		{
			if(type == typeof(object))
            {
				var ser = typeof(RawPropertySerializer<>).MakeGenericType(ownerType);
				return (IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, typeRepo });
			}else if(type == typeof(object[]))
			{
				var ser = typeof(RawArraySerializer<>).MakeGenericType(ownerType);
				return (IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, typeRepo });
			}
			NetworkSerializableAttribute gnab = type.GetCustomAttribute<NetworkSerializableAttribute>();
			if (gnab == null)
			{
				var ser = typeof(PropertySerializer<,>).MakeGenericType(ownerType, type);
				if (typeRepo.TryGetConverter(type, out STypeConverter conv))
				{
					return (IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, conv.writer, conv.reader });
				}
			}
			else
			{
				var ser = typeof(NetworkEventPropertySerializer<,>).MakeGenericType(ownerType, type);
				INetworkSerializer pser = serFactory.Build(type);
				return (IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, pser });
			}
			return null;
		}

		public object Deserialize(BinaryBlob blob)
		{
			object ob = factory();
			foreach(IPropertySerializer d in serializers)
			{
				d.Deserialize(ob, blob);
			}
			return ob;
		}

		public void Serialize(BinaryBlob blob, object ob)
		{
			foreach(IPropertySerializer d in serializers)
			{
				d.Serialize(ob, blob);
			}
		}
	}
}
