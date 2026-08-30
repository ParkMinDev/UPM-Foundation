using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ParkMinPackages.Foundation.Objects.Datas;
using ParkMinPackages.Foundation.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ParkMinPackages.Foundation.Editor.Tests
{
	public sealed class SerializableValueTests
	{
		// - Statics -
		const string TemporaryFolderPath = "Assets/SerializableValueTestsTemp";
		const string PrefabPath = TemporaryFolderPath + "/SerializableValueTest.prefab";
		const string NestedPrefabPath = TemporaryFolderPath + "/SerializableValueNestedTest.prefab";
		const string VariantPrefabPath = TemporaryFolderPath + "/SerializableValueVariantTest.prefab";
		const string ScriptableObjectPath = TemporaryFolderPath + "/SerializableValueTest.asset";

		// - Public Methods -
		[SetUp]
		public void SetUp() {
			EnsureTemporaryFolder();
			Undo.ClearAll();
		}

		[TearDown]
		public void TearDown() {
			StageUtility.GoToMainStage();
			AssetDatabase.DeleteAsset(TemporaryFolderPath);
			SerializableValueObjectSearch.ClearCache();
			Undo.ClearAll();
		}

		[Test]
		public void ConcreteValueUsesInlineStorage() {
			SerializableValueTestData data = new SerializableValueTestData(10);
			SerializableValue<SerializableValueTestData> value = new SerializableValue<SerializableValueTestData>(data);

			Assert.That(GetSource(value), Is.EqualTo(SerializableValueSource.InlineValue));
			Assert.That(value.Status, Is.EqualTo(SerializableValueStatus.Valid));
			Assert.That(value.Value, Is.SameAs(data));
		}

		[Test]
		public void ManagedStorageIsPreservedWhenValueChanges() {
			SerializableValue<SerializableValueTestData> value = new SerializableValue<SerializableValueTestData>(new SerializableValueDerivedTestData(20));

			Assert.That(GetSource(value), Is.EqualTo(SerializableValueSource.ManagedReference));
			value.Value = new SerializableValueTestData(30);

			Assert.That(GetSource(value), Is.EqualTo(SerializableValueSource.ManagedReference));
			Assert.That(value.Value.Number, Is.EqualTo(30));
		}

		[Test]
		public void NullValueClearsStorage() {
			SerializableValue<SerializableValueTestData> value = new SerializableValue<SerializableValueTestData>(new SerializableValueTestData(10));

			value.Value = null;

			Assert.That(GetSource(value), Is.EqualTo(SerializableValueSource.None));
			Assert.That(value.Status, Is.EqualTo(SerializableValueStatus.Empty));
			Assert.That(value.Value, Is.Null);
		}

		[Test]
		public void NonSerializableValueIsRejected() {
			SerializableValue<SerializableValueNonSerializableTestData> value = new SerializableValue<SerializableValueNonSerializableTestData>();

			Assert.Throws<ArgumentException>(() => value.Value = new SerializableValueNonSerializableTestData());
		}

		[Test]
		public void CollectionValueIsRejected() {
			SerializableValue<List<int>> value = new SerializableValue<List<int>>();

			Assert.Throws<ArgumentException>(() => value.Value = new List<int>());
		}

		[Test]
		public void EmptyUnityObjectSourceReportsEmpty() {
			SerializableValue<ISerializableValueTest> value = new SerializableValue<ISerializableValueTest>();
			SetField(value, "_source", SerializableValueSource.UnityObject);

			Assert.That(value.Status, Is.EqualTo(SerializableValueStatus.Empty));
			Assert.That(value.Value, Is.Null);
		}

		[Test]
		public void EmptyManagedReferenceSourceReportsEmpty() {
			SerializableValue<ISerializableValueTest> value = new SerializableValue<ISerializableValueTest>();
			SetField(value, "_source", SerializableValueSource.ManagedReference);

			Assert.That(value.Status, Is.EqualTo(SerializableValueStatus.Empty));
			Assert.That(value.Value, Is.Null);
		}

		[Test]
		public void IncompatibleManagedValueReportsTypeMismatch() {
			SerializableValue<ISerializableValueTest> value = new SerializableValue<ISerializableValueTest>();
			SetField(value, "_source", SerializableValueSource.ManagedReference);
			SetField(value, "_managedReference", new SerializableValueUnrelatedTestData());

			Assert.That(value.Status, Is.EqualTo(SerializableValueStatus.TypeMismatch));
			Assert.Throws<InvalidCastException>(() => _ = value.Value);
		}

		[Test]
		public void DestroyedUnityObjectReportsMissingReference() {
			GameObject gameObject = new GameObject("Target");
			SerializableValueTestBehaviour behaviour = gameObject.AddComponent<SerializableValueTestBehaviour>();
			SerializableValue<ISerializableValueTest> value = new SerializableValue<ISerializableValueTest>(behaviour);

			Object.DestroyImmediate(gameObject);

			Assert.That(value.Status, Is.EqualTo(SerializableValueStatus.MissingReference));
			Assert.Throws<InvalidOperationException>(() => _ = value.Value);
		}

		[Test]
		public void PrefabRoundTripPreservesEveryStorageType() {
			SerializableValueTestScriptableObject asset = ScriptableObject.CreateInstance<SerializableValueTestScriptableObject>();
			asset.Number = 40;
			AssetDatabase.CreateAsset(asset, ScriptableObjectPath);

			GameObject root = new GameObject("Root");
			SerializableValueTestHost host = root.AddComponent<SerializableValueTestHost>();
			GameObject child = new GameObject("Child");
			child.transform.SetParent(root.transform);
			SerializableValueTestBehaviour behaviour = child.AddComponent<SerializableValueTestBehaviour>();
			behaviour.Number = 30;
			host.InlineValue.Value = new SerializableValueTestData(10);
			host.ManagedValue.Value = new SerializableValueTestData(20);
			host.ComponentValue.Value = behaviour;
			host.AssetValue.Value = asset;

			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);
			AssetDatabase.SaveAssets();

			GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			SerializableValueTestHost loadedHost = prefabRoot.GetComponent<SerializableValueTestHost>();

			Assert.That(GetSource(loadedHost.InlineValue), Is.EqualTo(SerializableValueSource.InlineValue));
			Assert.That(loadedHost.InlineValue.Value.Number, Is.EqualTo(10));
			Assert.That(GetSource(loadedHost.ManagedValue), Is.EqualTo(SerializableValueSource.ManagedReference));
			Assert.That(loadedHost.ManagedValue.Value.Number, Is.EqualTo(20));
			Assert.That(GetSource(loadedHost.ComponentValue), Is.EqualTo(SerializableValueSource.UnityObject));
			Assert.That(loadedHost.ComponentValue.Value.Number, Is.EqualTo(30));
			Assert.That(loadedHost.AssetValue.Value, Is.SameAs(asset));
		}

		[Test]
		public void SceneContextFindsOnlyComponentsInTheTargetScene() {
			Scene targetScene = EditorSceneManager.NewPreviewScene();
			Scene otherScene = EditorSceneManager.NewPreviewScene();
			try {
				GameObject targetRoot = new GameObject("TargetRoot");
				GameObject inactiveChild = new GameObject("InactiveChild");
				inactiveChild.transform.SetParent(targetRoot.transform);
				inactiveChild.SetActive(false);
				SerializableValueTestHost host = targetRoot.AddComponent<SerializableValueTestHost>();
				SerializableValueTestBehaviour included = inactiveChild.AddComponent<SerializableValueTestBehaviour>();
				GameObject otherRoot = new GameObject("OtherRoot");
				SerializableValueTestBehaviour excluded = otherRoot.AddComponent<SerializableValueTestBehaviour>();
				SceneManager.MoveGameObjectToScene(targetRoot, targetScene);
				SceneManager.MoveGameObjectToScene(otherRoot, otherScene);

				SerializableValueObjectContext context = SerializableValueObjectContext.Create(host);
				Component[] values = context.GetComponents(typeof(ISerializableValueTest));

				Assert.That(context.Kind, Is.EqualTo(SerializableValueObjectContextKind.Scene));
				CollectionAssert.Contains(values, included);
				CollectionAssert.DoesNotContain(values, excluded);
				Assert.That(context.Allows(included, typeof(ISerializableValueTest)), Is.True);
				Assert.That(context.Allows(excluded, typeof(ISerializableValueTest)), Is.False);
			}
			finally {
				EditorSceneManager.ClosePreviewScene(targetScene);
				EditorSceneManager.ClosePreviewScene(otherScene);
			}
		}

		[Test]
		public void PrefabAssetContextIsReadOnly() {
			GameObject root = CreatePrefabSource();
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);

			GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			SerializableValueTestHost host = prefabRoot.GetComponent<SerializableValueTestHost>();
			SerializableValueTestBehaviour expected = prefabRoot.GetComponentInChildren<SerializableValueTestBehaviour>(true);
			SerializableValueObjectContext context = SerializableValueObjectContext.Create(host);
			Component[] values = context.GetComponents(typeof(ISerializableValueTest));

			Assert.That(context.Kind, Is.EqualTo(SerializableValueObjectContextKind.PrefabAsset));
			Assert.That(values, Is.Empty);
			Assert.That(context.Allows(expected, typeof(ISerializableValueTest)), Is.False);
		}

		[Test]
		public void PrefabStageContextFindsOnlyPrefabContents() {
			GameObject root = CreatePrefabSource();
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);

			PrefabStage prefabStage = PrefabStageUtility.OpenPrefab(PrefabPath);
			SerializableValueTestHost host = prefabStage.prefabContentsRoot.GetComponent<SerializableValueTestHost>();
			SerializableValueTestBehaviour expected = prefabStage.prefabContentsRoot.GetComponentInChildren<SerializableValueTestBehaviour>(true);
			SerializableValueObjectContext context = SerializableValueObjectContext.Create(host);

			Assert.That(context.Kind, Is.EqualTo(SerializableValueObjectContextKind.PrefabStage));
			CollectionAssert.Contains(context.GetComponents(typeof(ISerializableValueTest)), expected);
			Assert.That(context.Allows(expected, typeof(ISerializableValueTest)), Is.True);
		}

		[Test]
		public void NestedAndVariantPrefabAssetContextsAreReadOnly() {
			GameObject root = CreatePrefabSource();
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);
			GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

			GameObject nestedRoot = new GameObject("NestedRoot");
			GameObject nestedInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
			nestedInstance.transform.SetParent(nestedRoot.transform);
			PrefabUtility.SaveAsPrefabAsset(nestedRoot, NestedPrefabPath);
			Object.DestroyImmediate(nestedRoot);

			GameObject variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
			PrefabUtility.SaveAsPrefabAsset(variantInstance, VariantPrefabPath);
			Object.DestroyImmediate(variantInstance);

			AssertPrefabAssetContext(NestedPrefabPath);
			AssertPrefabAssetContext(VariantPrefabPath);
		}

		[Test]
		public void ScriptableObjectSearchFindsMatchingAssets() {
			SerializableValueTestScriptableObject asset = ScriptableObject.CreateInstance<SerializableValueTestScriptableObject>();
			AssetDatabase.CreateAsset(asset, ScriptableObjectPath);
			SerializableValueObjectSearch.ClearCache();

			ScriptableObject[] values = SerializableValueObjectSearch.GetScriptableObjectAssets(typeof(ISerializableValueTest));

			CollectionAssert.Contains(values, asset);
			Assert.That(values.All(value => value is ISerializableValueTest), Is.True);
		}

		[Test]
		public void SerializedPropertyChangesSupportUndo() {
			GameObject gameObject = new GameObject("Host");
			SerializableValueTestHost host = gameObject.AddComponent<SerializableValueTestHost>();
			SerializedObject serializedObject = new SerializedObject(host);
			SerializedProperty sourceProperty = serializedObject.FindProperty("_inlineValue").FindPropertyRelative("_source");

			sourceProperty.intValue = (int)SerializableValueSource.InlineValue;
			serializedObject.ApplyModifiedProperties();
			Assert.That(GetSource(host.InlineValue), Is.EqualTo(SerializableValueSource.InlineValue));

			Undo.PerformUndo();
			Assert.That(GetSource(host.InlineValue), Is.EqualTo(SerializableValueSource.None));

			Object.DestroyImmediate(gameObject);
		}

		[Test]
		public void DrawerSourceChangesClearInactiveStorage() {
			GameObject gameObject = new GameObject("Host");
			try {
				SerializableValueTestHost host = gameObject.AddComponent<SerializableValueTestHost>();
				host.InlineValue.Value = new SerializableValueTestData(10);
				SerializedObject serializedObject = new SerializedObject(host);
				SerializedProperty property = serializedObject.FindProperty("_inlineValue");

				InvokeSetSource(SerializableValueSource.ManagedReference, typeof(SerializableValueTestData), property);
				serializedObject.ApplyModifiedProperties();
				SerializableValueTestData resetInlineValue = (SerializableValueTestData)property.FindPropertyRelative("_inlineValue").boxedValue;
				Assert.That(resetInlineValue.Number, Is.Zero);

				property.FindPropertyRelative("_managedReference").managedReferenceValue = new SerializableValueTestData(20);
				serializedObject.ApplyModifiedProperties();
				InvokeSetSource(SerializableValueSource.InlineValue, typeof(SerializableValueTestData), property);
				serializedObject.ApplyModifiedProperties();

				Assert.That(GetSource(host.InlineValue), Is.EqualTo(SerializableValueSource.InlineValue));
				Assert.That(host.InlineValue.Value, Is.Not.Null);
				Assert.That(property.FindPropertyRelative("_managedReference").managedReferenceValue, Is.Null);
			}
			finally {
				Object.DestroyImmediate(gameObject);
			}
		}

		[Test]
		public void ManagedTypeWithOnlyParameterizedConstructorCanBeCreated() {
			MethodInfo getTypesMethod = typeof(SerializableValueDrawer).GetMethod("GetManagedReferenceTypes", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo createMethod = typeof(SerializableValueDrawer).GetMethod("CreateSerializedInstance", BindingFlags.Static | BindingFlags.NonPublic);
			Type[] types = (Type[])getTypesMethod.Invoke(null, new object[] { typeof(ISerializableValueTest) });

			CollectionAssert.Contains(types, typeof(SerializableValueParameterizedTestData));
			Assert.That(createMethod.Invoke(null, new object[] { typeof(SerializableValueParameterizedTestData) }), Is.TypeOf<SerializableValueParameterizedTestData>());
		}

		[Test]
		public void MissingManagedTypeCanBeCleared() {
			GameObject root = new GameObject("Root");
			SerializableValueTestHost host = root.AddComponent<SerializableValueTestHost>();
			host.ManagedValue.Value = new SerializableValueTestData(20);
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);

			string prefabFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PrefabPath));
			string prefabContents = File.ReadAllText(prefabFilePath);
			string missingTypeContents = prefabContents.Replace("class: SerializableValueTestData", "class: MissingSerializableValueTestData");
			Assert.That(missingTypeContents, Is.Not.EqualTo(prefabContents));
			File.WriteAllText(prefabFilePath, missingTypeContents);
			AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);

			GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			SerializableValueTestHost loadedHost = prefabRoot.GetComponent<SerializableValueTestHost>();
			SerializedObject serializedObject = new SerializedObject(loadedHost);
			SerializedProperty property = serializedObject.FindProperty("_managedValue");
			SerializedProperty managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			Assert.That(managedReferenceProperty.managedReferenceValue, Is.Null);
			Assert.That(PrefabUtility.HasManagedReferencesWithMissingTypes(loadedHost), Is.True);

			InvokeClearValue(property, typeof(ISerializableValueTest));
			PrefabUtility.SavePrefabAsset(prefabRoot);
			AssetDatabase.SaveAssets();

			Assert.That(GetSource(loadedHost.ManagedValue), Is.EqualTo(SerializableValueSource.None));
			Assert.That(PrefabUtility.HasManagedReferencesWithMissingTypes(loadedHost), Is.False);
			Assert.That(File.ReadAllText(prefabFilePath), Does.Not.Contain("MissingSerializableValueTestData"));
		}

		// - Private & Protected -
		static SerializableValueSource GetSource<T>(SerializableValue<T> value) where T : class {
			FieldInfo sourceField = typeof(SerializableValue<T>).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);
			return (SerializableValueSource)sourceField.GetValue(value);
		}

		static void SetField<T>(SerializableValue<T> value, string fieldName, object fieldValue) where T : class {
			FieldInfo field = typeof(SerializableValue<T>).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			field.SetValue(value, fieldValue);
		}

		static void InvokeSetSource(SerializableValueSource source, Type valueType, SerializedProperty property) {
			MethodInfo method = typeof(SerializableValueDrawer).GetMethod("SetSource", BindingFlags.Static | BindingFlags.NonPublic);
			method.Invoke(
				null,
				new object[]
				{
					source,
					valueType,
					property.FindPropertyRelative("_source"),
					property.FindPropertyRelative("_inlineValue"),
					property.FindPropertyRelative("_unityObject"),
					property.FindPropertyRelative("_managedReference")
				}
			);
		}

		static void InvokeClearValue(SerializedProperty property, Type valueType) {
			MethodInfo method = typeof(SerializableValueDrawer).GetMethod("ClearValue", BindingFlags.Static | BindingFlags.NonPublic);
			method.Invoke(null, new object[] { property, valueType });
		}

		static void AssertPrefabAssetContext(string prefabPath) {
			GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			SerializableValueTestBehaviour expected = prefabRoot.GetComponentInChildren<SerializableValueTestBehaviour>(true);
			SerializableValueObjectContext context = SerializableValueObjectContext.Create(prefabRoot.transform);

			Assert.That(context.Kind, Is.EqualTo(SerializableValueObjectContextKind.PrefabAsset));
			Assert.That(context.GetComponents(typeof(ISerializableValueTest)), Is.Empty);
			Assert.That(context.Allows(expected, typeof(ISerializableValueTest)), Is.False);
		}

		static GameObject CreatePrefabSource() {
			GameObject root = new GameObject("Root");
			root.AddComponent<SerializableValueTestHost>();
			GameObject child = new GameObject("Child");
			child.transform.SetParent(root.transform);
			child.SetActive(false);
			child.AddComponent<SerializableValueTestBehaviour>();
			return root;
		}

		static void EnsureTemporaryFolder() {
			if (AssetDatabase.IsValidFolder(TemporaryFolderPath) == false)
				AssetDatabase.CreateFolder("Assets", "SerializableValueTestsTemp");
		}
	}
}
