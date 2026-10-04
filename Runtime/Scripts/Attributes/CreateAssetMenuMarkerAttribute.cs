using System;

namespace ParkMinDev.UPM.Foundation.Attributes
{
	[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
	public sealed class CreateAssetMenuMarkerAttribute : Attribute
	{
		// - Public Construct -
		public CreateAssetMenuMarkerAttribute(string menuPath = null) {
			MenuPath = menuPath;
		}

		// - Public Properties-
		public string MenuPath { get; }
	}
}