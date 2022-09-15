using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer
{
    class EnumerablePropertySerializer<TT, T, D> : IPropertySerializer where T:ICollection<D>, new()
    {
        private readonly Func<TT, T> getterFunc;
        private readonly Action<TT, T> setterFunc;
        private readonly INetworkSerializer serializer;

        public EnumerablePropertySerializer(Func<TT, T> getterFunc, Action<TT, T> setterFunc, INetworkSerializer serializer)
        {
            this.getterFunc = getterFunc;
            this.setterFunc = setterFunc;
            this.serializer = serializer;
        }

        public void Deserialize(object owner, BinaryBlob blob)
        {
            ushort count = blob.ReadUShort();
            T list = new T();
            for (int i = 0; i < count; i++)
            {
                object ob = serializer.Deserialize(blob);
                list.Add((D)ob);
            }
            setterFunc.Invoke((TT)owner, list);
        }

        public void Serialize(object ob, BinaryBlob blob)
        {
            T prop = getterFunc.Invoke((TT)ob);
            blob.AddUShort((ushort)prop.Count());
            foreach( var d in prop)
            {
                serializer.Serialize(blob, d);
            }
        }
    }
}
