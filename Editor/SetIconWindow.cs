using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ParkMinDev.UPM.Foundation.Editor
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Editor", sourceAssembly: "ParkMinPackages.Foundation.Editor", sourceClassName: "SetIconWindow")]
	public sealed class SetIconWindow : EditorWindow
	{
		const string MenuPath = "Assets/" + nameof(ParkMinDev) + "/스크립트 아이콘 변경";
		const float CellSize = 72f;
		const float WindowPadding = 20f;

		readonly List<Texture2D> icons = new List<Texture2D>();
		readonly List<GUIContent> iconContents = new List<GUIContent>();

		Vector2 scrollPosition;
		int selectedIconIndex;
		GUIStyle iconStyle;

		[MenuItem(MenuPath, priority = -100)]
		public static void ShowMenuItem() {
			SetIconWindow window = GetWindow<SetIconWindow>();
			window.titleContent = new GUIContent("Set Script Icon");
			window.minSize = new Vector2(250f, 180f);
			window.Show();
		}

		[MenuItem(MenuPath, validate = true)]
		public static bool ValidateShowMenuItem() {
			return HasOnlyMonoScriptsSelected();
		}

		void OnEnable() {
			RefreshIcons();
			scrollPosition = Vector2.zero;
			selectedIconIndex = 0;
		}

		void OnSelectionChange() {
			Repaint();
		}

		void OnGUI() {
			EnsureStyles();

			EditorGUILayout.LabelField($"Selected scripts: {Selection.objects.Length}");
			EditorGUILayout.Space(4f);

			using (new EditorGUILayout.HorizontalScope()) {
				using (new EditorGUI.DisabledScope(!HasOnlyMonoScriptsSelected() || icons.Count == 0)) {
					if (GUILayout.Button("Apply", GUILayout.Height(24f))) {
						ApplySelectedIcon();
						Close();
						GUIUtility.ExitGUI();
					}
				}

				if (GUILayout.Button("Refresh", GUILayout.Width(80f), GUILayout.Height(24f))) {
					RefreshIcons();
				}
			}

			EditorGUILayout.Space(4f);

			if (icons.Count == 0) {
				EditorGUILayout.HelpBox("No script icons were found.", MessageType.Info);
				return;
			}

			int columnCount = Mathf.Max(1, Mathf.FloorToInt((position.width - WindowPadding) / CellSize));
			int rowCount = Mathf.CeilToInt((float)icons.Count / columnCount);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			selectedIconIndex = GUILayout.SelectionGrid(
				Mathf.Clamp(selectedIconIndex, 0, icons.Count - 1),
				iconContents.ToArray(),
				columnCount,
				iconStyle,
				GUILayout.Height(rowCount * CellSize)
			);
			EditorGUILayout.EndScrollView();
		}

		void RefreshIcons() {
			icons.Clear();
			iconContents.Clear();

			icons.Add(null);
			iconContents.Add(new GUIContent("None", "Remove the custom script icon"));

			string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D l:ScriptIcon");
			Array.Sort(assetGuids, StringComparer.Ordinal);

			foreach (string assetGuid in assetGuids) {
				string path = AssetDatabase.GUIDToAssetPath(assetGuid);
				Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (icon == null || icons.Contains(icon))
					continue;

				icons.Add(icon);
				iconContents.Add(new GUIContent(icon, icon.name));
			}

			selectedIconIndex = Mathf.Clamp(selectedIconIndex, 0, icons.Count - 1);
			Repaint();
		}

		void EnsureStyles() {
			if (iconStyle != null)
				return;

			iconStyle = new GUIStyle(GUI.skin.button)
			{
				fixedWidth = CellSize,
				fixedHeight = CellSize,
				imagePosition = ImagePosition.ImageAbove
			};
		}

		void ApplySelectedIcon() {
			if (!HasOnlyMonoScriptsSelected() || icons.Count == 0)
				return;

			Texture2D icon = icons[Mathf.Clamp(selectedIconIndex, 0, icons.Count - 1)];

			foreach (Object asset in Selection.objects) {
				string path = AssetDatabase.GetAssetPath(asset);
				MonoImporter importer = AssetImporter.GetAtPath(path) as MonoImporter;
				if (importer == null) {
					Debug.LogWarning($"Could not load MonoImporter for {path}", asset);
					continue;
				}

				try {
					importer.SetIcon(icon);
					importer.SaveAndReimport();
				}
				catch (Exception exception) {
					Debug.LogError($"Failed to set the script icon for {path}:\n{exception}", asset);
				}
			}

			AssetDatabase.Refresh();
		}

		static bool HasOnlyMonoScriptsSelected() {
			Object[] selectedAssets = Selection.objects;
			if (selectedAssets == null || selectedAssets.Length == 0)
				return false;

			foreach (Object asset in selectedAssets) {
				if (asset is not MonoScript)
					return false;

				string path = AssetDatabase.GetAssetPath(asset);
				if (AssetImporter.GetAtPath(path) is not MonoImporter)
					return false;
			}

			return true;
		}
	}
}