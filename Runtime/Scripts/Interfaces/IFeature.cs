using UnityEngine;

namespace ParkMinPackages.Foundation.Interfaces
{
	public interface IFeature : IDependencyValidator
	{
		public void InitOwner(Component owner);
		public bool IsOwner(Component owner);
	}
}
