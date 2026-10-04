using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Interfaces
{
	public interface IFeature : IDependencyValidator
	{
		public void InitOwner(Component owner);
		public bool IsOwner(Component owner);
	}
}
