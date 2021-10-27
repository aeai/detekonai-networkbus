using Detekonai.Core;
using Detekonai.Networking.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer
{
    public class NetworkEventPropertySerializer<TT,T> : IPropertySerializer
    {
        private readonly Func<TT, T> getterFunc;
        private readonly Action<TT, T> setterFunc;
        private readonly INetworkSerializer serializer;

        public NetworkEventPropertySerializer(Func<TT, T> getterFunc, Action<TT, T> setterFunc, INetworkSerializer serializer)
        {
            this.getterFunc = getterFunc;
            this.setterFunc = setterFunc;
            this.serializer = serializer;
        }

        public void Deserialize(object owner, BinaryBlob blob)
        {
            object ob = serializer.Deserialize(blob);
            setterFunc.Invoke((TT)owner, (T)ob);
        }

        public void Serialize(object ob, BinaryBlob blob)
        {
            T prop = getterFunc.Invoke((TT)ob);
            serializer.Serialize(blob, prop);
        }
    }
}
