using System.Text.RegularExpressions;

namespace MyBudgetIA.Domain.Constraints
{
    public static partial class PhotoConstraints
    {
        public const long MaxSizeInBytes = 5 * 1024 * 1024; // 5 MB

        public const int MaxSizeInMB = 10;

        public static readonly string[] AllowedExtensions =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".heic"
        ];

        public static readonly string[] AllowedContentTypes =
        [
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/heic"
        ];

        public static readonly Dictionary<string, string[]> CorrespondingMimeTypes = new()
        {
            ["jpg"] = ["image/jpg", "image/jpeg"],
            ["jpeg"] = ["image/jpeg"],
            ["png"] = ["image/png"],
            ["webp"] = ["image/webp"],
            ["heic"] = ["image/heic"]
        };

        public const int MaxPhotosPerRequest = 5;

        public const int MaxFileNameLength = 255;

        // Regex for forbidden caracters in Azure Blob Storage
        public static readonly Regex InvalidFileNameCharsRegex = ForbiddenBlobCharactersRegex();

        [GeneratedRegex(@"[\""\\/:|<>*?\x00-\x1F\x7F]")]
        private static partial Regex ForbiddenBlobCharactersRegex();

        public static bool IsValidFileName(string fileName)
        {
            if (fileName == "." || fileName == "..") return false;
            return !InvalidFileNameCharsRegex.IsMatch(fileName);
        }

    }
}
