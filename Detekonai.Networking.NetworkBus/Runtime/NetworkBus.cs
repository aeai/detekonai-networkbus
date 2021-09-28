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

		private void Connect()
		{
			if(channel == null)
			{
				return;
			}

			channel.OnBlobReceived += Channel_BlobReceived;
			Logger?.Invoke(this, $"{Name} NetworkBus connected to channel: {channel.Name}");
		}

		private void Channel_BlobReceived(ICommChannel channel, BinaryBlob e)
		{
			uint hash = e.ReadUInt();
			if(serializersByHash.TryGetValue(hash, out INetworkSerializer ser))
			{
				BaseMessage msg = ser.Deserialize(e);
				processedMessages.Add(msg);
				if(tokens.TryGetValue(ser.SerializedType, out IHandlerToken token))
				{
					Logger?.Invoke(this, $"{Name} Dispatching message {msg.GetType()} to memory bus");
					token.Trigger(msg);
				}
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
				if(serializers.TryGetValue(msg.GetType(), out INetworkSerializer ser))
				{
					BinaryBlob blob = channel.CreateMessage();
					ser.Serialize(blob, msg);
					channel.Send(blob);
				}
			}
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
				channel.OnBlobReceived -= Channel_BlobReceived;
			}
		}
	}
}
