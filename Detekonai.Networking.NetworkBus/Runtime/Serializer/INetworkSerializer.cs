using Detekonai.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Detekonai.Networking
{
	public interface INetworkSerializer
	{
		void Serialize(BinaryBlob blob, object ob);
		object Deserialize(BinaryBlob blob);
		uint ObjectId { get; }
		Type SerializedType { get; }
		int RequiredSize { get; }
	}
}
