using UnityEngine;

namespace ParkMinPackages.Foundation.Components
{
	public sealed class DestroyOnStart : MonoBehaviour
	{
		public enum DestroyTarget
		{
			GameObject,
			MonoBehaviour
		}

		void Start() {
			switch (_destroyTarget) {
				case DestroyTarget.GameObject:
					Destroy(gameObject);
					break;
				case DestroyTarget.MonoBehaviour:
					foreach (MonoBehaviour monoBehaviour in _monoBehaviours) {
						if (monoBehaviour == null || monoBehaviour == this) continue;
						Destroy(monoBehaviour);
					}
					Destroy(this);
					break;
			}
		}

		[SerializeField] DestroyTarget _destroyTarget = DestroyTarget.GameObject;
		[SerializeField] MonoBehaviour[] _monoBehaviours = new MonoBehaviour[] { };
	}
}