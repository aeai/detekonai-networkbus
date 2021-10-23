using System;
using System.Runtime.CompilerServices;

namespace Detekonai.Networking
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class NetworkEventAttribute : Attribute
	{
		public string Name { get; private set; }
		public int SizeRequirement { get; set; } = 0;
		public NetworkEventAttribute(string name = null)
        {
			Name = name; ///this not working
        }
	}
}
