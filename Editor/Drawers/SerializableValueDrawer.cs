using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ParkMinPackages.Foundation.Objects.Datas;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ParkMinPackages.Foundation.Editor
{
	[CustomPropertyDrawer(typeof(SerializableValue<>), true)]
	public sealed class SerializableValueDrawer : PropertyDrawer
	{
		// - Statics -
		static readonly GUIContent[] InterfaceSourceOptions =
		{
			new GUIContent("None"),
			new GUIContent("Unity Object"),
			new GUIContent("Managed Reference")
		};
		static readonly SerializableValueSource[] InterfaceSourceOptionValues =
		{
			SerializableValueSource.None,
			SerializableValueSource.UnityObject,
			SerializableValueSource.ManagedReference
		};
		static readonly GUIContent[] ClassSourceOptions =
		{
			new GUIContent("None"),
			new GUIContent("Inline Value"),
			new GUIContent("Managed Reference")
		};
		static readonly SerializableValueSource[] ClassSourceOptionValues =
		{
			SerializableValueSource.None,
			SerializableValueSource.InlineValue,
			SerializableValueSource.ManagedReference
		};
		static readonly GUIContent[] AbstractClassSourceOptions =
		{
			new GUIContent("None"),
			new GUIContent("Managed Reference")
		};
		static readonly SerializableValueSource[] AbstractClassSourceOptionValues =
		{
			SerializableValueSource.None,
			SerializableValueSource.ManagedReference
		};
		static readonly Dictionary<Type, Type[]> ManagedReferenceTypes = new Dictionary<Type, Type[]>();

		const float FieldSpacing = 2f;
		const float SourceWidth = 110f;

		// - Public Methods -
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (property.serializedObject.isEditingMultipleObjects) {
				EditorGUI.HelpBox(position, "SerializableValue does not support multi-object editing.", MessageType.Info);
				return;
			}

			SerializedProperty sourceProperty = property.FindPropertyRelative("_source");
			SerializedProperty inlineValueProperty = property.FindPropertyRelative("_inlineValue");
			SerializedProperty unityObjectProperty = property.FindPropertyRelative("_unityObject");
			SerializedProperty managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			Type valueType = GetValueType();
			SerializableValueObjectContext objectContext = SerializableValueObjectContext.Create(property.serializedObject.targetObject);

			EditorGUI.BeginProperty(position, label, property);

			if (objectContext.Kind == SerializableValueObjectContextKind.PrefabAsset) {
				DrawPrefabAssetMessage(position, property.serializedObject.targetObject, label);
			}
			else if (sourceProperty == null || unityObjectProperty == null || managedReferenceProperty == null || valueType == null || CanUseInlineValue(valueType) && inlineValueProperty == null) {
				EditorGUI.HelpBox(position, $"{label.text} cannot be drawn as a SerializableValue.", MessageType.Error);
			}
			else if (IsSupportedDeclaredType(valueType) == false) {
				EditorGUI.HelpBox(position, $"{valueType.Name} cannot be stored as a SerializableValue.", MessageType.Error);
			}
			else if (GetStatus(property.serializedObject.targetObject, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty) is SerializableValueStatus.MissingReference or SerializableValueStatus.TypeMismatch) {
				DrawInvalidValue(position, property, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);
			}
			else if (typeof(Object).IsAssignableFrom(valueType)) {
				DrawUnityObject(position, label, valueType, sourceProperty, unityObjectProperty, managedReferenceProperty, objectContext.AllowsSceneObjects);
			}
			else if (valueType.IsInterface) {
				DrawPolymorphic(position, property, label, valueType, sourceProperty, unityObjectProperty, managedReferenceProperty, objectContext);
			}
			else {
				DrawClassValue(position, property, label, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.serializedObject.isEditingMultipleObjects)
				return EditorGUIUtility.singleLineHeight * 2f;
			if (SerializableValueObjectContext.Create(property.serializedObject.targetObject).Kind == SerializableValueObjectContextKind.PrefabAsset)
				return EditorGUIUtility.singleLineHeight * 3f + FieldSpacing;

			SerializedProperty sourceProperty = property.FindPropertyRelative("_source");
			SerializedProperty inlineValueProperty = property.FindPropertyRelative("_inlineValue");
			SerializedProperty unityObjectProperty = property.FindPropertyRelative("_unityObject");
			SerializedProperty managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			Type valueType = GetValueType();

			if (sourceProperty == null || unityObjectProperty == null || managedReferenceProperty == null || valueType == null || CanUseInlineValue(valueType) && inlineValueProperty == null)
				return EditorGUIUtility.singleLineHeight;
			if (IsSupportedDeclaredType(valueType) == false)
				return EditorGUIUtility.singleLineHeight;
			if (GetStatus(property.serializedObject.targetObject, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty) is SerializableValueStatus.MissingReference or SerializableValueStatus.TypeMismatch)
				return EditorGUIUtility.singleLineHeight * 3f + FieldSpacing;
			if (typeof(Object).IsAssignableFrom(valueType))
				return EditorGUIUtility.singleLineHeight;

			SerializableValueSource source = GetSource(sourceProperty);
			if (valueType.IsInterface && source != SerializableValueSource.ManagedReference)
				return EditorGUIUtility.singleLineHeight;
			if (source == SerializableValueSource.InlineValue)
				return GetSerializedValueHeight(property, inlineValueProperty);

			return GetSerializedValueHeight(property, managedReferenceProperty);
		}

		// - Private & Protected -
		Type GetValueType() {
			if (fieldInfo == null)
				return null;

			return GetValueType(fieldInfo.FieldType);
		}

		static Type GetValueType(Type fieldType) {
			if (fieldType == null)
				return null;
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(SerializableValue<>))
				return fieldType.GetGenericArguments()[0];
			if (fieldType.IsArray)
				return GetValueType(fieldType.GetElementType());
			if (fieldType.IsGenericType)
				return GetValueType(fieldType.GetGenericArguments()[0]);

			return null;
		}

		static bool IsSupportedDeclaredType(Type valueType) {
			if (valueType == null || valueType.IsValueType || valueType == typeof(string))
				return false;
			if (typeof(Delegate).IsAssignableFrom(valueType) || typeof(ICollection).IsAssignableFrom(valueType))
				return false;
			if (valueType.IsGenericType && valueType.IsInterface == false && valueType.IsAbstract == false)
				return false;
			if (typeof(Object).IsAssignableFrom(valueType) || valueType.IsInterface || valueType.IsAbstract)
				return true;

			return valueType.GetCustomAttribute<SerializableAttribute>() != null;
		}

		static bool CanUseInlineValue(Type valueType) {
			return valueType != null && valueType.IsInterface == false && valueType.IsAbstract == false && typeof(Object).IsAssignableFrom(valueType) == false;
		}

		static bool HasMissingPrefabReferences(Object targetObject) {
			return targetObject != null && EditorUtility.IsPersistent(targetObject) && PrefabUtility.IsPartOfPrefabAsset(targetObject) && PrefabUtility.HasManagedReferencesWithMissingTypes(targetObject);
		}

		static SerializableValueStatus GetStatus(
			Object targetObject,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty inlineValueProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty
		) {
			switch (GetSource(sourceProperty)) {
				case SerializableValueSource.None:
					return SerializableValueStatus.Empty;
				case SerializableValueSource.InlineValue:
					if (inlineValueProperty == null)
						return SerializableValueStatus.TypeMismatch;

					object inlineValue = inlineValueProperty.boxedValue;
					if (inlineValue == null)
						return SerializableValueStatus.Empty;

					return valueType.IsInstanceOfType(inlineValue)
						? SerializableValueStatus.Valid
						: SerializableValueStatus.TypeMismatch;
				case SerializableValueSource.UnityObject:
					Object unityObject = unityObjectProperty.objectReferenceValue;
					if (unityObject == null)
						return unityObjectProperty.objectReferenceEntityIdValue == EntityId.None
							? SerializableValueStatus.Empty
							: SerializableValueStatus.MissingReference;

					return valueType.IsInstanceOfType(unityObject)
						? SerializableValueStatus.Valid
						: SerializableValueStatus.TypeMismatch;
				case SerializableValueSource.ManagedReference:
					object managedReference = managedReferenceProperty.managedReferenceValue;
					if (managedReference == null)
						return managedReferenceProperty.managedReferenceId >= 0 || HasMissingPrefabReferences(targetObject)
							? SerializableValueStatus.MissingReference
							: SerializableValueStatus.Empty;

					return valueType.IsInstanceOfType(managedReference)
						? SerializableValueStatus.Valid
						: SerializableValueStatus.TypeMismatch;
				default:
					return SerializableValueStatus.TypeMismatch;
			}
		}

		static void DrawInvalidValue(
			Rect position,
			SerializedProperty property,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty inlineValueProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty
		) {
			Rect helpBoxPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2f);
			Rect buttonPosition = new Rect(position.x, helpBoxPosition.yMax + FieldSpacing, position.width, EditorGUIUtility.singleLineHeight);
			SerializableValueStatus status = GetStatus(property.serializedObject.targetObject, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);
			string message = GetInvalidValueMessage(valueType, status, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);

			EditorGUI.HelpBox(helpBoxPosition, message, MessageType.Error);
			if (GUI.Button(buttonPosition, "Clear Invalid Value"))
				ClearValue(property, valueType);
		}

		static string GetInvalidValueMessage(
			Type valueType,
			SerializableValueStatus status,
			SerializedProperty sourceProperty,
			SerializedProperty inlineValueProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty
		) {
			if (status == SerializableValueStatus.MissingReference) {
				if (GetSource(sourceProperty) == SerializableValueSource.ManagedReference) {
					string missingTypeName = managedReferenceProperty.managedReferenceFullTypename;
					return string.IsNullOrEmpty(missingTypeName)
						? $"{valueType.Name} contains a missing managed reference type."
						: $"{valueType.Name} contains a missing managed type: {missingTypeName}.";
				}

				return $"{valueType.Name} contains a missing Unity object reference.";
			}

			object inlineValue = inlineValueProperty?.boxedValue;
			Object unityObject = unityObjectProperty.objectReferenceValue;
			object managedReference = managedReferenceProperty.managedReferenceValue;
			string actualTypeName = GetSource(sourceProperty) switch
			{
				SerializableValueSource.InlineValue => inlineValue?.GetType().FullName,
				SerializableValueSource.UnityObject => unityObject?.GetType().FullName,
				SerializableValueSource.ManagedReference => managedReference?.GetType().FullName,
				_ => null
			};
			return $"Stored type {actualTypeName ?? "Unknown"} cannot be assigned to {valueType.FullName}.";
		}

		static void ClearValue(SerializedProperty property, Type valueType) {
			SerializedObject serializedObject = property.serializedObject;
			SerializedProperty sourceProperty = property.FindPropertyRelative("_source");
			SerializedProperty inlineValueProperty = property.FindPropertyRelative("_inlineValue");
			SerializedProperty unityObjectProperty = property.FindPropertyRelative("_unityObject");
			SerializedProperty managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			bool hasMissingPrefabReference = HasMissingPrefabReferences(serializedObject.targetObject);
			if (GetSource(sourceProperty) == SerializableValueSource.ManagedReference && managedReferenceProperty.managedReferenceValue == null && (managedReferenceProperty.managedReferenceId >= 0 || hasMissingPrefabReference)) {
				long managedReferenceId = managedReferenceProperty.managedReferenceId;
				Object targetObject = serializedObject.targetObject;
				Undo.RecordObject(targetObject, "Clear Invalid Serializable Value");
				if (managedReferenceId >= 0)
					SerializationUtility.ClearManagedReferenceWithMissingType(targetObject, managedReferenceId);
				else
					SerializationUtility.ClearAllManagedReferencesWithMissingTypes(targetObject);
				serializedObject.Update();
				property = serializedObject.FindProperty(property.propertyPath);
				sourceProperty = property.FindPropertyRelative("_source");
				inlineValueProperty = property.FindPropertyRelative("_inlineValue");
				unityObjectProperty = property.FindPropertyRelative("_unityObject");
				managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			}

			SetSource(SerializableValueSource.None, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);
			property.isExpanded = false;
			serializedObject.ApplyModifiedProperties();
		}

		static void DrawUnityObject(
			Rect position,
			GUIContent label,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty,
			bool allowSceneObjects
		) {
			Object currentValue = GetSource(sourceProperty) == SerializableValueSource.UnityObject ? unityObjectProperty.objectReferenceValue : null;
			EditorGUI.BeginChangeCheck();
			Object selectedValue = EditorGUI.ObjectField(position, label, currentValue, valueType, allowSceneObjects);
			if (EditorGUI.EndChangeCheck() == false)
				return;

			unityObjectProperty.objectReferenceValue = selectedValue;
			managedReferenceProperty.managedReferenceValue = null;
			sourceProperty.intValue = selectedValue == null ? (int)SerializableValueSource.None : (int)SerializableValueSource.UnityObject;
		}

		static void DrawPolymorphic(
			Rect position,
			SerializedProperty property,
			GUIContent label,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty,
			SerializableValueObjectContext objectContext
		) {
			Rect linePosition = GetLinePosition(position);
			SerializableValueSource source = GetSource(sourceProperty);
			bool isExpandable = source == SerializableValueSource.ManagedReference && managedReferenceProperty.managedReferenceValue != null;
			Rect labelPosition = new Rect(linePosition.x, linePosition.y, EditorGUIUtility.labelWidth, linePosition.height);
			Rect contentPosition = new Rect(labelPosition.xMax, linePosition.y, Mathf.Max(0f, linePosition.xMax - labelPosition.xMax), linePosition.height);

			if (isExpandable) {
				property.isExpanded = EditorGUI.Foldout(labelPosition, property.isExpanded, label, true);
			}
			else {
				EditorGUI.LabelField(labelPosition, label);
			}

			float sourceWidth = Mathf.Min(SourceWidth, contentPosition.width * 0.45f);
			Rect sourcePosition = new Rect(contentPosition.x, contentPosition.y, sourceWidth, contentPosition.height);
			Rect fieldPosition = new Rect(sourcePosition.xMax + FieldSpacing, contentPosition.y, Mathf.Max(0f, contentPosition.xMax - sourcePosition.xMax - FieldSpacing), contentPosition.height);
			int sourceIndex = GetSourceOptionIndex(source, InterfaceSourceOptionValues);

			EditorGUI.BeginChangeCheck();
			int selectedSourceIndex = EditorGUI.Popup(sourcePosition, sourceIndex, InterfaceSourceOptions);
			if (EditorGUI.EndChangeCheck()) {
				source = InterfaceSourceOptionValues[selectedSourceIndex];
				SetSource(source, valueType, sourceProperty, null, unityObjectProperty, managedReferenceProperty);
			}

			switch (source) {
				case SerializableValueSource.None:
					EditorGUI.LabelField(fieldPosition, GUIContent.none, InterfaceSourceOptions[0]);
					break;
				case SerializableValueSource.UnityObject:
					DrawInterfaceObject(fieldPosition, property, valueType, sourceProperty, unityObjectProperty, managedReferenceProperty, objectContext);
					break;
				case SerializableValueSource.ManagedReference:
					DrawManagedReferenceType(fieldPosition, property, valueType, sourceProperty, unityObjectProperty, managedReferenceProperty);
					DrawSerializedValueChildren(position, property, managedReferenceProperty);
					break;
				default:
					SetSource(SerializableValueSource.None, valueType, sourceProperty, null, unityObjectProperty, managedReferenceProperty);
					break;
			}
		}

		static void DrawClassValue(
			Rect position,
			SerializedProperty property,
			GUIContent label,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty inlineValueProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty
		) {
			Rect linePosition = GetLinePosition(position);
			SerializableValueSource source = GetSource(sourceProperty);
			bool isExpandable = source == SerializableValueSource.InlineValue && inlineValueProperty.hasVisibleChildren ||
			                    source == SerializableValueSource.ManagedReference && managedReferenceProperty.managedReferenceValue != null;
			Rect labelPosition = new Rect(linePosition.x, linePosition.y, EditorGUIUtility.labelWidth, linePosition.height);
			Rect contentPosition = new Rect(labelPosition.xMax, linePosition.y, Mathf.Max(0f, linePosition.xMax - labelPosition.xMax), linePosition.height);

			if (isExpandable) {
				property.isExpanded = EditorGUI.Foldout(labelPosition, property.isExpanded, label, true);
			}
			else {
				EditorGUI.LabelField(labelPosition, label);
			}

			GUIContent[] sourceOptions = valueType.IsAbstract ? AbstractClassSourceOptions : ClassSourceOptions;
			SerializableValueSource[] sourceOptionValues = valueType.IsAbstract ? AbstractClassSourceOptionValues : ClassSourceOptionValues;
			float sourceWidth = Mathf.Min(SourceWidth, contentPosition.width * 0.45f);
			Rect sourcePosition = new Rect(contentPosition.x, contentPosition.y, sourceWidth, contentPosition.height);
			Rect fieldPosition = new Rect(sourcePosition.xMax + FieldSpacing, contentPosition.y, Mathf.Max(0f, contentPosition.xMax - sourcePosition.xMax - FieldSpacing), contentPosition.height);
			int sourceIndex = GetSourceOptionIndex(source, sourceOptionValues);

			EditorGUI.BeginChangeCheck();
			int selectedSourceIndex = EditorGUI.Popup(sourcePosition, sourceIndex, sourceOptions);
			if (EditorGUI.EndChangeCheck()) {
				source = sourceOptionValues[selectedSourceIndex];
				SetSource(source, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);
			}

			switch (source) {
				case SerializableValueSource.None:
					EditorGUI.LabelField(fieldPosition, GUIContent.none, sourceOptions[0]);
					break;
				case SerializableValueSource.InlineValue when valueType.IsAbstract == false:
					EditorGUI.LabelField(fieldPosition, "Inline Value");
					DrawSerializedValueChildren(position, property, inlineValueProperty);
					break;
				case SerializableValueSource.ManagedReference:
					DrawManagedReferenceType(fieldPosition, property, valueType, sourceProperty, unityObjectProperty, managedReferenceProperty);
					DrawSerializedValueChildren(position, property, managedReferenceProperty);
					break;
				default:
					SetSource(SerializableValueSource.None, valueType, sourceProperty, inlineValueProperty, unityObjectProperty, managedReferenceProperty);
					break;
			}
		}

		static void DrawInterfaceObject(
			Rect position,
			SerializedProperty property,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty,
			SerializableValueObjectContext objectContext
		) {
			Object currentValue = unityObjectProperty.objectReferenceValue;
			if (currentValue != null) {
				EditorGUI.BeginChangeCheck();
				Object selectedValue = EditorGUI.ObjectField(position, GUIContent.none, currentValue, currentValue.GetType(), objectContext.AllowsSceneObjects);
				if (EditorGUI.EndChangeCheck()) {
					Object resolvedValue = ResolveUnityObject(selectedValue, valueType);
					if (selectedValue == null || objectContext.Allows(resolvedValue, valueType))
						SetUnityObject(property.serializedObject, property.propertyPath, valueType, resolvedValue);
				}
			}
			else {
				GUIContent buttonContent = new GUIContent($"{ObjectNames.NicifyVariableName(valueType.Name)}: None");
				if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard, EditorStyles.objectField)) {
					SerializedObject serializedObject = property.serializedObject;
					string propertyPath = property.propertyPath;
					SerializableValueObjectDropdown dropdown = new SerializableValueObjectDropdown(
						new AdvancedDropdownState(),
						valueType,
						objectContext,
						value => SetUnityObject(serializedObject, propertyPath, valueType, value)
					);
					dropdown.Show(position);
				}
			}

			HandleInterfaceObjectDrag(position, property, valueType, objectContext);
		}

		static Object ResolveUnityObject(Object value, Type valueType) {
			if (value == null || valueType.IsInstanceOfType(value))
				return value;

			GameObject gameObject = value as GameObject;
			if (gameObject == null && value is Component component)
				gameObject = component.gameObject;
			if (gameObject == null)
				return null;

			Component[] components = gameObject.GetComponents<Component>();
			return components.FirstOrDefault(component => component != null && valueType.IsInstanceOfType(component));
		}

		static void HandleInterfaceObjectDrag(Rect position, SerializedProperty property, Type valueType, SerializableValueObjectContext objectContext) {
			Event currentEvent = Event.current;
			if (position.Contains(currentEvent.mousePosition) == false || currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform)
				return;

			Object resolvedValue = DragAndDrop.objectReferences
				.Select(value => ResolveUnityObject(value, valueType))
				.FirstOrDefault(value => objectContext.Allows(value, valueType));
			if (resolvedValue == null)
				return;

			DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			if (currentEvent.type == EventType.DragPerform) {
				DragAndDrop.AcceptDrag();
				SetUnityObject(property.serializedObject, property.propertyPath, valueType, resolvedValue);
			}
			currentEvent.Use();
		}

		static void SetUnityObject(SerializedObject serializedObject, string propertyPath, Type valueType, Object value) {
			serializedObject.Update();
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
				return;

			SerializedProperty sourceProperty = property.FindPropertyRelative("_source");
			SerializedProperty inlineValueProperty = property.FindPropertyRelative("_inlineValue");
			SerializedProperty unityObjectProperty = property.FindPropertyRelative("_unityObject");
			SerializedProperty managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			if (sourceProperty == null || unityObjectProperty == null || managedReferenceProperty == null)
				return;

			unityObjectProperty.objectReferenceValue = value;
			ResetInlineValue(inlineValueProperty, valueType);
			managedReferenceProperty.managedReferenceValue = null;
			sourceProperty.intValue = value == null ? (int)SerializableValueSource.None : (int)SerializableValueSource.UnityObject;
			property.isExpanded = false;
			serializedObject.ApplyModifiedProperties();
		}

		static void DrawManagedReferenceType(
			Rect position,
			SerializedProperty property,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty
		) {
			Type currentType = GetSource(sourceProperty) == SerializableValueSource.ManagedReference && managedReferenceProperty.hasMultipleDifferentValues == false
				? managedReferenceProperty.managedReferenceValue?.GetType()
				: null;
			string buttonText = currentType == null ? "None" : ObjectNames.NicifyVariableName(currentType.Name);

			if (GUI.Button(position, buttonText, EditorStyles.popup))
				ShowManagedReferenceMenu(position, property, valueType, currentType);
		}

		static void ShowManagedReferenceMenu(Rect position, SerializedProperty property, Type valueType, Type currentType) {
			GenericMenu menu = new GenericMenu();
			SerializedObject serializedObject = property.serializedObject;
			string propertyPath = property.propertyPath;

			menu.AddItem(
				new GUIContent("None"),
				currentType == null,
				() => SetManagedReference(serializedObject, propertyPath, valueType, null)
			);
			menu.AddSeparator(string.Empty);

			Type[] managedTypes = GetManagedReferenceTypes(valueType);
			foreach (Type managedType in managedTypes) {
				Type selectedType = managedType;
				string typeName = (managedType.FullName ?? managedType.Name).Replace('.', '/').Replace('+', '/');
				menu.AddItem(
					new GUIContent(typeName),
					currentType == managedType,
					() => SetManagedReference(serializedObject, propertyPath, valueType, selectedType)
				);
			}

			if (managedTypes.Length == 0)
				menu.AddDisabledItem(new GUIContent("No Serializable Type"));

			menu.DropDown(position);
		}

		static void SetManagedReference(SerializedObject serializedObject, string propertyPath, Type valueType, Type selectedType) {
			serializedObject.Update();
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
				return;

			SerializedProperty sourceProperty = property.FindPropertyRelative("_source");
			SerializedProperty inlineValueProperty = property.FindPropertyRelative("_inlineValue");
			SerializedProperty unityObjectProperty = property.FindPropertyRelative("_unityObject");
			SerializedProperty managedReferenceProperty = property.FindPropertyRelative("_managedReference");
			if (sourceProperty == null || unityObjectProperty == null || managedReferenceProperty == null)
				return;

			object managedReference = CreateSerializedInstance(selectedType);
			managedReferenceProperty.managedReferenceValue = managedReference;
			ResetInlineValue(inlineValueProperty, valueType);
			unityObjectProperty.objectReferenceValue = null;
			sourceProperty.intValue = managedReference == null ? (int)SerializableValueSource.None : (int)SerializableValueSource.ManagedReference;
			property.isExpanded = managedReference != null;
			serializedObject.ApplyModifiedProperties();
		}

		static Type[] GetManagedReferenceTypes(Type valueType) {
			if (ManagedReferenceTypes.TryGetValue(valueType, out Type[] managedTypes))
				return managedTypes;

			IEnumerable<Type> derivedTypes = TypeCache.GetTypesDerivedFrom(valueType);
			IEnumerable<Type> candidateTypes = IsManagedReferenceType(valueType)
				? new[] { valueType }.Concat(derivedTypes)
				: derivedTypes;
			managedTypes = candidateTypes
				.Where(IsManagedReferenceType)
				.Distinct()
				.OrderBy(type => type.FullName)
				.ToArray();
			ManagedReferenceTypes[valueType] = managedTypes;
			return managedTypes;
		}

		static bool IsManagedReferenceType(Type type) {
			if (type == null || type.IsClass == false || type.IsAbstract || type.IsGenericType)
				return false;
			if (type == typeof(string) || typeof(Object).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type) || typeof(ICollection).IsAssignableFrom(type))
				return false;
			return type.GetCustomAttribute<SerializableAttribute>() != null;
		}

		static void DrawPrefabAssetMessage(Rect position, Object targetObject, GUIContent label) {
			Rect helpBoxPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2f);
			Rect buttonPosition = new Rect(position.x, helpBoxPosition.yMax + FieldSpacing, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.HelpBox(helpBoxPosition, $"{label.text} can only be edited in Prefab Mode.", MessageType.Info);

			if (GUI.Button(buttonPosition, "Open Prefab") == false)
				return;

			string prefabPath = AssetDatabase.GetAssetPath(targetObject);
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			if (prefab != null)
				AssetDatabase.OpenAsset(prefab);
		}

		static object CreateSerializedInstance(Type type) {
			if (type == null)
				return null;

			try {
				ConstructorInfo constructor = type.GetConstructor(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					Type.EmptyTypes,
					null
				);
				return constructor == null
					? FormatterServices.GetUninitializedObject(type)
					: Activator.CreateInstance(type, true);
			}
			catch (Exception exception) {
				Debug.LogException(exception);
				return null;
			}
		}

		static void SetSource(
			SerializableValueSource source,
			Type valueType,
			SerializedProperty sourceProperty,
			SerializedProperty inlineValueProperty,
			SerializedProperty unityObjectProperty,
			SerializedProperty managedReferenceProperty
		) {
			if (source == SerializableValueSource.InlineValue && inlineValueProperty != null && inlineValueProperty.boxedValue == null) {
				object inlineValue = CreateSerializedInstance(valueType);
				if (inlineValue == null)
					source = SerializableValueSource.None;
				else
					inlineValueProperty.boxedValue = inlineValue;
			}

			sourceProperty.intValue = (int)source;

			if (source != SerializableValueSource.InlineValue)
				ResetInlineValue(inlineValueProperty, valueType);
			if (source != SerializableValueSource.UnityObject)
				unityObjectProperty.objectReferenceValue = null;
			if (source != SerializableValueSource.ManagedReference)
				managedReferenceProperty.managedReferenceValue = null;
		}

		static void ResetInlineValue(SerializedProperty inlineValueProperty, Type valueType) {
			if (inlineValueProperty == null || CanUseInlineValue(valueType) == false)
				return;

			object inlineValue = CreateSerializedInstance(valueType);
			if (inlineValue != null)
				inlineValueProperty.boxedValue = inlineValue;
		}

		static SerializableValueSource GetSource(SerializedProperty sourceProperty) {
			return (SerializableValueSource)sourceProperty.intValue;
		}

		static int GetSourceOptionIndex(SerializableValueSource source, SerializableValueSource[] sourceOptionValues) {
			for (int index = 0; index < sourceOptionValues.Length; index++) {
				if (sourceOptionValues[index] == source)
					return index;
			}

			return 0;
		}

		static Rect GetLinePosition(Rect position) {
			return new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
		}

		static float GetSerializedValueHeight(SerializedProperty property, SerializedProperty valueProperty) {
			float height = EditorGUIUtility.singleLineHeight;
			if (property.isExpanded == false || valueProperty.hasVisibleChildren == false)
				return height;

			SerializedProperty childProperty = valueProperty.Copy();
			SerializedProperty endProperty = childProperty.GetEndProperty();
			bool enterChildren = true;

			while (childProperty.NextVisible(enterChildren) && SerializedProperty.EqualContents(childProperty, endProperty) == false) {
				height += FieldSpacing + EditorGUI.GetPropertyHeight(childProperty, true);
				enterChildren = false;
			}

			return height;
		}

		static void DrawSerializedValueChildren(Rect position, SerializedProperty property, SerializedProperty valueProperty) {
			if (property.isExpanded == false || valueProperty.hasVisibleChildren == false)
				return;

			SerializedProperty childProperty = valueProperty.Copy();
			SerializedProperty endProperty = childProperty.GetEndProperty();
			bool enterChildren = true;
			float currentY = position.y + EditorGUIUtility.singleLineHeight + FieldSpacing;

			EditorGUI.indentLevel++;
			while (childProperty.NextVisible(enterChildren) && SerializedProperty.EqualContents(childProperty, endProperty) == false) {
				float propertyHeight = EditorGUI.GetPropertyHeight(childProperty, true);
				Rect childPosition = new Rect(position.x, currentY, position.width, propertyHeight);
				EditorGUI.PropertyField(childPosition, childProperty, true);
				currentY += propertyHeight + FieldSpacing;
				enterChildren = false;
			}
			EditorGUI.indentLevel--;
		}
	}
}
