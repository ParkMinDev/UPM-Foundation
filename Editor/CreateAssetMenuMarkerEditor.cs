using System;
using System.Linq;
using System.Reflection;
using ParkMinDev.UPM.Foundation.Attributes;
using UnityEditor;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Editor
{
	public static class CreateAssetMenuMarkerEditor
	{
		// - Statics -
		[MenuItem("Assets/Create/Project", false, 0)]
		public static void OpenCreateAssetMenu() {
			GenericMenu menu = new GenericMenu();
			Type[] markerInterfaceTypes = TypeCache
				.GetTypesWithAttribute<CreateAssetMenuMarkerAttribute>()
				.Where(type => type.IsInterface)
				.OrderBy(type => type.FullName)
				.ToArray();
			bool hasMenuItem = false;

			foreach (Type markerInterfaceType in markerInterfaceTypes) {
				CreateAssetMenuMarkerAttribute markerAttribute = markerInterfaceType.GetCustomAttribute<CreateAssetMenuMarkerAttribute>();
				string normalizedMarkerPath = string.Join(
					"/",
					(markerAttribute.MenuPath ?? string.Empty)
					.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(pathSegment => pathSegment.Trim())
					.Where(pathSegment => string.IsNullOrWhiteSpace(pathSegment) == false)
				);
				Type[] assetTypes = TypeCache
					.GetTypesDerivedFrom(markerInterfaceType)
					.Where(type =>
						type.IsAbstract == false &&
						type.IsGenericType == false &&
						type.IsSubclassOf(typeof(ScriptableObject))
					)
					.OrderBy(type => type.Name)
					.ToArray();

				foreach (Type assetType in assetTypes) {
					Type capturedAssetType = assetType;
					string assetMenuName = ObjectNames.NicifyVariableName(assetType.Name);
					string menuPath = string.IsNullOrWhiteSpace(normalizedMarkerPath)
						? assetMenuName
						: $"{normalizedMarkerPath}/{assetMenuName}";

					menu.AddItem(
						new GUIContent(menuPath),
						false,
						() => {
							ScriptableObject asset = ScriptableObject.CreateInstance(capturedAssetType);
							ProjectWindowUtil.CreateAsset(asset, $"New {capturedAssetType.Name}.asset");
						}
					);
					hasMenuItem = true;
				}
			}

			if (hasMenuItem == false) {
				menu.AddDisabledItem(new GUIContent("No Marked ScriptableObject Types"));
			}
			Vector2 mouseScreenPosition = GetMouseScreenPosition();
			Rect menuPosition = new Rect(mouseScreenPosition, Vector2.zero);
			EditorApplication.delayCall += () => DropDownScreenSpaceMethod.Invoke(
				menu,
				new object[] { menuPosition, false }
			);
		}

		private static readonly MethodInfo GetCurrentMousePositionMethod = typeof(UnityEditor.Editor).GetMethod(
			"GetCurrentMousePosition",
			BindingFlags.Static | BindingFlags.NonPublic
		);
		private static readonly MethodInfo DropDownScreenSpaceMethod = typeof(GenericMenu).GetMethod(
			"DropDownScreenSpace",
			BindingFlags.Instance | BindingFlags.NonPublic,
			null,
			new Type[] { typeof(Rect), typeof(bool) },
			null
		);

		private static Vector2 GetMouseScreenPosition() {
			if (GetCurrentMousePositionMethod?.Invoke(null, null) is Vector2 mousePosition) {
				return mousePosition;
			}
			EditorWindow targetWindow = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
			return targetWindow == null ? new Vector2(100f, 100f) : targetWindow.position.center;
		}
	}
}
