using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ParkMinPackages.Foundation.Editor")]
[assembly: InternalsVisibleTo("ParkMinPackages.Foundation.Editor.Tests")]

namespace ParkMinPackages.Foundation.Objects.Datas
{
	public enum SerializableValueStatus
	{
		Empty,
		Valid,
		MissingReference,
		TypeMismatch
	}

	internal enum SerializableValueSource
	{
		None = 0,
		InlineValue = 1,
		UnityObject = 2,
		ManagedReference = 3
	}

	[Serializable] public sealed class SerializableValue<T> where T : class
	{
		// - Statics -
		static bool IsSupportedDeclaredType() {
			Type type = typeof(T);
			return typeof(ICollection).IsAssignableFrom(type) == false &&
			       (type.IsGenericType == false || type.IsInterface || type.IsAbstract);
		}

		static bool IsSupportedManagedReference(object value) {
			Type type = value.GetType();
			return value is not string &&
			       value is not Delegate &&
			       value is not ICollection &&
			       type.IsGenericType == false &&
			       type.GetCustomAttribute<SerializableAttribute>() != null;
		}

		static bool CanStoreInline(T value) {
			Type valueType = typeof(T);
			return valueType.IsInterface == false &&
			       valueType.IsAbstract == false &&
			       value.GetType() == valueType &&
			       valueType.GetCustomAttribute<SerializableAttribute>() != null;
		}

		// - Construct -
		public SerializableValue() { }

		public SerializableValue(T value) {
			Value = value;
		}

		// - Public Properties -
		public SerializableValueStatus Status
		{
			get
			{
				switch (_source) {
					case SerializableValueSource.None:
						return SerializableValueStatus.Empty;
					case SerializableValueSource.InlineValue:
						return _inlineValue == null
							? SerializableValueStatus.Empty
							: SerializableValueStatus.Valid;
					case SerializableValueSource.UnityObject:
						if (ReferenceEquals(_unityObject, null))
							return SerializableValueStatus.Empty;
						if (_unityObject == null)
							return SerializableValueStatus.MissingReference;

						return _unityObject is T
							? SerializableValueStatus.Valid
							: SerializableValueStatus.TypeMismatch;
					case SerializableValueSource.ManagedReference:
						if (_managedReference == null)
							return SerializableValueStatus.Empty;

						return _managedReference is T
							? SerializableValueStatus.Valid
							: SerializableValueStatus.TypeMismatch;
					default:
						return SerializableValueStatus.TypeMismatch;
				}
			}
		}
		public bool HasValue
		{
			get { return Status == SerializableValueStatus.Valid; }
		}
		public T Value
		{
			get
			{
				switch (Status) {
					case SerializableValueStatus.Empty:
						return default;
					case SerializableValueStatus.Valid:
						switch (_source) {
							case SerializableValueSource.InlineValue:
								return _inlineValue;
							case SerializableValueSource.UnityObject when _unityObject is T unityValue:
								return unityValue;
							case SerializableValueSource.ManagedReference when _managedReference is T managedValue:
								return managedValue;
							default:
								throw new InvalidOperationException($"Stored value for {typeof(T).Name} could not be resolved.");
						}
					case SerializableValueStatus.MissingReference:
						throw new InvalidOperationException($"Stored reference for {typeof(T).Name} is missing.");
					case SerializableValueStatus.TypeMismatch:
						throw new InvalidCastException($"Stored value cannot be converted to {typeof(T).Name}.");
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			set
			{
				if (value != null && IsSupportedDeclaredType() == false)
					throw new ArgumentException($"{typeof(T).FullName} cannot be stored as a SerializableValue.", nameof(value));
				if (value != null && value is not UnityEngine.Object && IsSupportedManagedReference(value) == false)
					throw new ArgumentException($"{value.GetType().FullName} cannot be stored as a managed reference.", nameof(value));

				SerializableValueSource previousSource = _source;

				_source = SerializableValueSource.None;
				_inlineValue = null;
				_unityObject = null;
				_managedReference = null;

				if (value is null)
					return;

				if (value is UnityEngine.Object unityObject) {
					_source = SerializableValueSource.UnityObject;
					_unityObject = unityObject;
				}
				else if (CanStoreInline(value) && (previousSource is SerializableValueSource.None or SerializableValueSource.InlineValue)) {
					_source = SerializableValueSource.InlineValue;
					_inlineValue = value;
				}
				else {
					_source = SerializableValueSource.ManagedReference;
					_managedReference = value;
				}
			}
		}

		// - Private & Protected -
		[SerializeField] SerializableValueSource _source;
		[SerializeField] T _inlineValue;
		[SerializeField] UnityEngine.Object _unityObject;
		[SerializeReference] object _managedReference;
	}
}
