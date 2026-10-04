using System;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Tests
{
	public interface ISerializableValueTest
	{
		int Number { get; }
	}

	[Serializable] public class SerializableValueTestData : ISerializableValueTest
	{
		// - Construct -
		public SerializableValueTestData() { }

		public SerializableValueTestData(int number) {
			_number = number;
		}

		// - Public Properties -
		public int Number
		{
			get { return _number; }
		}

		// - Private & Protected -
		[SerializeField] int _number;
	}

	[Serializable] public sealed class SerializableValueDerivedTestData : SerializableValueTestData
	{
		// - Construct -
		public SerializableValueDerivedTestData(int number) : base(number) { }
	}

	[Serializable] public sealed class SerializableValueParameterizedTestData : ISerializableValueTest
	{
		// - Construct -
		public SerializableValueParameterizedTestData(int number) {
			_number = number;
		}

		// - Public Properties -
		public int Number
		{
			get { return _number; }
		}

		// - Private & Protected -
		[SerializeField] int _number;
	}

	[Serializable] public sealed class SerializableValueUnrelatedTestData { }

	public sealed class SerializableValueNonSerializableTestData { }

}
