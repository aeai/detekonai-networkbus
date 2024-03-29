﻿using Detekonai.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Detekonai.Networking.Serializer
{
	public class PropertySerializer<TT, T> : IPropertySerializer
	{
		private readonly Func<TT, T> getter;
		private readonly Action<TT, T> setter;
		private readonly Action<BinaryBlob, T> writer;
		private readonly Func<BinaryBlob, T> reader;

		public PropertySerializer(Func<TT, T> getterFunc, Action<TT, T> setterFunc, Action<BinaryBlob, T> writerFunc, Func<BinaryBlob, T> readerFunc)
		{
			getter = getterFunc;
			setter = setterFunc;
			writer = writerFunc;
			reader = readerFunc;

		}

		public void Deserialize(object ob, BinaryBlob blob)
		{
			var r = reader.Invoke(blob);
			setter.Invoke((TT)ob, r);
		}

        public void Serialize(object ob, BinaryBlob blob)
		{
			var r = getter.Invoke((TT)ob);
			writer.Invoke(blob, r);
		}
	}
}
