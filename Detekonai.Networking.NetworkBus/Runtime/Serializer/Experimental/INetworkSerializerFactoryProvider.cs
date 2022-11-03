using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.NetworkBus.Runtime.Serializer.Experimental
{
    public interface INetworkSerializerFactoryProvider
    {
        INetworkSerializerFactory Factory { get; }
    }
}
