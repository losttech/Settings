namespace LostTech.App
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class Settings: ISettingsSet
    {
        readonly DirectoryInfo folder;
        readonly IFreezerFactory freezerFactory;
        readonly ISerializerFactory serializerFactory;
        readonly IDeserializerFactory deserializerFactory;
        readonly Stack<ISettingsSet> loadedSettings = new();

        public Settings(DirectoryInfo folder,
            IFreezerFactory freezerFactory,
            ISerializerFactory serializerFactory,
            IDeserializerFactory deserializerFactory)
        {
            this.folder = folder ?? throw new ArgumentNullException(nameof(folder));
            this.freezerFactory = freezerFactory ?? throw new ArgumentNullException(nameof(freezerFactory));
            this.serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
            this.deserializerFactory = deserializerFactory ?? throw new ArgumentNullException(nameof(deserializerFactory));
        }

        public Task<SettingsSet<T, T>?> Load<T>(string fileName) where T : class
            => this.Load<T, T>(fileName);
        public async Task<SettingsSet<T, TFreezed>?> Load<T, TFreezed>(string fileName) where T: class
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var file = new FileInfo(Path.Combine(this.folder.FullName, fileName));
            if (!file.Exists)
                return null;

            T value;
            using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                value = await this.deserializerFactory.MakeDeserializer<T>()(stream).ConfigureAwait(false);

            var result = new SettingsSet<T, TFreezed>(file, value,
                this.freezerFactory.MakeFreezer<T, TFreezed>(),
                this.serializerFactory.MakeSerializer<TFreezed>());
            this.loadedSettings.Push(result);
            return result;
        }

        public Task<SettingsSet<T, T>> LoadOrCreate<T>(string fileName) where T : class, new()
            => this.LoadOrCreate<T, T>(fileName, () => new T());
        public Task<SettingsSet<T, TFreezed>> LoadOrCreate<T, TFreezed>(string fileName)
            where T : class, new()
            => this.LoadOrCreate<T, TFreezed>(fileName, () => new T());

        public Task<SettingsSet<T, T>> LoadOrCreate<T>(string fileName, Func<T> defaultSettings)
            where T : class
            => this.LoadOrCreate<T, T>(fileName, defaultSettings);
        public async Task<SettingsSet<T, TFreezed>> LoadOrCreate<T, TFreezed>(string fileName, Func<T> defaultSettings)
            where T : class
        {
            if (defaultSettings == null)
                throw new ArgumentNullException(nameof(defaultSettings));

            var settings = await this.Load<T, TFreezed>(fileName).ConfigureAwait(false);
            if (settings != null)
                return settings;

            var file = new FileInfo(Path.Combine(this.folder.FullName, fileName));
            file.Create().Dispose();
            var result = new SettingsSet<T, TFreezed>(file, defaultSettings(),
                this.freezerFactory.MakeFreezer<T, TFreezed>(),
                this.serializerFactory.MakeSerializer<TFreezed>());
            result.ScheduleSave();
            this.loadedSettings.Push(result);
            return result;
        }

        public void ScheduleSave()
        {
            foreach (var item in this.loadedSettings)
                item.ScheduleSave();
        }

        public Task DisposeAsync()
        {
            if (this.loadedSettings.Count == 0)
                return Task.CompletedTask;

            var disposeTasks = this.loadedSettings.Select(set => set.DisposeAsync()).ToArray();
            this.loadedSettings.Clear();
            return Task.WhenAll(disposeTasks);
        }
    }
}
