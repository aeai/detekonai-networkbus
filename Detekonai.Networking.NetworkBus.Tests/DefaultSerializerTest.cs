using Detekonai.Core;
using Detekonai.Core.Common;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Detekonai.Networking.Serializer.Experimental;
namespace Detekonai.Networking.Serializer
{
	public class DefaultSerializerTest
	{
		public class NetworkTestMessageWithPrivateSetter : NetworkMessage
		{
			[NetworkSerializableProperty("Int2")]
			public int Int2Prop { get;  set; } = 2;


			public NetworkTestMessageWithPrivateSetter()
			{
			}

			public NetworkTestMessageWithPrivateSetter(int int2Value)
			{
				Int2Prop = int2Value;
			}
		}

		[NetworkSerializable]
		public class InheritTest : NetworkTestMessage
		{
			[NetworkSerializableProperty("Boo")]
			public string Boo { get; set; }
		}

		public class NetworkTestMessage : NetworkMessage, ILogger
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }

			[NetworkSerializableProperty("Int")]
			public int IntProp { get; set; }

			[NetworkSerializableProperty("ULong")]
			public ulong UlongProp { get; set; }

			[NetworkSerializableProperty("TimeOffset")]
			public DateTimeOffset TimeOffsetProp { get; set; }

			[NetworkSerializableProperty("TimeSpan")]
			public TimeSpan TimeSpanProp { get; set; }

			[NetworkSerializableProperty("Stuff")]
			public DataThing Stuff { get; set; }

			[NetworkSerializableProperty("StuffList")]
			public List<DataThing> StuffList { get; set; }

			[NetworkSerializableProperty("List")]
			public List<string> StringList { get; set; } = new List<string>();

			[NetworkSerializableProperty("Dict")]
			public Dictionary<int, string> intStringMap { get; set; } = new Dictionary<int, string>();

            public void Log(object sender, string msg, ILogger.LogLevel level = ILogger.LogLevel.Verbose, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            {
                throw new NotImplementedException();
            }

            public void Log(object sender, string msg, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            {
                throw new NotImplementedException();
            }
        }

		[NetworkSerializable]
		public class MessageWithDictionary
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; } = "ss";

			[NetworkSerializableProperty("Int")]
			public int intProp { get; set; } = 4;

			[NetworkSerializableProperty("Dict")]
			public Dictionary<int, string> IntStringMap { get; set; }
		}

		[NetworkSerializable]
		public class MessageWithStruct
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; } = "ss";

			[NetworkSerializableProperty("struct")]
			public StructThing structProp { get; set; }

			[NetworkSerializableProperty("structList")]
			public List<StructThing> structList { get; set; } = new List<StructThing>();

			[NetworkSerializableProperty("structDict")]
			public Dictionary<string, StructThing> structDict { get; set; }

			[NetworkSerializableProperty("structObj")]
			public object structObj { get; set; }

			[NetworkSerializableProperty("Raw")]
			public object[] Raw { get; set; }

			public MessageWithStruct() { }
		}

		[NetworkSerializable]
		public class DataThing
		{
			[NetworkSerializableProperty("Fruit")]
			public string Fruit { get; set; }
			[NetworkSerializableProperty("Int")]
			public int Number { get; set; }
		}

		public struct StructThing
		{		
			public string Fruit { get; set; }
			public int Number { get; set; }
		}

		[NetworkSerializable]
		public class MessageWithObject
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }
			[NetworkSerializableProperty("Raw")]
			public object Raw { get; set; }
		}

		[NetworkSerializable]
		public class MessageWithObjectArray
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }
			[NetworkSerializableProperty("Raw")]
			public object[] Raw { get; set; }
		}

		[NetworkSerializable]
		public class ParentThing
		{
			[NetworkSerializableProperty("ParentProp")]
			public string StringProp { get; set; }
		}
		[NetworkSerializable]
		public class ChildThing1 : ParentThing
		{
			[NetworkSerializableProperty("Child1Prop")]
			public int intProp { get; set; }
		}
		[NetworkSerializable]
		public class ChildThing2 : ParentThing
		{
			[NetworkSerializableProperty("Child2Prop")]
			public bool BoolProp { get; set; }
		}
		[NetworkSerializable]
		public class MessageWithSomethingInherited
		{
			[NetworkSerializableProperty("String")]
			public string StringProp { get; set; }
			[NetworkSerializableProperty("Obj", Virtual = true)]
			public ParentThing Obj { get; set; }
		}

		[Test]
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_virtual_objects(INetworkSerializerFactory factory)
		{
			object value = 12;

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithSomethingInherited));
			MessageWithSomethingInherited msg = new MessageWithSomethingInherited() { StringProp = "Test", Obj = new ChildThing1() { intProp = 515} };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithSomethingInherited msg2 = (MessageWithSomethingInherited)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(msg2.Obj, Is.Not.Null);
			Assert.That(((ChildThing1)msg2.Obj).intProp, Is.EqualTo(515));
		}

		[Test]
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_raw_objects(INetworkSerializerFactory factory)
		{
			object value = 12;

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithObject));
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
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_null_objects(INetworkSerializerFactory factory)
		{
			object value = null;

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithObject));
			MessageWithObject msg = new MessageWithObject() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObject msg2 = (MessageWithObject)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(msg2.Raw, Is.Null);
		}

		[Test]
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_raw_lists(INetworkSerializerFactory factory)
		{
			object value = new List<string>() { "alma",null, "korte" };

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithObject));
			MessageWithObject msg = new MessageWithObject() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObject msg2 = (MessageWithObject)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			List<string> res = (List<string>)msg2.Raw;
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(res.Count, Is.EqualTo(3));
			Assert.That(res[0], Is.EqualTo("alma"));
			Assert.That(res[1], Is.Null);
			Assert.That(res[2], Is.EqualTo("korte"));
		}

		[Test]
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_raw_arrays(INetworkSerializerFactory factory)
		{
			object[] value = new object[] { 1234, "alma", 56 };

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithObjectArray));
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
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_empty_raw_arrays(INetworkSerializerFactory factory)
		{
			object[] value = new object[] { };

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithObjectArray));
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
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_null_raw_arrays(INetworkSerializerFactory factory)
		{
			object[] value = null;

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithObjectArray));
			MessageWithObjectArray msg = new MessageWithObjectArray() { StringProp = "Test", Raw = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithObjectArray msg2 = (MessageWithObjectArray)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("Test"));
			Assert.That(msg2.Raw, Is.Null);
		}

		[Test]
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_dictionaries(INetworkSerializerFactory factory)
		{
			Dictionary<int, string> value = new Dictionary<int, string>();
			value[13124] = "A big number";
			value[-1234] = "A small number";
			value[1985] = "A year";

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithDictionary));
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
		[TestCaseSource(nameof(Factories))]
		public void Default_serializer_can_serialize_null_dictionaries(INetworkSerializerFactory factory)
		{
			Dictionary<int, string> value = null;

			BinaryBlobPool pool = new BinaryBlobPool(10, 128);

			INetworkSerializer serializer = factory.Build(typeof(MessageWithDictionary));
			MessageWithDictionary msg = new MessageWithDictionary() { IntStringMap = value };
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithDictionary msg2 = (MessageWithDictionary)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.IntStringMap, Is.Null);
		}

		[Test]
		public void Default_serializer_can_have_custom_converter()
		{
			BinaryBlobPool pool = new BinaryBlobPool(10, 512);
			DefaultSerializerFactory factory = new DefaultSerializerFactory();
			Action<BinaryBlob, StructThing> writter = (BinaryBlob blob, StructThing st) =>
			{
				blob.AddString(st.Fruit);
				blob.AddInt(st.Number);
			};
			Func<BinaryBlob, StructThing> reader = (BinaryBlob blob) =>
			{
				StructThing st = new StructThing();
				st.Fruit = blob.ReadString();
				st.Number = blob.ReadInt();
				return st;
			};
			factory.AddCustomConverter(writter, reader);
			INetworkSerializer serializer = factory.Build(typeof(MessageWithStruct));

			MessageWithStruct msg = new MessageWithStruct() { 
				StringProp = "alma",
				structProp = new StructThing() { Fruit = "korte", Number = 9 },
				structList = new List<StructThing>() { new StructThing() { Fruit = "korte", Number = 10 }, new StructThing() { Fruit = "barack", Number = 11 } },
				structDict = new Dictionary<string, StructThing>() { { "alma", new StructThing() { Fruit = "alma", Number = 12 } } },
				structObj = new StructThing() { Fruit = "szilva", Number = 13 },
				Raw = new object[2] { new StructThing() { Fruit = "alma", Number = 14 }, new StructThing() { Fruit = "barack", Number = 15 } }
			};
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);
			blob.JumpIndexToBegin();
			MessageWithStruct msg2 = (MessageWithStruct)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.StringProp, Is.EqualTo("alma"));
			Assert.That(msg2.structProp, Is.Not.Null);
			Assert.That(msg2.structProp.Fruit, Is.EqualTo("korte"));
			Assert.That(msg2.structProp.Number, Is.EqualTo(9));


			Assert.That(msg2.structObj, Is.Not.Null);
			Assert.That(((StructThing)msg2.structObj).Fruit, Is.EqualTo("szilva"));
			Assert.That(((StructThing)msg2.structObj).Number, Is.EqualTo(13));

			Assert.That(msg2.structList, Is.Not.Null);
			Assert.That(msg2.structList.Count, Is.EqualTo(2));
			Assert.That(msg2.structList[0].Fruit, Is.EqualTo("korte"));
			Assert.That(msg2.structList[0].Number, Is.EqualTo(10));
			Assert.That(msg2.structList[1].Fruit, Is.EqualTo("barack"));
			Assert.That(msg2.structList[1].Number, Is.EqualTo(11));

			Assert.That(msg2.structDict, Is.Not.Null);
			Assert.That(msg2.structDict.Count, Is.EqualTo(1));
			Assert.That(msg2.structDict["alma"].Fruit, Is.EqualTo("alma"));
			Assert.That(msg2.structDict["alma"].Number, Is.EqualTo(12));


			Assert.That(msg2.Raw, Is.Not.Null);
			Assert.That(msg2.Raw.Length, Is.EqualTo(2));
			Assert.That(((StructThing)msg2.Raw[0]).Fruit, Is.EqualTo("alma"));
			Assert.That(((StructThing)msg2.Raw[0]).Number, Is.EqualTo(14));
			Assert.That(((StructThing)msg2.Raw[1]).Fruit, Is.EqualTo("barack"));
			Assert.That(((StructThing)msg2.Raw[1]).Number, Is.EqualTo(15));
		}
		private class Logger : ILogger
		{
			public void Log(object sender, string msg, ILogger.LogLevel level = ILogger.LogLevel.Verbose, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
			{
				Console.WriteLine(msg);
			}

			public void Log(object sender, string msg, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
			{
				throw new NotImplementedException();
			}
		}
		static object[] Factories =
{
			new DefaultSerializerFactory(),
			new DetekonaiNetworkingNetworkBusTestsSerializerFactory(new Logger()),
			new CompositeNetworkSerializerFactory(new DetekonaiNetworkingNetworkBusTestsSerializerFactory(new Logger()), new DefaultSerializerFactory())
		};


		[Test]
		[TestCaseSource(nameof(Factories))]
		public void DefaultSerializerTestSimplePasses(INetworkSerializerFactory factory)
		{


			INetworkSerializer serializer = factory.Build(typeof(NetworkTestMessage));
			NetworkTestMessage msg = new NetworkTestMessage();
			msg.StringProp = "alma";
			msg.IntProp = 1234;
			msg.UlongProp = 45;
			msg.TimeSpanProp = TimeSpan.FromSeconds(45);
			DateTimeOffset now = DateTimeOffset.Now;
			msg.TimeOffsetProp = now;
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
			Assert.That(msg2.UlongProp, Is.EqualTo(45));
			Assert.That(msg2.TimeSpanProp, Is.EqualTo(TimeSpan.FromSeconds(45)));
			Assert.That(msg2.TimeOffsetProp, Is.EqualTo(now));
			Assert.That(msg2.UlongProp, Is.EqualTo(45));
			Assert.That(msg2.StringProp, Is.EqualTo("alma"));
			Assert.That(msg2.StringList.Count, Is.EqualTo(2));
			Assert.That(msg2.Stuff.Fruit, Is.EqualTo("alma"));
			Assert.That(msg2.Stuff.Number, Is.EqualTo(12));
			Assert.That(msg2.StuffList.Count, Is.EqualTo(2));
		}

		[Test]
		[TestCaseSource(nameof(Factories))]
		public void DefaultSerializerCanUsePrivateSetters(INetworkSerializerFactory factory)
		{

			INetworkSerializer serializer = factory.Build(typeof(NetworkTestMessageWithPrivateSetter));
			NetworkTestMessageWithPrivateSetter msg = new NetworkTestMessageWithPrivateSetter(45);
			BinaryBlobPool pool = new BinaryBlobPool(10, 128);
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);

			blob.JumpIndexToBegin();
			NetworkTestMessageWithPrivateSetter msg2 = (NetworkTestMessageWithPrivateSetter)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.Int2Prop, Is.EqualTo(45));
	
		}

	}
}
