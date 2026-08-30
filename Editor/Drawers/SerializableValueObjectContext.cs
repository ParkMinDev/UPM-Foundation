using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ParkMinPackages.Foundation.Editor
{
	internal enum SerializableValueObjectContextKind
	{
		Asset,
		Scene,
		PrefabStage,
		PrefabAsset
	}

	internal sealed class SerializableValueObjectContext
	{
		// - Construct -
		SerializableValueObjectContext(SerializableValueObjectContextKind kind, Scene scene, PrefabStage prefabStage) {
			Kind = kind;
			_scene = scene;
			_prefabStage = prefabStage;
		}

		// - Public Methods -
		public static SerializableValueObjectContext Create(Object target) {
			GameObject targetGameObject = GetGameObject(target);
			if (targetGameObject == null)
				return new SerializableValueObjectContext(SerializableValueObjectContextKind.Asset, default, null);

			PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(targetGameObject) ?? PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null && prefabStage.scene.handle == targetGameObject.scene.handle && prefabStage.IsPartOfPrefabContents(targetGameObject))
				return new SerializableValueObjectContext(SerializableValueObjectContextKind.PrefabStage, prefabStage.scene, prefabStage);

			string prefabAssetPath = AssetDatabase.GetAssetPath(targetGameObject);
			if (string.IsNullOrEmpty(prefabAssetPath) == false && prefabAssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
				return new SerializableValueObjectContext(SerializableValueObjectContextKind.PrefabAsset, default, null);

			Scene scene = targetGameObject.scene;
			return scene.IsValid()
				? new SerializableValueObjectContext(SerializableValueObjectContextKind.Scene, scene, null)
				: new SerializableValueObjectContext(SerializableValueObjectContextKind.Asset, default, null);
		}

		public Component[] GetComponents(Type valueType) {
			IEnumerable<Component> components = Kind switch
			{
				SerializableValueObjectContextKind.Scene => GetSceneComponents(),
				SerializableValueObjectContextKind.PrefabStage => GetPrefabStageComponents(),
				_ => Array.Empty<Component>()
			};
			return components
				.Where(value => value != null && valueType.IsInstanceOfType(value))
				.Distinct()
				.ToArray();
		}

		public bool Allows(Object value, Type valueType) {
			if (value == null || valueType.IsInstanceOfType(value) == false)
				return false;
			if (value is ScriptableObject)
				return EditorUtility.IsPersistent(value);
			if (value is not Component component)
				return false;

			return Kind switch
			{
				SerializableValueObjectContextKind.Scene => EditorUtility.IsPersistent(component) == false && component.gameObject.scene.handle == _scene.handle,
				SerializableValueObjectContextKind.PrefabStage => _prefabStage != null && _prefabStage.IsPartOfPrefabContents(component.gameObject),
				_ => false
			};
		}

		// - Public Properties -
		public SerializableValueObjectContextKind Kind { get; }
		public string ComponentGroupName
		{
			get { return Kind is SerializableValueObjectContextKind.Scene ? "Hierarchy" : "Prefab"; }
		}
		public bool AllowsSceneObjects
		{
			get { return Kind is SerializableValueObjectContextKind.Scene or SerializableValueObjectContextKind.PrefabStage; }
		}

		// - Private & Protected -
		readonly Scene _scene;
		readonly PrefabStage _prefabStage;

		IEnumerable<Component> GetSceneComponents() {
			if (_scene.IsValid() == false || _scene.isLoaded == false)
				return Array.Empty<Component>();

			return _scene
				.GetRootGameObjects()
				.SelectMany(value => value.GetComponentsInChildren<Component>(true));
		}

		IEnumerable<Component> GetPrefabStageComponents() {
			if (_prefabStage == null || _prefabStage.prefabContentsRoot == null)
				return Array.Empty<Component>();

			return _prefabStage.prefabContentsRoot
				.GetComponentsInChildren<Component>(true)
				.Where(value => value != null && _prefabStage.IsPartOfPrefabContents(value.gameObject));
		}

		static GameObject GetGameObject(Object target) {
			if (target is Component component)
				return component.gameObject;
			if (target is GameObject gameObject)
				return gameObject;

			return null;
		}
	}

	[InitializeOnLoad]
	internal static class SerializableValueObjectSearch
	{
		// - Statics -
		static readonly Dictionary<Type, ScriptableObject[]> AssetValues = new Dictionary<Type, ScriptableObject[]>();

		static SerializableValueObjectSearch() {
			EditorApplication.projectChanged += ClearCache;
		}

		public static ScriptableObject[] GetScriptableObjectAssets(Type valueType) {
			if (AssetValues.TryGetValue(valueType, out ScriptableObject[] values))
				return values.Where(value => value != null).ToArray();

			IEnumerable<Type> candidateTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>();
			if (IsScriptableObjectType(valueType))
				candidateTypes = new[] { valueType }.Concat(candidateTypes);
			values = candidateTypes
				.Where(type => IsScriptableObjectType(type) && valueType.IsAssignableFrom(type))
				.Distinct()
				.SelectMany(type => AssetDatabase.FindAssets($"t:{type.Name}"))
				.Distinct()
				.Select(AssetDatabase.GUIDToAssetPath)
				.SelectMany(AssetDatabase.LoadAllAssetsAtPath)
				.OfType<ScriptableObject>()
				.Where(value => valueType.IsInstanceOfType(value))
				.Distinct()
				.ToArray();
			AssetValues[valueType] = values;
			return values;
		}

		internal static void ClearCache() {
			AssetValues.Clear();
		}

		static bool IsScriptableObjectType(Type type) {
			return type != null && type.IsAbstract == false && type.IsGenericTypeDefinition == false && type.ContainsGenericParameters == false && typeof(ScriptableObject).IsAssignableFrom(type);
		}
	}
}
