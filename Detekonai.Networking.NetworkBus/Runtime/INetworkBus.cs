using Detekonai.Core.Common;
using System;

namespace Detekonai.Networking
{
	public interface INetworkBus : ILogCapable, IDisposable
	{
		public string Name { get; }
		public ICommChannel Channel { get; set; }
		public bool Active { get; }
	}
}
