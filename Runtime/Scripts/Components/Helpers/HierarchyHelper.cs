using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using ParkMinDev.UPM.Foundation.Constants;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ParkMinDev.UPM.Foundation.Components.Helpers
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class HierarchyHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        // - Public Methods -
        public void ExpandOnlyTargetChildren() {
            Transform[] targets = _autoExpandTargets.Where(child => child != null && child != gameObject && child.transform.IsChildOf(transform)).Select(child => child.transform).Distinct().ToArray();
            if (targets.Length == 0)
                return;
            if (SetExpandedMethod == null || SetExpandedRecursiveMethod == null) {
                Debug.LogWarning("Hierarchy 창의 접힘 상태를 변경하는 기능을 찾지 못했습니다.", this);
                return;
            }

            // 지정한 경로들을 펼치고 선택은 유지
            Transform[] path = targets.SelectMany(child => child.GetComponentsInParent<Transform>(true).Reverse()).Distinct().ToArray();
            foreach (UnityEngine.Object window in Resources.FindObjectsOfTypeAll(HierarchyWindowType)) {
                if (window == null)
                    continue;
                SetExpandedRecursiveMethod.Invoke(window, new object[] { gameObject.GetEntityId(), false });
                foreach (Transform item in path)
                    SetExpandedMethod.Invoke(window, new object[] { item.gameObject.GetEntityId(), true });
            }
            EditorApplication.RepaintHierarchyWindow();
        }

        // - Handler -
        void OnEnable() {
            EditorApplication.hierarchyChanged += ScheduleVisibility;
            ScheduleVisibility();
        }

        void OnDisable() {
            EditorApplication.hierarchyChanged -= ScheduleVisibility;
            EditorApplication.delayCall -= ApplyVisibility;
            foreach (SceneVisibilityController controller in _appliedVisibilityControllers)
                controller.Restore();
            _appliedVisibilityControllers = Array.Empty<SceneVisibilityController>();
        }

        void OnValidate() {
            ScheduleVisibility();
        }

        // - Private & Protected -
        [SerializeField] GameObject[] _autoExpandTargets = Array.Empty<GameObject>();
        [SerializeField] SceneVisibilityController[] _sceneVisibilityControllers = Array.Empty<SceneVisibilityController>();

        SceneVisibilityController[] _appliedVisibilityControllers = Array.Empty<SceneVisibilityController>();

        void ScheduleVisibility() {
            EditorApplication.delayCall -= ApplyVisibility;
            if (isActiveAndEnabled && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += ApplyVisibility;
        }

        void ApplyVisibility() {
            if (this == null || !isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            SceneVisibilityController[] controllers = _sceneVisibilityControllers.Where(controller => controller != null).ToArray();
            foreach (SceneVisibilityController removed in _appliedVisibilityControllers.Except(controllers))
                removed.Restore();
            foreach (SceneVisibilityController controller in controllers)
                controller.Apply();
            _appliedVisibilityControllers = controllers;
            EditorApplication.RepaintHierarchyWindow();
        }

        // - Class Struct Enum -
        [Serializable]
        public sealed class SceneVisibilityController
        {
            // - Public Methods -
            public void Apply() {
                if (_appliedTarget != _target)
                    Restore();
                if (_target == null || !_target.scene.IsValid() || EditorUtility.IsPersistent(_target))
                    return;
                SceneVisibilityManager manager = SceneVisibilityManager.instance;
                if (_appliedTarget == null) {
                    _wasHidden = manager.IsHidden(_target);
                    _wasPickingDisabled = manager.IsPickingDisabled(_target);
                    _appliedTarget = _target;
                }
                if (_sceneVisibility)
                    manager.Show(_target, true);
                else
                    manager.Hide(_target, true);
                if (_scenePicking)
                    manager.EnablePicking(_target, true);
                else
                    manager.DisablePicking(_target, true);
            }

            public void Restore() {
                if (_appliedTarget != null) {
                    SceneVisibilityManager manager = SceneVisibilityManager.instance;
                    if (_wasHidden)
                        manager.Hide(_appliedTarget, true);
                    else
                        manager.Show(_appliedTarget, true);
                    if (_wasPickingDisabled)
                        manager.DisablePicking(_appliedTarget, true);
                    else
                        manager.EnablePicking(_appliedTarget, true);
                }
                _appliedTarget = null;
            }

            // - Private & Protected -
            [Header(Headers.Required)]
            [SerializeField, Required] GameObject _target;

            [Header(Headers.Settings)]
            [SerializeField] bool _sceneVisibility = true;
            [SerializeField] bool _scenePicking = true;

            [NonSerialized] GameObject _appliedTarget;
            [NonSerialized] bool _wasHidden;
            [NonSerialized] bool _wasPickingDisabled;
        }

        // - Private Statics -
        static readonly Type HierarchyWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static readonly MethodInfo SetExpandedMethod = HierarchyWindowType?.GetMethod("SetExpanded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(EntityId), typeof(bool) }, null);
        static readonly MethodInfo SetExpandedRecursiveMethod = HierarchyWindowType?.GetMethod("SetExpandedRecursive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(EntityId), typeof(bool) }, null);

        [InitializeOnLoadMethod]
        static void InitializeSelectionHandler() {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void OnSelectionChanged() {
            EditorApplication.delayCall -= ExpandSelected;
            EditorApplication.delayCall += ExpandSelected;
        }

        static void ExpandSelected() {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                return;
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid() || PrefabStageUtility.GetPrefabStage(selected) != null || !selected.TryGetComponent(out HierarchyHelper helper) || !helper.enabled)
                return;
            helper.ExpandOnlyTargetChildren();
        }
#endif
    }
}
