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
		[NetworkSerializable]
		private class NetworkTestMessage : BaseMessage
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

		}

		[NetworkSerializable]
		private class DataThing 
		{
			[NetworkSerializableProperty("Fruit")]
			public string Fruit { get; set; }
			[NetworkSerializableProperty("Int")]
			public int Number { get; set; }
		}

		// A Test behaves as an ordinary method
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
