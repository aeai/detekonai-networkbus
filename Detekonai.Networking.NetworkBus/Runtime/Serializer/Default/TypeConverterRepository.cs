using Detekonai.Core;
using System;
using System.Collections.Generic;

namespace Detekonai.Networking.Serializer
{
	public struct STypeConverter
	{
		public Delegate writer;
		public Delegate reader;
	}

	public interface ITypeConverterRepository
	{
		bool TryGetConverter(Type type, out STypeConverter del);
	}

	public sealed class TypeConverterRepository : ITypeConverterRepository
	{
		private Dictionary<Type, STypeConverter> converters = new Dictionary<Type, STypeConverter>();

		public bool TryGetConverter(Type type, out STypeConverter del)
		{
			return converters.TryGetValue(type, out del);
		}

		public void AddConverter(Type type, Delegate writer, Delegate reader)
		{
			converters[type] = new STypeConverter { writer = writer, reader = reader };
		}

		public TypeConverterRepository()
		{
			//using a real binary blob here so we can extract methodinfo without using the GetMethod function which would not work in an obfuscated code
			BinaryBlobPool tempPool = new BinaryBlobPool(1, 1);
			BinaryBlob blob = new BinaryBlob(tempPool);
			blob.Release();

			RegisterSimpleConverter(blob.AddString, blob.ReadString);
			RegisterSimpleConverter(blob.AddInt, blob.ReadInt);
			RegisterSimpleConverter(blob.AddUInt, blob.ReadUInt);
			RegisterSimpleConverter(blob.AddLong, blob.ReadLong);
			RegisterSimpleConverter(blob.AddULong, blob.ReadULong);
			RegisterSimpleConverter(blob.AddByte, blob.ReadByte);
			RegisterSimpleConverter(blob.AddShort, blob.ReadShort);
			RegisterSimpleConverter(blob.AddUShort, blob.ReadUShort);
			RegisterSimpleConverter(blob.AddSingle, blob.ReadSingle);
		}

		private void RegisterSimpleConverter<T>(Action<T> setFunc, Func<T> getFunc)
		{
			var writerType = typeof(Action<,>).MakeGenericType(typeof(BinaryBlob), typeof(T));
			var readerType = typeof(Func<,>).MakeGenericType(typeof(BinaryBlob), typeof(T));
			converters[typeof(T)] = new STypeConverter
			{
				writer = Delegate.CreateDelegate(writerType, null, setFunc.Method),
				reader = Delegate.CreateDelegate(readerType, null, getFunc.Method),
			};
		}
	}
}
