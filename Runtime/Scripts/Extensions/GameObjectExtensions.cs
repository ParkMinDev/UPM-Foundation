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
	}
}