using Detekonai.Core;
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
		[NetworkEvent]
		private class NetworkTestMessage : BaseMessage
		{
			[NetworkSerializable("String")]
			public string StringProp { get; set; }
			[NetworkSerializable("Int")]
			public int IntProp { get; set; }
		}

		// A Test behaves as an ordinary method
		[Test]
		public void DefaultSerializerTestSimplePasses()
		{
			// Use the Assert class to test conditions
			DefaultSerializer serializer = new DefaultSerializer(typeof(NetworkTestMessage), new TypeConverterRepository());
			NetworkTestMessage msg = new NetworkTestMessage();
			msg.StringProp = "alma";
			msg.IntProp = 1234;

			BinaryBlobPool pool = new BinaryBlobPool(10, 64);
			BinaryBlob blob = pool.GetBlob();
			serializer.Serialize(blob, msg);

			blob.JumpIndexToBegin();
			uint hash = blob.ReadUInt();
			NetworkTestMessage msg2 = (NetworkTestMessage)serializer.Deserialize(blob);

			Assert.That(msg2, Is.Not.Null);
			Assert.That(msg2.IntProp, Is.EqualTo(1234));
			Assert.That(msg2.StringProp, Is.EqualTo("alma"));
		}
	}
}
