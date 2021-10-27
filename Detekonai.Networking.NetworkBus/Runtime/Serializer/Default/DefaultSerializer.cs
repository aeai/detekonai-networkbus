﻿using Detekonai.Core;
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

		public uint MessageId { get; private set; }

		public Type SerializedType { get; private set; }

        public int RequiredSize { get; private set; }

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
			RequiredSize = nab.SizeRequirement;
			string fn = nab.Name != null ? nab.Name : t.Name;
			uint len = (uint)System.Text.Encoding.UTF8.GetBytes(fn, 0, fn.Length, data, 0);
			MessageId = MurmurHash3.Hash(data, len, 19850922);

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

				NetworkSerializableAttribute gnab = getter.ReturnType.GetCustomAttribute<NetworkSerializableAttribute>();
				if (gnab == null)
				{
					var ser = typeof(PropertySerializer<,>).MakeGenericType(t, getter.ReturnType);
					if (typeRepo.TryGetConverter(getter.ReturnType, out STypeConverter conv))
					{
						serializers.Add((IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, conv.writer, conv.reader }));
					}
				}
				else
                {
					var ser = typeof(NetworkEventPropertySerializer<,>).MakeGenericType(t, getter.ReturnType);
					INetworkSerializer pser = serFactory.Build(getter.ReturnType);
					serializers.Add((IPropertySerializer)Activator.CreateInstance(ser, new object[] { getterDelegate, setterDelegate, pser }));
                }
			}
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
