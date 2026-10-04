using System;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace ParkMinDev.UPM.Foundation.ScriptableObjects
{
#if UNITY_EDITOR
	internal sealed class ScriptableSingletonAssetCreator : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths,
			bool didDomainReload) {
			if (!didDomainReload)
				return;

			foreach (Type type in TypeCache.GetTypesDerivedFrom(typeof(ScriptableSingleton<>))) {
				if (type.IsAbstract)
					continue;

				PropertyInfo instanceProperty = type.GetProperty(
					"Instance",
					BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

				instanceProperty.GetValue(null);
			}
		}
	}
#endif

	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.ScriptableObjects", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "ScriptableSingleton`1")]
	public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
	{
		// - Public - 
		[Preserve]
		public static T Instance
		{
			get
			{
				if (_instance != null)
					return _instance;

				string resourcesPath = GetResourcesPath();
				_instance = Resources.Load<T>(resourcesPath);
#if UNITY_EDITOR
				if (_instance == null)
					_instance = CreateAsset(resourcesPath);
#endif

				if (_instance == null)
					throw new InvalidOperationException($"{typeof(T).FullName} resource asset was not found: {resourcesPath}");

				return _instance;
			}
		}

		// - Internal - 
		static T _instance;

		static string GetResourcesPath() {
			string namespacePath = typeof(T).Namespace?.Replace('.', '/');
			return string.IsNullOrEmpty(namespacePath)
				? $"ScriptableSingletons/{typeof(T).Name}"
				: $"ScriptableSingletons/{namespacePath}/{typeof(T).Name}";
		}

#if UNITY_EDITOR
		static T CreateAsset(string resourcesPath) {
			string assetPath = $"Assets/Resources/{resourcesPath}.asset";
			T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (existingAsset != null)
				return existingAsset;

			if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
				throw new InvalidOperationException($"Another asset already exists at path: {assetPath}");

			EnsureFolder(assetPath.Substring(0, assetPath.LastIndexOf('/')));

			T instance = CreateInstance<T>();
			AssetDatabase.CreateAsset(instance, assetPath);
			AssetDatabase.SaveAssets();
			return instance;
		}

		static void EnsureFolder(string folderPath) {
			string[] folders = folderPath.Split('/');
			string currentPath = folders[0];
			for (int i = 1; i < folders.Length; i++) {
				string nextPath = $"{currentPath}/{folders[i]}";
				if (!AssetDatabase.IsValidFolder(nextPath))
					AssetDatabase.CreateFolder(currentPath, folders[i]);
				currentPath = nextPath;
			}
		}
#endif
	}
}