namespace ParkMinPackages.Foundation.Components
{
	public abstract class DependencyBehaviour : ExtendedBehaviour
	{
		//Public Methods-------------------------------------------------------------------------------------------
		public abstract void ValidateDependencies();

		//Handlers-------------------------------------------------------------------------------------------------
		protected override void Start() {
			ValidateDependencies();
			base.Start();
		}
	}
}
