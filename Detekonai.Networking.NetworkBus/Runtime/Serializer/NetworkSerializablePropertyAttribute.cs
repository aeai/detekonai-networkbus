using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Detekonai.Networking
{ 
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class NetworkSerializablePropertyAttribute : Attribute
	{
        public string Name { get; }
        public bool Virtual { get; set; } = false;
        public NetworkSerializablePropertyAttribute([CallerMemberName] string name = null)
        {
            Name = name;
        }

    }
}
