using System;
using ParkMinDev.UPM.Foundation.Constants;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Components
{
    [DefaultExecutionOrder(int.MinValue), DisallowMultipleComponent]
    public sealed class DestroyOnAwake : MonoBehaviour
    {
        // - Handler -
        void Awake() {
            // 대상 비활성화 및 삭제 예약
            foreach (GameObject target in _gameObjects) {
                if (target != null) {
                    target.SetActive(false);
                    Destroy(target);
                }
            }
            foreach (Component target in _components) {
                if (target == null || target == this)
                    continue;
                switch (target) {
                    case Transform:
                        Debug.LogWarning("Transform은 단독으로 삭제할 수 없습니다. 해당 GameObject를 Game Objects 목록에 지정해주세요.", this);
                        continue;
                    case MonoBehaviour behaviour:
                        behaviour.enabled = false;
                        break;
                }
                Destroy(target);
            }

            // 정리 컴포넌트도 제거
            enabled = false;
            Destroy(this);
        }

        // - Private & Protected -
        [Header(Headers.Optional)]
        [SerializeField] GameObject[] _gameObjects = Array.Empty<GameObject>();
        [SerializeField] Component[] _components = Array.Empty<Component>();
    }
}
