using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Objects.Datas
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Objects.Datas", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "ClampedInt")]
	[System.Serializable]
	public class ClampedInt
	{
		public ClampedInt(int current, int min, int max) {
			_min = min;
			_max = max;
			_current = current;
			Update();
		}

		public void SetNormalized(float t) {
			t = Mathf.Clamp01(t);
			_current = Mathf.RoundToInt(_min + (_max - _min) * t);
			Clamp();
		}

		public int Current
		{
			get { return _current; }
			set
			{
				_current = value;
				Update();
			}
		}

		public int Max
		{
			get { return _max; }
			set
			{
				_max = value;
				Update();
			}
		}

		public int Min
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
			get { return Mathf.Abs(_max - _min) < 0.0001f ? 0f : (_current - _min) / (float)(_max - _min); }
		}

		public bool IsFull
		{
			get { return _current >= _max; }
		}
		public bool IsEmpty
		{
			get { return _current <= _min; }
		}

		[SerializeField] int _current;
		[SerializeField] int _min;
		[SerializeField] int _max;

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