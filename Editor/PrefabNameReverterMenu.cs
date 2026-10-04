using UnityEditor;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Editor
{
	internal static class PrefabNameReverterMenu
	{
		[MenuItem("GameObject/Prefab/Revert Name", false, 0)]
		static void RevertName() {
			GameObject gameObject = Selection.activeGameObject;
			if (gameObject == null || !PrefabUtility.IsPartOfPrefabInstance(gameObject))
				return;

			SerializedObject serializedObject = new SerializedObject(gameObject);
			SerializedProperty nameProperty = serializedObject.FindProperty("m_Name");
			PrefabUtility.RevertPropertyOverride(nameProperty, InteractionMode.UserAction);
		}

		[MenuItem("GameObject/Prefab/Revert Name", true)]
		static bool ValidateRevertName() {
			GameObject gameObject = Selection.activeGameObject;
			return gameObject != null && PrefabUtility.IsPartOfPrefabInstance(gameObject);
		}
	}
}
