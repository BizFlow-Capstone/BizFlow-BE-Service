namespace BizFlow.Application.Common.Constants
{
    public static class ImageUploadConstants
    {
        public const string DefaultFileName = "image";
        public const string ProductsPresetKey = "Products";
        public const string ImportsPresetKey = "Imports";

        public static string GetPresetKey(ImageUploadTarget target)
        {
            return target switch
            {
                ImageUploadTarget.Products => ProductsPresetKey,
                ImageUploadTarget.Imports => ImportsPresetKey,
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
            };
        }
    }

    public enum ImageUploadTarget
    {
        Products,
        Imports
    }
}
