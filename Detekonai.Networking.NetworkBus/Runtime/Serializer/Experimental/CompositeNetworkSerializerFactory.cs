using System;
using System.Collections.Generic;
using System.Linq;

namespace Detekonai.Networking.Serializer.Experimental
{
    public class CompositeNetworkSerializerFactory : INetworkSerializerFactory
    {
        private readonly List<INetworkSerializerFactory> factories = new List<INetworkSerializerFactory>();

        public IEnumerable<INetworkSerializer> Serializers => factories.SelectMany(x => x.Serializers).Distinct();

        public CompositeNetworkSerializerFactory(params INetworkSerializerFactory[] factories)
        {
            this.factories.AddRange(factories);
        }
        public CompositeNetworkSerializerFactory AddFactory(params INetworkSerializerFactory[] factories)
        {
            this.factories.AddRange(factories);
            return this;
        }

        public INetworkSerializer Get(Type type)
        {
            foreach (var factory in factories)
            {
                var res = factory.Get(type);
                if (res != null)
                {
                    return res;
                }
            }
            return null;
        }

        public INetworkSerializer Get(uint id)
        {
            foreach (var factory in factories)
            {
                var res = factory.Get(id);
                if (res != null)
                {
                    return res;
                }
            }
            return null;
        }
    }
}
