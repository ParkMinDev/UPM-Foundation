namespace ParkMinDev.UPM.Foundation.Interfaces
{
	public interface IInitializable<TArguments>
	{
		void Init(TArguments arguments);
	}
}