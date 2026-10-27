using System;
using System.Linq;
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

            // LOD 전환 중지
            foreach (LODGroup target in _components.OfType<LODGroup>()) {
                if (target != null)
                    target.enabled = false;
            }

            // 컴포넌트 비활성화 및 삭제 예약
            foreach (Component target in _components) {
                if (target == null || target == this)
                    continue;
                switch (target) {
                    case Transform:
                        Debug.LogWarning("Transform은 단독으로 삭제할 수 없습니다. 해당 GameObject를 Game Objects 목록에 지정해주세요.", this);
                        continue;
                    case Behaviour behaviour:
                        behaviour.enabled = false;
                        break;
                    case Renderer renderer:
                        renderer.enabled = false;
                        break;
#if FOUNDATION_HAS_PHYSICS
                    case Collider collider:
                        collider.enabled = false;
                        break;
                    case Rigidbody rigidbody:
                        rigidbody.isKinematic = true;
                        rigidbody.detectCollisions = false;
                        break;
#endif
#if FOUNDATION_HAS_PHYSICS_2D
                    case Rigidbody2D rigidbody2D:
                        rigidbody2D.simulated = false;
                        break;
#endif
#if FOUNDATION_HAS_CLOTH
                    case Cloth cloth:
                        cloth.enabled = false;
                        break;
#endif
#if FOUNDATION_HAS_PARTICLE_SYSTEM
                    case ParticleSystem particleSystem:
                        ParticleSystem.MainModule main = particleSystem.main;
                        main.stopAction = ParticleSystemStopAction.None;
                        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                        break;
#endif
#if FOUNDATION_HAS_UI
                    case CanvasRenderer canvasRenderer:
                        canvasRenderer.cull = true;
                        break;
#endif
#if FOUNDATION_HAS_WIND
                    case WindZone windZone:
                        windZone.windMain = 0f;
                        windZone.windTurbulence = 0f;
                        windZone.windPulseMagnitude = 0f;
                        break;
#endif
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
