using Detekonai.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Detekonai.Networking
{
	public interface INetworkSerializer
	{
		void Serialize(BinaryBlob blob, BaseMessage ob);
		BaseMessage Deserialize(BinaryBlob blob);
		uint MessageId { get; }
		Type SerializedType { get; }
		int RequiredSize { get; }
	}
}
