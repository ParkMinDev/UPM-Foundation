using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.Serialization;
#endif

namespace ParkMinPackages.Foundation.Components.Helpers
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public sealed class HierarchyHelper : MonoBehaviour
	{
#if UNITY_EDITOR
		// - Class Struct Enum -
		[Serializable]
		public sealed class HierarchyExpansionController
		{
			// - Public Methods -
			public void Initialize(HierarchyHelper owner) {
				_owner = owner;
			}
			public void ApplyOnceDelayed() {
				if (!_enabled || _owner == null) {
					return;
				}

				EditorApplication.delayCall -= ApplyOnce;
				EditorApplication.delayCall += ApplyOnce;
			}
			public void Release() {
				EditorApplication.delayCall -= ApplyOnce;
				_owner = null;
			}

			// - Internals -
			[SerializeField] bool _enabled;
			[SerializeField, ShowIf(nameof(_enabled))] Transform _targetChild;
			[SerializeField, ShowIf(nameof(_enabled)), LabelText("씬 진입 시 자동 적용"), FormerlySerializedAs("_autoExpand")] bool _applyOnSceneOpen;
			[NonSerialized] HierarchyHelper _owner;

			[Button("지정 자식만 펼치기"), ShowIf(nameof(_enabled))]
			void ExpandOnlyTargetChild() {
				ApplyHierarchyState(true);
			}

			void ApplyOnce() {
				EditorApplication.delayCall -= ApplyOnce;
				if (_applyOnSceneOpen && IsTargetValid()) {
					ApplyHierarchyState(false);
				}
			}

			bool IsTargetValid() {
				return _owner != null && _targetChild != null && _targetChild != _owner.transform && _targetChild.IsChildOf(_owner.transform);
			}

			void ApplyHierarchyState(bool selectTarget) {
				if (!IsTargetValid()) {
					Debug.LogError("지정한 오브젝트는 HierarchyHelper가 붙은 오브젝트의 자식이어야 합니다.", _owner);
					return;
				}

				Type hierarchyWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
				MethodInfo setExpandedRecursiveMethod = hierarchyWindowType?.GetMethod("SetExpandedRecursive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				MethodInfo setExpandedMethod = hierarchyWindowType?.GetMethod("SetExpanded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(EntityId), typeof(bool) }, null);
				if (hierarchyWindowType == null || setExpandedRecursiveMethod == null || setExpandedMethod == null) {
					Debug.LogError("Hierarchy 창의 접힘 상태를 변경하는 기능을 찾지 못했습니다.", _owner);
					return;
				}

				List<Transform> targetPath = new List<Transform>();
				Transform currentTransform = _targetChild;
				while (currentTransform != null) {
					targetPath.Add(currentTransform);
					if (currentTransform == _owner.transform) {
						break;
					}
					currentTransform = currentTransform.parent;
				}
				targetPath.Reverse();

				UnityEngine.Object[] hierarchyWindows = Resources.FindObjectsOfTypeAll(hierarchyWindowType);
				for (int i = 0; i < hierarchyWindows.Length; i++) {
					setExpandedRecursiveMethod.Invoke(hierarchyWindows[i], new object[] { _owner.gameObject.GetEntityId(), false });
					for (int j = 0; j < targetPath.Count; j++) {
						setExpandedMethod.Invoke(hierarchyWindows[i], new object[] { targetPath[j].gameObject.GetEntityId(), true });
					}
				}

				if (selectTarget) {
					Selection.activeGameObject = _targetChild.gameObject;
					EditorGUIUtility.PingObject(_targetChild.gameObject);
				}
			}
		}

		[Serializable]
		public sealed class SceneVisibilityController
		{
			// - Public Methods -
			public void Apply(GameObject target) {
				if (!_enabled) {
					if (_wasApplied) {
						Restore(target);
					}
					return;
				}

				SetSceneVisibility(target, _sceneVisibility, true);
				SetScenePicking(target, _scenePicking, true);
				EditorApplication.RepaintHierarchyWindow();
				_wasApplied = true;
			}
			public void Restore(GameObject target) {
				SetSceneVisibility(target, true, true);
				SetScenePicking(target, true, true);
				EditorApplication.RepaintHierarchyWindow();
				_wasApplied = false;
			}

			// - Internals -
			[SerializeField] bool _enabled;
			[SerializeField, ShowIf(nameof(_enabled))] bool _sceneVisibility = true;
			[SerializeField, ShowIf(nameof(_enabled))] bool _scenePicking = true;
			[NonSerialized] bool _wasApplied;

			void SetSceneVisibility(GameObject target, bool visible, bool includeDescendants) {
				SceneVisibilityManager manager = SceneVisibilityManager.instance;
				if (visible) {
					manager.Show(target, includeDescendants);
				}
				else {
					manager.Hide(target, includeDescendants);
				}
			}

			void SetScenePicking(GameObject target, bool visible, bool includeDescendants) {
				SceneVisibilityManager manager = SceneVisibilityManager.instance;
				if (visible) {
					manager.EnablePicking(target, includeDescendants);
				}
				else {
					manager.DisablePicking(target, includeDescendants);
				}
			}
		}

		// - Handler -
		void OnEnable() {
			_hierarchyExpansionController.Initialize(this);
			_hierarchyExpansionController.ApplyOnceDelayed();
			_sceneVisibilityController.Apply(gameObject);
			EditorApplication.update -= HandleEditorUpdate;
			EditorApplication.update += HandleEditorUpdate;
		}
		void OnDisable() {
			EditorApplication.update -= HandleEditorUpdate;
			_hierarchyExpansionController.Release();
			_sceneVisibilityController.Restore(gameObject);
		}
		void OnDestroy() {
			EditorApplication.update -= HandleEditorUpdate;
			_hierarchyExpansionController.Release();
			_sceneVisibilityController.Restore(gameObject);
		}
		void OnTransformChildrenChanged() {
			_sceneVisibilityController.Apply(gameObject);
		}
		void OnValidate() {
			_hierarchyExpansionController.Initialize(this);
			_sceneVisibilityController.Apply(gameObject);
		}

		// - Internals -
		[SerializeField] HierarchyExpansionController _hierarchyExpansionController = new HierarchyExpansionController();
		[SerializeField] SceneVisibilityController _sceneVisibilityController = new SceneVisibilityController();

		void HandleEditorUpdate() {
			if (Application.isPlaying) {
				return;
			}

			_sceneVisibilityController.Apply(gameObject);
		}
#endif
	}
}