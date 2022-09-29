using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Detekonai.Networking.Serializer
{
	public struct STypeConverter
	{
		public Delegate writer;
		public Delegate reader;
		public Action<BinaryBlob, object> rawWriter;
		public Func<BinaryBlob, object> rawReader;
		public ushort id;
	}

	public interface ITypeConverterRepository
	{
		bool TryGetConverter(Type type, out STypeConverter del);
		bool TryGetConverter(int typeId, out STypeConverter del);
	}

	public sealed class TypeConverterRepository : ITypeConverterRepository
	{
		private Dictionary<Type, STypeConverter> converters = new Dictionary<Type, STypeConverter>();
		private List<Type> typeList = new List<Type>();
		public bool TryGetConverter(Type type, out STypeConverter del)
		{
			return converters.TryGetValue(type, out del);
		}
		public bool TryGetConverter(int typeId, out STypeConverter del)
		{
			if(typeId >= 0 && typeId < typeList.Count)
            {
				return converters.TryGetValue(typeList[typeId], out del);
            }
			del = default;
			return false;
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
			RegisterListConverter(blob.AddString, blob.ReadString);
			RegisterListConverter(blob.AddInt, blob.ReadInt);
			RegisterListConverter(blob.AddUInt, blob.ReadUInt);
			RegisterListConverter(blob.AddLong, blob.ReadLong);
			RegisterListConverter(blob.AddULong, blob.ReadULong);
			RegisterListConverter(blob.AddByte, blob.ReadByte);
			RegisterListConverter(blob.AddShort, blob.ReadShort);
			RegisterListConverter(blob.AddUShort, blob.ReadUShort);
			RegisterListConverter(blob.AddSingle, blob.ReadSingle);
			RegisterTimeSpan();
			RegisterTimeOffset();
		}
		private void RegisterListConverter<T>(Action<T> setFunc, Func<T> getFunc)
		{
			var writerType = typeof(Action<,>).MakeGenericType(typeof(BinaryBlob), typeof(T));
			var readerType = typeof(Func<,>).MakeGenericType(typeof(BinaryBlob), typeof(T));
			Action<BinaryBlob, T> writerDelegate = (Action<BinaryBlob, T>)Delegate.CreateDelegate(writerType, null, setFunc.Method);
			Func<BinaryBlob, T> readerDelegate = (Func<BinaryBlob, T>)Delegate.CreateDelegate(readerType, null, getFunc.Method);

			Action<BinaryBlob, List<T>> w = (BinaryBlob b, List<T> list) =>
			 {
				 b.AddUShort((ushort)list.Count);
				 foreach(var v in list)
                 {
					 writerDelegate(b, v);
                 }
			 };
			Func<BinaryBlob, List<T>> r = (BinaryBlob b) =>
			{
				List<T> res = new List<T>();
				ushort count = b.ReadUShort();
				for(int i = 0; i < count; i++)
				{
					res.Add(readerDelegate(b));
				}
				return res;
			};
			converters[typeof(List<T>)] = new STypeConverter
			{
				writer = w,
				reader = r,
				id = (ushort)typeList.Count,
			rawWriter = (BinaryBlob blob, object ob) => {
					((Action<BinaryBlob, List<T>>)w).Invoke(blob, (List<T>)ob);
				},
				rawReader = (BinaryBlob blob) => { return ((Func<BinaryBlob, List<T>>)r).Invoke(blob); },
			};
			typeList.Add(typeof(List<T>));
		}

		private void RegisterSimpleConverter<T>(Action<T> setFunc, Func<T> getFunc)
		{
			var writerType = typeof(Action<,>).MakeGenericType(typeof(BinaryBlob), typeof(T));
			var readerType = typeof(Func<,>).MakeGenericType(typeof(BinaryBlob), typeof(T));
			var wDelegate = Delegate.CreateDelegate(writerType, null, setFunc.Method);
			var rDelegate = Delegate.CreateDelegate(readerType, null, getFunc.Method);
			converters[typeof(T)] = new STypeConverter
			{
				writer = wDelegate,
				reader = rDelegate,
				id = (ushort)typeList.Count,
				rawWriter = (BinaryBlob blob, object ob) => {
					((Action<BinaryBlob, T>)wDelegate).Invoke(blob, (T)ob);
				},
				rawReader = (BinaryBlob blob) => { return ((Func<BinaryBlob, T>)rDelegate).Invoke(blob); },
			};
			typeList.Add(typeof(T));
		}

		private void RegisterTimeSpan()
		{
			Action<BinaryBlob, TimeSpan > w = (BinaryBlob blob, TimeSpan sp) => {
				blob.AddLong(sp.Ticks);
			};
			Func<BinaryBlob, TimeSpan> r = (BinaryBlob blob) => {
				return TimeSpan.FromTicks(blob.ReadLong());
			};

			converters[typeof(TimeSpan)] = new STypeConverter
			{
				writer = w,
				reader = r,
				id = (ushort)typeList.Count,
				rawWriter = (BinaryBlob blob, object ob) => {
					w.Invoke(blob, (TimeSpan)ob);
				},
				rawReader = (BinaryBlob blob) => { return r.Invoke(blob); },
			};
			typeList.Add(typeof(TimeSpan));
		}

		private void RegisterTimeOffset()
		{
			Action<BinaryBlob, DateTimeOffset> w = (BinaryBlob blob, DateTimeOffset sp) => {
				blob.AddLong(sp.UtcTicks);
			};
			Func<BinaryBlob, DateTimeOffset> r = (BinaryBlob blob) => {
				return new DateTimeOffset(blob.ReadLong(), TimeSpan.Zero);
			};

			converters[typeof(DateTimeOffset)] = new STypeConverter
			{
				writer = w,
				reader = r,
				id = (ushort)typeList.Count,
				rawWriter = (BinaryBlob blob, object ob) => {
					w.Invoke(blob, (DateTimeOffset)ob);
				},
				rawReader = (BinaryBlob blob) => { return r.Invoke(blob); },
			};
			typeList.Add(typeof(DateTimeOffset));
		}
	}
}
