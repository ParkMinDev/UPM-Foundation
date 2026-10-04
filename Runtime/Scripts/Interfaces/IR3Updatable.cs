namespace ParkMinDev.UPM.Foundation.Interfaces
{
	public interface IR3EarlyUpdatable
	{
		void R3EarlyUpdate();
	}

	public interface IR3FixedUpdatable
	{
		void R3FixedUpdate();
	}

	public interface IR3PreUpdatable
	{
		void R3PreUpdate();
	}

	public interface IR3Updatable
	{
		void R3Update();
	}

	public interface IR3PreLateUpdatable
	{
		void R3PreLateUpdate();
	}

	public interface IR3PostLateUpdatable
	{
		void R3PostLateUpdate();
	}

	public interface IR3TimeUpdatable
	{
		void R3TimeUpdate();
	}

	public interface IR3PostFixedUpdatable
	{
		void R3PostFixedUpdate();
	}
}