﻿using Detekonai.Core;
using System;

namespace Detekonai.Networking.Serializer
{
    public class RawPropertySerializer<TT> : IPropertySerializer
    {
        private Func<TT, object> getter;
        private Action<TT, object> setter;
        private readonly ITypeConverterRepository repo;

        public RawPropertySerializer(Func<TT, object> getterFunc, Action<TT, object> setterFunc, ITypeConverterRepository repo)
        {
            getter = getterFunc;
            setter = setterFunc;
            this.repo = repo;
        }

        public void Deserialize(object ob, BinaryBlob blob)
        {
            int typeId = blob.ReadShort();
            if (repo.TryGetConverter(typeId, out STypeConverter del))
            {
                var r = del.rawReader.Invoke(blob);
                setter.Invoke((TT)ob, r);
            }
        }

        public void Serialize(object ob, BinaryBlob blob)
        {
            var r = getter.Invoke((TT)ob);
            if (repo.TryGetConverter(r.GetType(), out STypeConverter del))
            {
                del.rawWriter.Invoke(blob, r);
            }
        }
    }
}