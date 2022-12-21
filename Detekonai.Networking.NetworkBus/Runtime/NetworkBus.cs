using Detekonai.Core;
using Detekonai.Core.Common;
using Detekonai.Networking.Runtime;
using Detekonai.Networking.Runtime.AsyncEvent;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Detekonai.Core.Common.ILogger;

namespace Detekonai.Networking
{
	public sealed class NetworkBus : INetworkBus
	{
		private readonly IMessageBus bus;
        private readonly INetworkSerializerFactory factory;
        private Dictionary<Type, IHandlerToken> tokens = new Dictionary<Type, IHandlerToken>();
		private readonly Dictionary<Type, Action<BaseMessage, MessageRequestTicket>> responseDelegates = new Dictionary<Type, Action<BaseMessage, MessageRequestTicket>>();
		private readonly HashSet<Type> incommingBlacklist = new HashSet<Type>();
		private readonly HashSet<Type> outgoingBlacklist = new HashSet<Type>();
		private ICommChannel channel;

		public string Name { get; private set; }
		public int MaximumAllowedBlobSerializationDelay { get; set; } = -1;
        public ICommChannel Channel { 
			get
			{
				return channel;
			}
			set
			{
				if(channel != null && channel != value)
                {
					Disconnect();
                }
				channel = value;
				Connect();
			}
		}

        public bool Active {
			get 
			{
				return channel != null && channel.Status == ICommChannel.EChannelStatus.Open;
			}
		}

		public ILogger LogConnector { get; set; } = null;

		public NetworkBus(string name, IMessageBus bus, INetworkSerializerFactory factory)
		{
			Name = name;
			this.bus = bus;
            this.factory = factory;
            RegisterMessages(factory);
		}

		public void AddToIncommingBlacklist<T>() where T : NetworkMessage
        {
			incommingBlacklist.Add(typeof(T));
        }
		public void AddToOutgoingBlacklist<T>() where T : NetworkMessage
        {
			outgoingBlacklist.Add(typeof(T));
		}

		public class MessageRequestTicket : INetworkBus.IMessageRequestTicket
        {
            private readonly NetworkBus bus;
            private readonly IRequestTicket originalTicket;

            public MessageRequestTicket(NetworkBus bus, IRequestTicket originalTicket)
            {
                this.bus = bus;
                this.originalTicket = originalTicket;
            }

            public async Task Fulfill(NetworkMessage msg)
            {
				originalTicket.Fulfill(await bus.Serialize(msg));
            }
        }

        public async Task<BaseMessage> SendRPC(NetworkMessage msg)
        {
			return await SendRPC(msg, CancellationToken.None);
        }

		public async Task<BaseMessage> SendRPC(NetworkMessage msg, CancellationToken token)
        {
			ICommResponse res = null;
			BinaryBlob blob = await Serialize(msg);
			try
            {
				using (res = await channel.SendRPC(blob, token))
                {
					return Deserialize(res.Blob);
                }
			}
			finally
            {
				if(res == null)
                {
					blob.Release();
                }
            }
		}


		private void Connect()
		{
			if(channel == null)
			{
				return;
			}

			channel.Tactics.OnBlobReceived += Channel_BlobReceived;
            channel.Tactics.RequestHandler = Channel_OnRequestReceived;
			LogConnector?.Log(this, $"{Name} NetworkBus connected to channel: {channel.Name}");
		}


        private void Channel_OnRequestReceived(ICommChannel channel, BinaryBlob request, IRequestTicket ticket)
        {
			var msg = Deserialize(request);

			if (msg != null)
			{
				if(incommingBlacklist.Contains(msg.GetType()))
                {
					LogConnector?.Log(this, $"Network message {msg.GetType()} is blacklisted, we will not process it!", LogLevel.Error);
				}
				else if(responseDelegates.TryGetValue(msg.GetType(), out Action<BaseMessage, MessageRequestTicket> callback))
				{
					callback(msg, new MessageRequestTicket(this,ticket));
				}
				else
                {
					LogConnector?.Log(this, $"Network message {msg.GetType()} requres a reply but we don't have a reply handler registered!", LogLevel.Error);
					return;
                }
			}
            else 
			{
				LogConnector?.Log(this, "Failed to deserialize message!", LogLevel.Error);
			}

			return;
		}

		public void SetRequestHandler<T>(Action<T, INetworkBus.IMessageRequestTicket> handler) where T : NetworkMessage
		{
			responseDelegates[typeof(T)] = (BaseMessage x, MessageRequestTicket y) => handler(x as T, y);
		}

		private void Channel_BlobReceived(ICommChannel channel, BinaryBlob e)
		{
			NetworkMessage msg = Deserialize(e);
			if(msg != null)
			{
				if (incommingBlacklist.Contains(msg.GetType()))
				{
					LogConnector?.Log(this, $"Network message {msg.GetType()} is blacklisted, we will not process it!", LogLevel.Error);
				}
				else if (tokens.TryGetValue(msg.GetType(), out IHandlerToken token))
				{
					LogConnector?.Log(this, $"{Name} Dispatching message {msg.GetType()} to memory bus");
					token.Trigger(msg);
				}
			}
			else
			{
				LogConnector?.Log(this, "Failed to deserialize message!", LogLevel.Error);
			}
		}

		private NetworkMessage Deserialize(BinaryBlob blob)
		{
			uint hash = blob.ReadUInt();
			INetworkSerializer ser = factory.Get(hash);
			if (ser != null)
			{

				NetworkMessage msg = (NetworkMessage)ser.Deserialize(blob);
				if(msg != null)
                {
					msg.Local = false;
                }
				return msg;
			}
			else
            {
				return null;
            }
		}

		private void RegisterMessages(INetworkSerializerFactory factory)
		{
			foreach(INetworkSerializer ser in factory.Serializers)
			{
				if (ser.SerializedType.IsSubclassOf(typeof(NetworkMessage)))
				{
					LogConnector?.Log(this, $"Message {ser.SerializedType} is registered to NetworkBus {Name}.");
					tokens[ser.SerializedType] = bus.Subscribe(ser.SerializedType, OnLocalMessage);
				}
			}
		}

		public void Dispose()
		{
			Disconnect();
			foreach(IHandlerToken token in tokens.Values)
			{
				bus.Unsubscribe(token);
			}
			tokens.Clear();
		}

		private async void OnLocalMessage(BaseMessage msg)
		{
			if(Active && msg is NetworkMessage nmsg && nmsg.Local && !outgoingBlacklist.Contains(msg.GetType()))
			{
				LogConnector?.Log(this, $"{Name} Dispatching message {msg.GetType()} to network");
				BinaryBlob blob = await Serialize(msg as NetworkMessage);
				if(blob != null)
				{
					channel.Send(blob);
				}
			}
		}

		private async Task<BinaryBlob> Serialize(NetworkMessage msg)
        {
			INetworkSerializer ser = factory.Get(msg.GetType());
			if (ser != null)
			{
				CancellationToken ct = CancellationToken.None;
				if (MaximumAllowedBlobSerializationDelay != -1)
                {
					CancellationTokenSource cts = new CancellationTokenSource();
					cts.CancelAfter(MaximumAllowedBlobSerializationDelay);
					ct = cts.Token;
                }

				BinaryBlob blob = await channel.CreateMessageWithSizeAsync(ct, ser.RequiredSize);
				blob.AddUInt(ser.ObjectId);
				ser.Serialize(blob, msg);
				return blob;
			}
			return null;
		}

		public void Disconnect()
		{
			if(channel != null)
            {
				channel.Tactics.OnBlobReceived -= Channel_BlobReceived;
			}
		}
    }
}
