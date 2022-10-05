using Detekonai.Core;
using Detekonai.Networking.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer
{
    public class RawArraySerializer<TT> : IPropertySerializer
    {
        private Func<TT, object[]> getter;
        private Action<TT, object[]> setter;
        private readonly ITypeConverterRepository repo;

        public RawArraySerializer(Func<TT, object[]> getterFunc, Action<TT, object[]> setterFunc, ITypeConverterRepository repo)
        {
            getter = getterFunc;
            setter = setterFunc;
            this.repo = repo;
        }


        public void Deserialize(object ob, BinaryBlob blob)
        {
            int count = blob.ReadUShort();
            if (count == 0)
            {
                setter.Invoke((TT)ob, null);
            }
            else
            {
                count--;
                object[] arr = new object[count];
                for (ushort i = 0; i < count; i++)
                {
                    int typeId = blob.ReadUShort();
                    if (repo.TryGetConverter(typeId, out STypeConverter del))
                    {
                        arr[i] = del.rawReader.Invoke(blob);
                    }
                }
                setter.Invoke((TT)ob, arr);
            }
        }

        public void Serialize(object ob, BinaryBlob blob)
        {
            object[] arr = getter.Invoke((TT)ob);
            if (arr == null)
            {
                blob.AddUShort(0);
            }
            else
            {
                blob.AddUShort((ushort)(arr.Length + 1));
                for (ushort i = 0; i < arr.Length; i++)
                {
                    if (repo.TryGetConverter(arr[i].GetType(), out STypeConverter del))
                    {
                        blob.AddUShort(del.id);
                        del.rawWriter.Invoke(blob, arr[i]);
                    }
                }
            }
        }
    }
}
