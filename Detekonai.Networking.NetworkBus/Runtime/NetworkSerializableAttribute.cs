using System;
using System.Runtime.CompilerServices;

namespace Detekonai.Networking
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
	public class NetworkSerializableAttribute : Attribute
	{
		public string Name { get; private set; }
		public int SizeRequirement { get; set; } = 0;
		public NetworkSerializableAttribute(string name = null)
        {
			Name = name; ///this not working
        }
	}
}
