using System;
using ParkMinPackages.Foundation.Components;
using UnityEngine;

namespace ParkMinPackages.Foundation.Extensions
{
	public static class GameObjectExtensions
	{
		public static bool IsSceneObject(this GameObject gameObject) {
			if (gameObject == null)
				return false;

			return gameObject.scene != default;
		}
		
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component {
			if (gameObject.TryGetComponent<T>(out T t))
				return t;
			else
				return gameObject.AddComponent<T>();
		}
		
		public static TDependencyBehaviour AddDependencyBehaviour<TDependencyBehaviour>(
			this GameObject target,
			Action<TDependencyBehaviour> dependencySetter = null
		) where TDependencyBehaviour : DependencyBehaviour {
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			TDependencyBehaviour dependencyBehaviour = target.AddComponent<TDependencyBehaviour>();
			try {
				dependencySetter?.Invoke(dependencyBehaviour);
				dependencyBehaviour.ValidateDependencies();
				return dependencyBehaviour;
			}
			catch {
				if (Application.isPlaying)
					UnityEngine.Object.Destroy(dependencyBehaviour);
				else
					UnityEngine.Object.DestroyImmediate(dependencyBehaviour);
				throw;
			}
		}

		public static TDependencyBehaviour GetDependencyBehaviour<TDependencyBehaviour>(
			this GameObject target,
			Action<TDependencyBehaviour> dependencySetter = null
		) where TDependencyBehaviour : DependencyBehaviour {
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (target.TryGetComponent(out TDependencyBehaviour dependencyBehaviour) == false)
				throw new MissingComponentException($"{typeof(TDependencyBehaviour).Name} was not found on {target.name}.");

			dependencySetter?.Invoke(dependencyBehaviour);
			dependencyBehaviour.ValidateDependencies();
			return dependencyBehaviour;
		}

		public static TDependencyBehaviour GetOrAddDependencyBehaviour<TDependencyBehaviour>(
			this GameObject target,
			Action<TDependencyBehaviour> dependencySetter = null
		) where TDependencyBehaviour : DependencyBehaviour {
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (target.TryGetComponent(out TDependencyBehaviour dependencyBehaviour))
				return target.GetDependencyBehaviour(dependencySetter);

			return target.AddDependencyBehaviour(dependencySetter);
		}
	}
}
