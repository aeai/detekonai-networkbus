using Detekonai.Core;
using Detekonai.Core.Common;
using Detekonai.Networking.Runtime;
using Detekonai.Networking.Runtime.AsyncEvent;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Detekonai.Networking
{
	public interface INetworkBus: IDisposable
	{
		public interface IMessageRequestTicket
        {
			Task Fulfill(NetworkMessage msg);
		}

		string Name { get; }
		ICommChannel Channel { get; set; }
		bool Active { get; }
		ILogger LogConnector { get; set; }
		Task<BaseMessage> SendRPC(NetworkMessage msg);
		Task<BaseMessage> SendRPC(NetworkMessage msg, CancellationToken token);
		void SetRequestHandler<T>(Action<T, IMessageRequestTicket> handler) where T : NetworkMessage;

		void AddToIncommingBlacklist<T>() where T : NetworkMessage;
		void AddToOutgoingBlacklist<T>() where T : NetworkMessage;
	}
}
