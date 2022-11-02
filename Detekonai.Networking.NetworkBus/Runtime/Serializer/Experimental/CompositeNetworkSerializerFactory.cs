using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer.Experimental
{
    public class CompositeNetworkSerializerFactory : INetworkSerializerFactory
    {
        private readonly INetworkSerializerFactory[] factories;

        public CompositeNetworkSerializerFactory(params INetworkSerializerFactory[] factories )
        {
            this.factories = factories;
        }

        public INetworkSerializer Build(Type type)
        {
            foreach (var factory in factories)
            {
                var res = factory.Build(type);
                if(res != null)
                {
                    return res;
                }
            }
            return null;
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
