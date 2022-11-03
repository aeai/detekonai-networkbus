using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer.Experimental
{
    public interface INetworkSerializerFactoryProvider
    {
        INetworkSerializerFactory Factory { get; }
    }
}
