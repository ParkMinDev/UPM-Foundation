using UnityEngine;

namespace ParkMinPackages.Foundation.Tests
{
	public sealed class SerializableValueTestBehaviour : MonoBehaviour, ISerializableValueTest
	{
		// - Public Properties -
		public int Number
		{
			get { return _number; }
			set { _number = value; }
		}

		// - Private & Protected -
		[SerializeField] int _number;
	}
}
