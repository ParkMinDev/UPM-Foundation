using System;
using UnityEngine;

namespace ParkMinPackages.Foundation.Extensions
{
	public static class ComponentExtensions
	{
		public static Component AddComponent(this Component component, Type componentType) {
			return component.gameObject.AddComponent(componentType);
		}

		public static T AddComponent<T>(this Component component) where T : Component {
			return component.gameObject.AddComponent<T>();
		}

		public static bool IsSceneObject(this Component component) {
			if (component == null)
				return false;

			return component.gameObject.scene.IsValid();
		}

		public static T GetOrAddComponent<T>(this Component component) where T : Component {
			if (component.TryGetComponent<T>(out T t))
				return t;
			else
				return component.AddComponent<T>();
		}
	}
}