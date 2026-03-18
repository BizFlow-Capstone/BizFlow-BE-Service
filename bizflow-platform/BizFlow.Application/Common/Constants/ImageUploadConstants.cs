namespace BizFlow.Application.Common.Constants
{
	public static class ImageUploadConstants
	{
		public const string DefaultFileName = "image";
		public const string ProductsPresetKey = "Products";
		public const string ImportsPresetKey = "Imports";
		public const string CostsPresetKey = "Costs";

		public static string GetPresetKey(ImageUploadTarget target)
		{
			return target switch
			{
				ImageUploadTarget.Products => ProductsPresetKey,
				// Import and Cost now share the same Cloudinary preset.
				ImageUploadTarget.Imports => CostsPresetKey,
				ImageUploadTarget.Costs => CostsPresetKey,
				_ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
			};
		}
	}

	public enum ImageUploadTarget
	{
		Products,
		Imports,
		Costs
	}
}
