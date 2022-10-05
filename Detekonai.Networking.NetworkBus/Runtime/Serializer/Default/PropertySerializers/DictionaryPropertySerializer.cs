using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer
{
    class DictionaryPropertySerializer<TT, T, K,V> : IPropertySerializer where T: Dictionary<K,V>, new()
    {
        private readonly Func<TT, T> getterFunc;
        private readonly Action<TT, T> setterFunc;
        private readonly INetworkSerializer keySerializer;
        private readonly INetworkSerializer valueSerializer;
        public DictionaryPropertySerializer(Func<TT, T> getterFunc, Action<TT, T> setterFunc, INetworkSerializer keySerializer, INetworkSerializer valueSerializer)
        {
            this.getterFunc = getterFunc;
            this.setterFunc = setterFunc;
            this.keySerializer = keySerializer;
            this.valueSerializer = valueSerializer;
        }

        public void Deserialize(object owner, BinaryBlob blob)
        {
            ushort count = blob.ReadUShort();
            if (count == 0)
            {
                setterFunc.Invoke((TT)owner, null);
            }
            else
            {
                count--;
                T dict = new T();
                for (int i = 0; i < count; i++)
                {
                    K key = (K)keySerializer.Deserialize(blob);
                    V value = (V)valueSerializer.Deserialize(blob);
                    dict[key] = value;
                }
                setterFunc.Invoke((TT)owner, dict);
            }
        }

        public void Serialize(object ob, BinaryBlob blob)
        {
            T prop = getterFunc.Invoke((TT)ob);
            if (prop == null)
            {
                blob.AddUShort(0);
            }
            else
            {
                blob.AddUShort((ushort)(prop.Count() + 1));
                foreach (var d in prop)
                {
                    keySerializer.Serialize(blob, d.Key);
                    valueSerializer.Serialize(blob, d.Value);
                }
            }
        }
    }
}
