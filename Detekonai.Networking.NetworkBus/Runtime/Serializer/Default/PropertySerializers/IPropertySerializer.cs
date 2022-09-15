using Detekonai.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking.Serializer 
{ 
	public interface IPropertySerializer
	{
		void Deserialize(object ob, BinaryBlob blob);
		void Serialize(object ob, BinaryBlob blob);
	}
}
