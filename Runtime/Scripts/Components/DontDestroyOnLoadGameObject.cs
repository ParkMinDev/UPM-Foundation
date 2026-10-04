using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Components
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Components", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "DontDestroyOnLoadGameObject")]
	public sealed class DontDestroyOnLoadGameObject : MonoBehaviour
	{
		void Awake() {
			transform.SetParent(null);
			DontDestroyOnLoad(gameObject);
		}
	}
}