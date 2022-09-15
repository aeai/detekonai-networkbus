using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking
{
    [NetworkSerializable]
    public abstract class NetworkMessage : BaseMessage
    {
        public bool Local { get; internal set; } = true;
    }
}
