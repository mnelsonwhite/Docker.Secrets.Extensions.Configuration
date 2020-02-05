using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace Docker.Secrets.Extensions.Configuration.Util
{
    internal static class FileExtensions
    {
        public static string GetExtension(this IFileInfo fileInfo)
        {
            return fileInfo.Name.Split(".").LastOrDefault();
        }

        public static string GetFileName(this IFileInfo fileInfo)
        {
            var fileParts = fileInfo.Name.Split(".");
            return fileParts.Length == 1
                ? fileInfo.Name
                : string.Join(".", fileParts.Take(fileParts.Length - 1));
        }
    }
}