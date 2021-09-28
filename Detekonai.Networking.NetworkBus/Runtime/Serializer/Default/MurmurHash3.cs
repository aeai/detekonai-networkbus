using System;
using System.Collections;
using System.Collections.Generic;

namespace Detekonai.Networking.Serializer
{
	public class MurmurHash3
	{

		private static uint rotl32(uint x, uint r)
		{
			return (x << (int)r) | (x >> (int)(32 - r));
		}

		private static uint fmix32(uint h)
		{
			h ^= h >> 16;
			h *= 0x85ebca6b;
			h ^= h >> 13;
			h *= 0xc2b2ae35;
			h ^= h >> 16;

			return h;
		}

		public static uint Hash(byte[] data, uint len, uint seed)
		{
			uint nblocks = len / 4;

			uint h1 = seed;

			uint c1 = 0xcc9e2d51;
			uint c2 = 0x1b873593;

			//----------
			// body

			//uint blocks = nblocks * 4;

			for(int i = 0; i < len; i += 4)
			{
				uint k1 = BitConverter.ToUInt32(data, i);//getblock32(data, i);

				k1 *= c1;
				k1 = rotl32(k1, 15);
				k1 *= c2;

				h1 ^= k1;
				h1 = rotl32(h1, 13);
				h1 = h1 * 5 + 0xe6546b64;
			}

			//----------
			// tail
			uint tailOffset = nblocks * 4;
			{
				uint k1 = 0;

				switch(len & 3)
				{
					case 3:
						k1 ^= (uint)data[tailOffset + 2] << 16;
						goto case 2;
					case 2:
						k1 ^= (uint)data[tailOffset + 1] << 8;
						goto case 1;
					case 1:
						k1 ^= (uint)data[tailOffset + 0];
						k1 *= c1;
						k1 = rotl32(k1, 15);
						k1 *= c2;
						h1 ^= k1;
						break;
				}
			}

			//----------
			// finalization

			h1 ^= len;

			h1 = fmix32(h1);

			return h1;
		}

	}
}
