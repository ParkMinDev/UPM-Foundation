using System;
using ParkMinPackages.Foundation.Components;
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

			TFeature[] features = owner.GetComponents<TFeature>();
			foreach (TFeature feature in features) {
				if (feature.IsOwner(owner))
					return feature;
			}

			throw new MissingComponentException($"{typeof(TFeature).Name} owned by {owner.name} was not found.");
		}

		public static TFeature GetOrAddFeature<TFeature>(
			this Component owner
		) where TFeature : Component, IFeature {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			TFeature[] features = owner.GetComponents<TFeature>();
			foreach (TFeature feature in features) {
				if (feature.IsOwner(owner))
					return feature;
			}

			return owner.AddFeature<TFeature>();
		}

		public static int RemoveFeatures<TFeature>(
			this Component owner
		) where TFeature : ExtendedBehaviour, IFeature {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			TFeature[] features = owner.GetComponents<TFeature>();
			int removedCount = 0;
			foreach (TFeature feature in features) {
				if (feature == null || feature.IsOwner(owner) == false)
					continue;

				feature.Dispose();
				removedCount++;
			}

			return removedCount;
		}
	}
}
