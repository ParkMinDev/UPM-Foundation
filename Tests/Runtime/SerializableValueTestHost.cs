using ParkMinPackages.Foundation.Objects.Datas;
using UnityEngine;

namespace ParkMinPackages.Foundation.Tests
{
	public sealed class SerializableValueTestHost : MonoBehaviour
	{
		// - Public Properties -
		public SerializableValue<SerializableValueTestData> InlineValue
		{
			get { return _inlineValue; }
		}
		public SerializableValue<ISerializableValueTest> ManagedValue
		{
			get { return _managedValue; }
		}
		public SerializableValue<ISerializableValueTest> ComponentValue
		{
			get { return _componentValue; }
		}
		public SerializableValue<ISerializableValueTest> AssetValue
		{
			get { return _assetValue; }
		}

		// - Private & Protected -
		[SerializeField] SerializableValue<SerializableValueTestData> _inlineValue = new SerializableValue<SerializableValueTestData>();
		[SerializeField] SerializableValue<ISerializableValueTest> _managedValue = new SerializableValue<ISerializableValueTest>();
		[SerializeField] SerializableValue<ISerializableValueTest> _componentValue = new SerializableValue<ISerializableValueTest>();
		[SerializeField] SerializableValue<ISerializableValueTest> _assetValue = new SerializableValue<ISerializableValueTest>();
	}
}
