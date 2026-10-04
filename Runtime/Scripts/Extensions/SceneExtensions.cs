using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParkMinDev.UPM.Foundation.Extensions
{
	public static class SceneExtensions
	{
		public static T FindComponent<T>(this Scene scene, bool includeInactive = true)
			where T : class {
			if (!scene.IsValid() || !scene.isLoaded)
				return null;

			foreach (GameObject root in scene.GetRootGameObjects()) {
				T component = root.GetComponentInChildren<T>(includeInactive);

				if (component != null)
					return component;
			}

			return null;
		}

		public static List<T> FindComponents<T>(this Scene scene, bool includeInactive = true)
			where T : class {
			List<T> results = new List<T>();

			if (!scene.IsValid() || !scene.isLoaded)
				return results;

			foreach (GameObject root in scene.GetRootGameObjects())
				root.GetComponentsInChildren(includeInactive, results);

			return results;
		}
	}
}