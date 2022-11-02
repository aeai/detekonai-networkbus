using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer.Experimental
{
    public static class ObjectPrimitiveSerializerHelper
    {



		private static Dictionary<Type, Action<BinaryBlob, object>> dynamicSerializerMap = new Dictionary<Type, Action<BinaryBlob, object>>(){
			{ typeof(String),   (BinaryBlob blob, object s) => {blob.AddUInt(1); blob.AddString((string)s)	;}},
			{ typeof(int),      (BinaryBlob blob, object s) => {blob.AddUInt(2); blob.AddInt((int)s)		;}},
			{ typeof(uint),     (BinaryBlob blob, object s) => {blob.AddUInt(3); blob.AddUInt((uint)s)		;}},
			{ typeof(long),     (BinaryBlob blob, object s) => {blob.AddUInt(4); blob.AddLong((long)s)		;}},
			{ typeof(ulong),    (BinaryBlob blob, object s) => {blob.AddUInt(5); blob.AddULong((ulong)s)	;}},
			{ typeof(byte),     (BinaryBlob blob, object s) => {blob.AddUInt(6); blob.AddByte((byte)s)		;}},
			{ typeof(short),    (BinaryBlob blob, object s) => {blob.AddUInt(7); blob.AddShort((short)s)	;}},
			{ typeof(ushort),   (BinaryBlob blob, object s) => {blob.AddUInt(8); blob.AddUShort((ushort)s)	;}},
			{ typeof(float),    (BinaryBlob blob, object s) => {blob.AddUInt(9); blob.AddSingle((float)s);  ;}},
			{ typeof(System.TimeSpan),          (BinaryBlob blob, object s) => {blob.AddUInt(10); blob.AddLong(((TimeSpan)s).Ticks); } },
			{ typeof(System.DateTimeOffset),    (BinaryBlob blob, object s) => {blob.AddUInt(11); blob.AddLong(((DateTimeOffset)s).UtcTicks); } }
		};

		private static Dictionary<uint, Type> primitiveIdMap = new Dictionary<uint, Type>(){
			{1, typeof(String)},
			{2, typeof(int)},
			{3, typeof(uint)},
			{4, typeof(long)},
			{5, typeof(ulong)},
			{6, typeof(byte)},
			{7, typeof(short)},
			{8, typeof(ushort)},
			{9, typeof(float)},
			{10, typeof(System.TimeSpan)},
			{11, typeof(System.DateTimeOffset)}
		};

		private static Dictionary<Type, Func<BinaryBlob, object>> dynamicDeserializerMap = new Dictionary<Type, Func<BinaryBlob, object>>(){
			{ typeof(String),   (BinaryBlob blob) => blob.ReadString() },
			{ typeof(int),      (BinaryBlob blob) => blob.ReadInt() },
			{ typeof(uint),     (BinaryBlob blob) => blob.ReadUInt() },
			{ typeof(long),     (BinaryBlob blob) => blob.ReadLong() },
			{ typeof(ulong),    (BinaryBlob blob) => blob.ReadULong() },
			{ typeof(byte),     (BinaryBlob blob) => blob.ReadByte() },
			{ typeof(short),    (BinaryBlob blob) => blob.ReadShort() },
			{ typeof(ushort),   (BinaryBlob blob) => blob.ReadUShort() },
			{ typeof(float),    (BinaryBlob blob) => blob.ReadSingle() },
			{ typeof(System.TimeSpan),          (BinaryBlob blob) => TimeSpan.FromTicks(blob.ReadLong()) },
			{ typeof(System.DateTimeOffset),    (BinaryBlob blob) => new DateTimeOffset(blob.ReadLong(), TimeSpan.Zero) }
		};

		public static Func<BinaryBlob, object> GetObjectDeserializer(uint id)
		{
			if(primitiveIdMap.TryGetValue(id, out Type type)) 
			{
				dynamicDeserializerMap.TryGetValue(type, out Func<BinaryBlob, object> res);
				return res;
			}
			return null;
		}
		public static Action<BinaryBlob, object> GetObjectSerializer(Type type)
		{
			dynamicSerializerMap.TryGetValue(type, out Action<BinaryBlob, object> res);
			return res;
		}
	}
}
