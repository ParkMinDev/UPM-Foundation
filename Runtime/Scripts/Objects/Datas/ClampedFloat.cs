using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Objects.Datas
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Objects.Datas", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "ClampedFloat")]
	[System.Serializable]
	public struct ClampedFloat
	{
		public ClampedFloat(float current, float min, float max) {
			_min = min;
			_max = max;
			_current = current;
			Update();
		}
		public void SetNormalized(float t) {
			_current = Mathf.Lerp(_min, _max, Mathf.Clamp01(t));
		}
		public float Current
		{
			get { return _current; }
			set
			{
				_current = value;
				Update();
			}
		}
		public float Max
		{
			get { return _max; }
			set
			{
				_max = value;
				Update();
			}
		}
		public float Min
		{
			get { return _min; }
			set
			{
				_min = value;
				Update();
			}
		}

		public float Normalized
		{
			get { return Mathf.Abs(_max - _min) < 0.0001f ? 0f : (_current - _min) / (_max - _min); }
		}

		public bool IsFull
		{
			get { return Current >= Max; }
		}
		public bool IsEmpty
		{
			get { return Current <= _min; }
		}

		[SerializeField] float _current;
		[SerializeField] float _min;
		[SerializeField] float _max;

		void Update() {
			ValidateBounds();
			Clamp();
		}

		void Clamp() {
			_current = Mathf.Clamp(_current, _min, _max);
		}
		void ValidateBounds() {
			if (_max < _min)
				_max = _min;
		}
	}
}