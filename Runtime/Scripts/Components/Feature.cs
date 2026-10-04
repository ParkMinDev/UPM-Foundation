using System;
using System.Linq;
using ParkMinDev.UPM.Foundation.Constants;
using ParkMinDev.UPM.Foundation.Interfaces;
using ParkMinDev.UPM.Foundation.Objects.Datas;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Components
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Components", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "Feature`1")]
	[DefaultExecutionOrder(1)]
	public abstract class Feature<TOwner> : ExtendedBehaviour, IInitializable<TOwner>, IFeature where TOwner : class
	{
		// - Public Methods -
		public virtual void Init(TOwner owner) {
			_owner = owner;
		}

		public virtual void ValidateDependencies() {
			if (Owner == null || Owner is UnityEngine.Object unityOwner && unityOwner == null)
				throw new ArgumentException($"{typeof(TOwner).Name} owner is not assigned.");
		}

		// - Public Properties -
		public TOwner Owner
		{
			get { return _owner; }
		}

		// - Handler -
		protected virtual void Awake() {
			if (_initialOwner != null && _initialOwner.HasValue)
				Init(_initialOwner.Value);
		}

		protected override void Start() {
			ValidateDependencies();
			base.Start();
		}

		protected virtual void Reset() {
			if (typeof(Component).IsAssignableFrom(typeof(TOwner)) == false && typeof(TOwner).IsInterface == false)
				return;

			_initialOwner ??= new SerializableValue<TOwner>();
			TOwner[] owners = GetComponents<Component>().OfType<TOwner>().ToArray();
			_initialOwner.Value = owners.Length == 1 ? owners[0] : default;
		}

		// - Internals -
		[Header(Headers.Injectable)]
		[SerializeField] SerializableValue<TOwner> _initialOwner = new SerializableValue<TOwner>();

		TOwner _owner;

		void IFeature.InitOwner(Component owner) {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			if (owner is not TOwner typedOwner)
				throw new ArgumentException($"{owner.GetType().Name} cannot be used as {typeof(TOwner).Name}.", nameof(owner));

			Init(typedOwner);
		}

		bool IFeature.IsOwner(Component owner) {
			return owner != null && _owner is Component ownerComponent && ownerComponent == owner;
		}
	}
}
