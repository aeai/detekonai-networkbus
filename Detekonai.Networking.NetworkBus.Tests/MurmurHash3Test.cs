using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Detekonai.Networking.Serializer
{
	public class MurmurHash3Test
	{
		[Test]
		[Sequential]
		public void String_Hashing_Working([Values(
		"Ye I hash the hash from this string",
		"Ye I hash the hash from this string uh",
		"tok",
		"to"
		)] string data,
			[Values(
		(uint)150947394,
		(uint)1416379515,
		(uint)2882400530,
		(uint)602284106)]uint exp)
		{
			uint hash = 0;
			Assert.DoesNotThrow(() =>
			{
				hash = HashString(data, 123456, 128);
			});
			Assert.That(hash, Is.EqualTo(exp));
		}

		[Test]
		public void Buffer_size_doesnt_matter([Values("Ye I hash the hash from this string", "Ye")] string data)
		{
			uint hash1 = 0;
			uint hash2 = 0;
			Assert.DoesNotThrow(() =>
			{
				hash1 = HashString(data, 123456, 128);
			});
			Assert.DoesNotThrow(() =>
			{
				hash2 = HashString(data, 123456, 256);
			});

			Assert.That(hash1, Is.EqualTo(hash2));
		}

		[Test]
		public void CollisionTest()
		{
			Dictionary<uint, string> hashes = new Dictionary<uint, string>();
			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).GroupBy(x => x.FullName).Select(x => x.First());
			int counter = 0;
			foreach(Type t in types)
			{

				uint hash = HashString(t.FullName, 1234567, 512);
				if(hashes.ContainsKey(hash))
				{
					Console.WriteLine($"Collision: {hashes[hash]} --> {t.FullName}");
					counter++;
				}
				else
				{
					hashes.Add(hash, t.FullName);
				}
			}

			Assert.That(counter, Is.EqualTo(0));
		}

		private uint HashString(string val, uint seed, uint bufferSize)
		{
			byte[] data = new byte[bufferSize];
			uint len = (uint)System.Text.Encoding.UTF8.GetBytes(val, 0, val.Length, data, 0);
			return MurmurHash3.Hash(data, len, seed);
		}
	}
}
