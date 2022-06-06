namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task ActuallySaves()
        {
            string temp = Path.Combine(Path.GetTempPath(), nameof(IntegrationTests), Guid.NewGuid().ToString());
            DirectoryInfo directory = Directory.CreateDirectory(temp);
            string value = new('X', 3013);
            try {
                var file = new FileInfo(Path.Combine(directory.FullName, nameof(this.ActuallySaves)));
                file.Create().Close();
                var settingsSet = new SettingsSet<string, byte[]>(file, value, 
                    freezer: s => Encoding.UTF8.GetBytes(s),
                    serializer: async (stream, data) => {
                        stream.Write(data, 0, data.Length);
                        await stream.FlushAsync().ConfigureAwait(false);
                    });
                settingsSet.ScheduleSave();
                await settingsSet.DisposeAsync();

                Assert.AreEqual(value, File.ReadAllText(file.FullName));
            }
            finally {
                Directory.Delete(temp, recursive: true);
            }
        }
    }
}
