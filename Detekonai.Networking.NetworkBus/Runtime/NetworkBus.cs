using Detekonai.Core;
using Detekonai.Networking.Runtime.AsyncEvent;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Detekonai.Core.Common.ILogCapable;

namespace Detekonai.Networking
{
	public sealed class NetworkBus : INetworkBus
	{
		private IMessageBus bus;
		private Dictionary<Type, IHandlerToken> tokens = new Dictionary<Type, IHandlerToken>();
		private Dictionary<Type, INetworkSerializer> serializers = new Dictionary<Type, INetworkSerializer>();
		private Dictionary<uint, INetworkSerializer> serializersByHash = new Dictionary<uint, INetworkSerializer>();
		private readonly Dictionary<Type, Func<BaseMessage, BaseMessage>> responseDelegates = new Dictionary<Type, Func<BaseMessage, BaseMessage>>();

		private ICommChannel channel;

		private HashSet<BaseMessage> processedMessages = new HashSet<BaseMessage>();
		
		public string Name { get; private set; }
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

        public event LogHandler Logger;

		public NetworkBus(string name, IMessageBus bus, INetworkSerializerFactory factory)
		{
			Name = name;
			this.bus = bus;
			RegisterMessages(factory);
		}

        private struct MessageAwaiter : IUniversalAwaiter<BaseMessage>
        {
            private readonly NetworkBus owner;
            private readonly IUniversalAwaiter<ICommResponse> other;

			public bool IsCompleted => other.IsCompleted;

            public bool IsInitialized => other.IsInitialized;

			private BaseMessage result;

			public MessageAwaiter(NetworkBus owner, IUniversalAwaiter<ICommResponse> other)
            {
                this.owner = owner;
                this.other = other;
				result = null;
            }

			public void Cancel()
            {
				other.Cancel();
            }

            public BaseMessage GetResult()
            {
				if (result == null)
				{
					using ICommResponse res = other.GetResult();
					if (res.Status == AwaitResponseStatus.Finished)
					{
						result = owner.Deserialize(res.Blob);
					}
				}

				return result;
            }

            public void OnCompleted(Action continuation)
            {
				other.OnCompleted(continuation);
            }
        }

        public UniversalAwaitable<BaseMessage> SendRPC(BaseMessage msg)
        {
			BinaryBlob blob = Serialize(msg);
			UniversalAwaitable<ICommResponse> res = channel.SendRPC(blob);
			return new UniversalAwaitable<BaseMessage>(new MessageAwaiter(this, res.GetAwaiter()));
        }


		private void Connect()
		{
			if(channel == null)
			{
				return;
			}

			channel.Tactics.OnBlobReceived += Channel_BlobReceived;
            channel.Tactics.RequestHandler = Channel_OnRequestReceived;
			Logger?.Invoke(this, $"{Name} NetworkBus connected to channel: {channel.Name}");
		}


        private BinaryBlob Channel_OnRequestReceived(ICommChannel channel, BinaryBlob request)
        {
			var msg = Deserialize(request);

			if (msg != null)
			{
				if(responseDelegates.TryGetValue(msg.GetType(), out Func<BaseMessage,BaseMessage> callback))
				{
					return Serialize(callback(msg));
				}
				else
                {
					return null;
                }
			}

			return null;
		}

		public void SetRequestHandler<T>(Func<T, BaseMessage> handler) where T : BaseMessage
		{
			responseDelegates[typeof(T)] = (BaseMessage x) => handler(x as T);
		}


		private void Channel_BlobReceived(ICommChannel channel, BinaryBlob e)
		{
			var msg = Deserialize(e);

			if(msg != null)
			{
				processedMessages.Add(msg);
				if(tokens.TryGetValue(msg.GetType(), out IHandlerToken token))
				{
					Logger?.Invoke(this, $"{Name} Dispatching message {msg.GetType()} to memory bus");
					token.Trigger(msg);
				}
			}
		}

		private BaseMessage Deserialize(BinaryBlob blob)
		{
			uint hash = blob.ReadUInt();
			if (serializersByHash.TryGetValue(hash, out INetworkSerializer ser))
			{
				
				return ser.Deserialize(blob);
			}
			else
            {
				return null;
            }
		}

		private void RegisterMessages(INetworkSerializerFactory factory)
		{
			var types = AppDomain.CurrentDomain.GetAssemblies().Where(x => !IsOmittable(x)).SelectMany(s => s.GetTypes()).Where(p => p.IsSubclassOf(typeof(BaseMessage)) && p.GetCustomAttribute<NetworkEventAttribute>() != null);
			foreach(Type t in types)
			{
				Logger?.Invoke(this, $"Message {t} is registered to NetworkBus {Name}.");
				INetworkSerializer ser = factory.Build(t);
				serializers[t] = ser;
				serializersByHash[ser.MessageId] = ser;
				tokens[t] = bus.Subscribe(t, OnLocalMessage);
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

		private void OnLocalMessage(BaseMessage msg)
		{
			if(!processedMessages.Remove(msg) && Active)
			{
				Logger?.Invoke(this, $"{Name} Dispatching message {msg.GetType()} to network");
				BinaryBlob blob = Serialize(msg);
				if(blob != null)
				{
					channel.Send(blob);
				}
			}
		}

		private BinaryBlob Serialize(BaseMessage msg)
        {
			if (serializers.TryGetValue(msg.GetType(), out INetworkSerializer ser))
			{
				BinaryBlob blob = channel.CreateMessage();
				ser.Serialize(blob, msg);
				return blob;
			}
			return null;
		}

		private static bool IsOmittable(Assembly assembly)
		{
			string assemblyName = assembly.GetName().Name;
			return StartsWith("System") ||
				StartsWith("Microsoft") ||
				StartsWith("Windows") ||
				StartsWith("Unity") ||
				StartsWith("netstandard");

			bool StartsWith(string value) => assemblyName.StartsWith(value, ignoreCase: false, culture: CultureInfo.CurrentCulture);
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
