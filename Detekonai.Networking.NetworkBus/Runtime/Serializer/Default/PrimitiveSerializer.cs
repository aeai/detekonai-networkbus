using Detekonai.Core;
using Detekonai.Networking.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer
{
   public class PrimitiveSerializer : INetworkSerializer
    {
        private readonly STypeConverter converter;

        public uint MessageId { get; private set; }

        public Type SerializedType { get; private set; }

        public int RequiredSize { get; private set; } = 0;

        public PrimitiveSerializer(Type t, STypeConverter converter)
        {
            SerializedType = t;
            this.converter = converter;
        }

        public object Deserialize(BinaryBlob blob)
        {
            return converter.rawReader(blob);
        }

        public void Serialize(BinaryBlob blob, object ob)
        {
            converter.rawWriter(blob, ob);
        }
    }
}
