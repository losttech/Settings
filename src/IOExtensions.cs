namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using PCLStorage;

    static class IOExtensions
    {
        public static async Task<IFile> GetFile(this IFolder folder, string name)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            try {
                return await folder.GetFileAsync(name).ConfigureAwait(false);
            } catch (FileNotFoundException) {
                return null;
            }
        }
    }
}
