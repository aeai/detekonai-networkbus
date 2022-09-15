using Detekonai.Core;
using Detekonai.Core.Common;
using Detekonai.Networking.Runtime;
using Detekonai.Networking.Runtime.AsyncEvent;
using System;
using System.Threading;

namespace Detekonai.Networking
{
	public interface INetworkBus: IDisposable
	{
		public interface IMessageRequestTicket
        {
			void Fulfill(NetworkMessage msg);
		}

		string Name { get; }
		ICommChannel Channel { get; set; }
		bool Active { get; }
		ILogger LogConnector { get; set; }
		UniversalAwaitable<BaseMessage> SendRPC(NetworkMessage msg);
		UniversalAwaitable<BaseMessage> SendRPC(NetworkMessage msg, CancellationToken token);
		void SetRequestHandler<T>(Action<T, IMessageRequestTicket> handler) where T : NetworkMessage;
	}
}
