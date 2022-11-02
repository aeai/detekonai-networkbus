using Detekonai.Core;
using Detekonai.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer.Experimental
{

    public class AbstractNetworkSerializerFactory : INetworkSerializerFactory
    {
        private readonly Dictionary<Type, INetworkSerializer> serializers = new Dictionary<Type, INetworkSerializer>();
        private readonly Dictionary<uint, INetworkSerializer> serializersByHash = new Dictionary<uint, INetworkSerializer>();
        public ILogger Logger { get; set; }

		public INetworkSerializer Get(uint id)
        {
            serializersByHash.TryGetValue(id, out INetworkSerializer res);
            return res;
        }

        public void AddSerializer(Type type, INetworkSerializer serializer)
        {
            serializers.Add(type, serializer);
            serializersByHash.Add(serializer.ObjectId, serializer);
        }

        public INetworkSerializer Build(Type type)
        {
            return Get(type);
        }

        public INetworkSerializer Get(Type type)
        {
            serializers.TryGetValue(type, out INetworkSerializer res);
            return res;
        }
    }
}
