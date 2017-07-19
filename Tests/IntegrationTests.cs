namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PCLStorage;

    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task ActuallySaves()
        {
            string temp = Path.Combine(Path.GetTempPath(), nameof(IntegrationTests), Guid.NewGuid().ToString());
            Directory.CreateDirectory(temp);
            string value = new string('X', 3013);
            try {
                IFolder directory = await FileSystem.Current.GetFolderFromPathAsync(temp);
                IFile file = await directory.CreateFileAsync(nameof(this.ActuallySaves), CreationCollisionOption.ReplaceExisting);
                var settingsSet = new SettingsSet<string, byte[]>(file, value, 
                    freezer: s => Encoding.UTF8.GetBytes(s),
                    serializer: async (stream, data) => {
                        stream.Write(data, 0, data.Length);
                        await stream.FlushAsync().ConfigureAwait(false);
                    });
                settingsSet.ScheduleSave();
                await settingsSet.DisposeAsync();

                Assert.AreEqual(value, await file.ReadAllTextAsync());
            }
            finally {
                Directory.Delete(temp, recursive: true);
            }
        }
    }
}
