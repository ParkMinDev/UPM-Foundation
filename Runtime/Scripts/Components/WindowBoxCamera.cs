using System;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Components
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.Foundation.Components", sourceAssembly: "ParkMinPackages.Foundation", sourceClassName: "WindowBoxCamera")]
	[ExecuteAlways]
	[RequireComponent(typeof(Camera))]
	public sealed class WindowBoxCamera : MonoBehaviour
	{
		// - Public Properties -
		public int TargetWidth
		{
			get { return _targetWidth; }
		}

		public int TargetHeight
		{
			get { return _targetHeight; }
		}

		public Camera Camera
		{
			get { return _camera; }
		}

		// - Handler -
		void Awake() {
			_camera = GetComponent<Camera>();
		}

		void Update() {
			Apply();
		}

		void OnDisable() {
			if (_camera != null)
				_camera.rect = new Rect(0f, 0f, 1f, 1f);
		}

		// - Internals -
		[SerializeField, Min(1)] int _editorWidth = 1920;
		[SerializeField, Min(1)] int _editorHeight = 1080;
		[SerializeField, Min(1)] int _targetWidth = 1920;
		[SerializeField, Min(1)] int _targetHeight = 1080;

		Camera _camera;

		void Apply() {
			if (_camera == null)
				_camera = GetComponent<Camera>();

			Vector2Int outputSize = GetOutputSize();

			float outputAspect = outputSize.x / (float)outputSize.y;
			float targetAspect = Mathf.Max(1, _targetWidth) /
			                     (float)Mathf.Max(1, _targetHeight);

			if (outputAspect > targetAspect) {
				float width = targetAspect / outputAspect;
				_camera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
			}
			else {
				float height = outputAspect / targetAspect;
				_camera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
			}
		}

		Vector2Int GetOutputSize() {
			if (_camera.targetTexture != null) {
				return new Vector2Int(
					_camera.targetTexture.width,
					_camera.targetTexture.height
				);
			}

			if (Application.isEditor)
				return new Vector2Int(Mathf.Max(1, _editorWidth), Mathf.Max(1, _editorHeight));

			int displayIndex = _camera.targetDisplay;
			if (0 <= displayIndex && displayIndex < Display.displays.Length) {
				Display display = Display.displays[displayIndex];
				if (0 < display.renderingWidth && 0 < display.renderingHeight)
					return new Vector2Int(display.renderingWidth, display.renderingHeight);
			}

			throw new InvalidOperationException("Unable to determine output size: no valid render target, editor size, or display resolution available.");
		}
	}
}