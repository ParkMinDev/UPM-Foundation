using System;
using ParkMinPackages.Foundation.Interfaces;
using UnityEngine;

namespace ParkMinPackages.Foundation.Extensions
{
	public static class FeatureExtensions
	{
		// - Public Methods -
		public static TFeature AddFeature<TFeature>(
			this Component owner
		) where TFeature : Component, IFeature {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			TFeature feature = owner.gameObject.AddComponent<TFeature>();
			try {
				feature.InitOwner(owner);
				return feature;
			}
			catch {
				if (Application.isPlaying)
					UnityEngine.Object.Destroy(feature);
				else
					UnityEngine.Object.DestroyImmediate(feature);
				throw;
			}
		}

		public static TFeature GetFeature<TFeature>(
			this Component owner
		) where TFeature : Component, IFeature {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			if (owner.TryGetComponent(out TFeature feature) == false)
				throw new MissingComponentException($"{typeof(TFeature).Name} was not found on {owner.name}.");
			if (feature.IsOwner(owner) == false)
				throw new InvalidOperationException($"{typeof(TFeature).Name} is assigned to another owner.");

			return feature;
		}

		public static TFeature GetOrAddFeature<TFeature>(
			this Component owner
		) where TFeature : Component, IFeature {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			if (owner.TryGetComponent(out TFeature feature) == false)
				return owner.AddFeature<TFeature>();
			if (feature.IsOwner(owner) == false)
				throw new InvalidOperationException($"{typeof(TFeature).Name} is assigned to another owner.");

			return feature;
		}
	}
}
