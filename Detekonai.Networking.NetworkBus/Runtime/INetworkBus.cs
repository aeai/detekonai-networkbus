﻿using Detekonai.Core;
using Detekonai.Core.Common;
using Detekonai.Networking.Runtime.AsyncEvent;
using System;

namespace Detekonai.Networking
{
	public interface INetworkBus: IDisposable
	{
		string Name { get; }
		ICommChannel Channel { get; set; }
		bool Active { get; }
		ILogConnector LogConnector { get; set; }
		UniversalAwaitable<BaseMessage> SendRPC(BaseMessage msg);
	}
}
