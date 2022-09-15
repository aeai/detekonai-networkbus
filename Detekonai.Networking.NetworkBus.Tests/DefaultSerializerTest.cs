using Detekonai.Core;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializerTest
	{
		
		private class NetworkTestMessage : NetworkMessage
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }
			
			[NetworkSerializableProperty("Int")]
			public int IntProp { get; set; }
			
			[NetworkSerializableProperty("Stuff")]
			public DataThing Stuff { get; set; }

			[NetworkSerializableProperty("StuffList")]
			public List<DataThing> StuffList { get; set; }

			[NetworkSerializableProperty("List")]
			public List<string> StringList { get; set; } = new List<string>();

			[NetworkSerializableProperty("Dict")]
			public Dictionary<int, string> intStringMap { get; set; } = new Dictionary<int, string>();

		}

		[NetworkSerializable]
		private class MessageWithDictionary
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; } = "ss";

			[NetworkSerializableProperty("Int")]
			public int intProp { get; set; } = 4;

			[NetworkSerializableProperty("Dict")]
			public Dictionary<int, string> IntStringMap { get; set; } = new Dictionary<int, string>();
		}

		[NetworkSerializable]
		private class DataThing 
		{
			[NetworkSerializableProperty("Fruit")]
			public string Fruit { get; set; }
			[NetworkSerializableProperty("Int")]
			public int Number { get; set; }
		}
		
		[NetworkSerializable]
		private class MessageWithObject 
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }
			[NetworkSerializableProperty("Raw")]
			public object Raw { get; set; }
		}

		[NetworkSerializable]
		private class MessageWithObjectArray
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }
			[NetworkSerializableProperty("Raw")]
			public object[] Raw { get; set; }
		}

		[Test]
		public void Default_serializer_can_serialize_raw_objects() 
		{
			object value = 12;

			INetworkSerializerFactory factory = new DefaultSerializerFactory();
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			DefaultSerializer serializer = new DefaultSerializer(typeof(MessageWithObject), new TypeConverterRepository(), factory);

			MessageWithObject msg = new MessageWithObject() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObject msg2 = (MessageWithObject)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(msg2.Raw, Is.EqualTo(value));
		}

		[Test]
		public void Default_serializer_can_serialize_raw_lists()
		{
			object value = new List<int>() { 1234, 5678};

			INetworkSerializerFactory factory = new DefaultSerializerFactory();
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			DefaultSerializer serializer = new DefaultSerializer(typeof(MessageWithObject), new TypeConverterRepository(), factory);

			MessageWithObject msg = new MessageWithObject() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObject msg2 = (MessageWithObject)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			List<int> res = (List<int>)msg2.Raw;
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(res.Count, Is.EqualTo(2));
			Assert.That(res[0], Is.EqualTo(1234));
			Assert.That(res[1], Is.EqualTo(5678));
		}

		[Test]
		public void Default_serializer_can_serialize_raw_arrays()
		{
			object[] value = new object[]{ 1234, "alma", 56 };

			INetworkSerializerFactory factory = new DefaultSerializerFactory();
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			DefaultSerializer serializer = new DefaultSerializer(typeof(MessageWithObjectArray), new TypeConverterRepository(), factory);

			MessageWithObjectArray msg = new MessageWithObjectArray() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObjectArray msg2 = (MessageWithObjectArray)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(msg2.Raw.Length, Is.EqualTo(3));
			Assert.That(msg2.Raw[0], Is.EqualTo(1234));
			Assert.That(msg2.Raw[1], Is.EqualTo("alma"));
			Assert.That(msg2.Raw[2], Is.EqualTo(56));
		}

		[Test]
		public void Default_serializer_can_serialize_empty_raw_arrays()
		{
			object[] value = new object[] {};

			INetworkSerializerFactory factory = new DefaultSerializerFactory();
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			DefaultSerializer serializer = new DefaultSerializer(typeof(MessageWithObjectArray), new TypeConverterRepository(), factory);

			MessageWithObjectArray msg = new MessageWithObjectArray() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObjectArray msg2 = (MessageWithObjectArray)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(msg2.Raw.Length, Is.EqualTo(0));
		}

		[Test]
		public void Default_serializer_can_serialize_dictionaries()
		{
			Dictionary<int,string> value = new Dictionary<int, string>();
			value[13124] = "A big number";
			value[-1234] = "A small number";
			value[1985] = "A year";

			INetworkSerializerFactory factory = new DefaultSerializerFactory();
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			DefaultSerializer serializer = new DefaultSerializer(typeof(MessageWithDictionary), new TypeConverterRepository(), factory);

			MessageWithDictionary msg = new MessageWithDictionary() { IntStringMap = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithDictionary msg2 = (MessageWithDictionary)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.IntStringMap.Count, Is.EqualTo(3));
			Assert.That(msg2.IntStringMap.ContainsKey(13124), Is.True);
			Assert.That(msg2.IntStringMap.ContainsKey(-1234), Is.True);
			Assert.That(msg2.IntStringMap.ContainsKey(1985), Is.True);
			Assert.That(msg2.IntStringMap[13124], Is.EqualTo("A big number"));
			Assert.That(msg2.IntStringMap[-1234], Is.EqualTo("A small number"));
			Assert.That(msg2.IntStringMap[1985], Is.EqualTo("A year"));
		}

		[Test]
		public void DefaultSerializerTestSimplePasses()
		{

			INetworkSerializerFactory factory = new DefaultSerializerFactory();

			DefaultSerializer serializer = new DefaultSerializer(typeof(NetworkTestMessage), new TypeConverterRepository(), factory);
			NetworkTestMessage msg = new NetworkTestMessage();
			msg.StringProp = "alma";
			msg.IntProp = 1234;
			msg.StringList.Add("barack");
			msg.StringList.Add("korte");
			msg.Stuff = new DataThing() { Fruit = "alma", Number = 12 };
			msg.StuffList = new List<DataThing>();
			msg.StuffList.Add(new DataThing() { Fruit = "list1", Number = 111 });
			msg.StuffList.Add(new DataThing() { Fruit = "list2", Number = 222 });
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);

			blob.JumpIndexToBegin();
			NetworkTestMessage msg2 = (NetworkTestMessage)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.IntProp, Is.EqualTo(1234));
			Assert.That(msg2.StringProp, Is.EqualTo("alma"));
			Assert.That(msg2.StringList.Count, Is.EqualTo(2));
			Assert.That(msg2.Stuff.Fruit, Is.EqualTo("alma"));
			Assert.That(msg2.Stuff.Number, Is.EqualTo(12));
			Assert.That(msg2.StuffList.Count, Is.EqualTo(2));
		}
	}
}
