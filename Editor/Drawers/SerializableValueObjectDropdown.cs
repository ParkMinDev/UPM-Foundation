using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ParkMinDev.UPM.Foundation.Editor
{
	internal sealed class SerializableValueObjectDropdown : AdvancedDropdown
	{
		// - Class Struct Enum -
		sealed class ObjectItem : AdvancedDropdownItem
		{
			// - Construct -
			public ObjectItem(string name, Object value) : base(name) {
				Value = value;
			}

			// - Public Properties -
			public Object Value { get; }
		}

		// - Construct -
		public SerializableValueObjectDropdown(AdvancedDropdownState state, Type valueType, SerializableValueObjectContext objectContext, Action<Object> selectionHandler) : base(state) {
			_valueType = valueType;
			_objectContext = objectContext;
			_selectionHandler = selectionHandler;
			minimumSize = new Vector2(360f, 320f);
		}

		// - Private & Protected -
		protected override AdvancedDropdownItem BuildRoot() {
			AdvancedDropdownItem root = new AdvancedDropdownItem(ObjectNames.NicifyVariableName(_valueType.Name));
			root.AddChild(new ObjectItem("None", null));

			Component[] componentValues = _objectContext
				.GetComponents(_valueType)
				.OrderBy(GetComponentValueName)
				.ToArray();
			AddValues(root, _objectContext.ComponentGroupName, componentValues.Cast<Object>(), GetComponentValueName);

			ScriptableObject[] assetValues = SerializableValueObjectSearch
				.GetScriptableObjectAssets(_valueType)
				.OrderBy(GetAssetValueName)
				.ToArray();
			AddValues(root, "Assets", assetValues.Cast<Object>(), GetAssetValueName);

			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item) {
			if (item is ObjectItem objectItem)
				_selectionHandler(objectItem.Value);
		}

		readonly Type _valueType;
		readonly SerializableValueObjectContext _objectContext;
		readonly Action<Object> _selectionHandler;

		static void AddValues(AdvancedDropdownItem root, string groupName, IEnumerable<Object> values, Func<Object, string> nameSelector) {
			Object[] valueArray = values.ToArray();
			if (valueArray.Length == 0)
				return;

			AdvancedDropdownItem group = new AdvancedDropdownItem(groupName);
			foreach (Object value in valueArray)
				group.AddChild(new ObjectItem(nameSelector(value), value));

			root.AddChild(group);
		}

		string GetComponentValueName(Object value) {
			Component component = (Component)value;
			Stack<string> hierarchy = new Stack<string>();
			Transform current = component.transform;

			while (current != null) {
				hierarchy.Push(current.name);
				current = current.parent;
			}

			string prefix = _objectContext.Kind == SerializableValueObjectContextKind.Scene
				? $"{component.gameObject.scene.name}/"
				: string.Empty;
			Component[] matchingComponents = component.gameObject.GetComponents(component.GetType());
			int componentIndex = Array.IndexOf(matchingComponents, component);
			string indexSuffix = matchingComponents.Length > 1 ? $" #{componentIndex + 1}" : string.Empty;
			return $"{prefix}{string.Join("/", hierarchy)} ({ObjectNames.NicifyVariableName(component.GetType().Name)}{indexSuffix})";
		}

		static string GetAssetValueName(Object value) {
			string assetPath = AssetDatabase.GetAssetPath(value);
			return $"{assetPath}/{value.name} ({ObjectNames.NicifyVariableName(value.GetType().Name)})";
		}
	}
}
