using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Components
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Components", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "InstantiateOnceOnRuntime")]
	[DefaultExecutionOrder(int.MinValue)]
	public partial class InstantiateOnceOnRuntime : MonoBehaviour
	{
		[AutoStaticsCleanup]
		static bool IsInstantiated = false;

		void Awake() {
			if (IsInstantiated)
				return;

			foreach (GameObject prefab in _prefabs) {
				try {
					Instantiate(prefab);
				}
				catch (Exception e) {
					Debug.LogError(e);
				}
			}
			IsInstantiated = true;
		}


		[SerializeField] GameObject[] _prefabs;
	}
}