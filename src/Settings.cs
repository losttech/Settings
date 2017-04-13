namespace LostTech.App
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using PCLStorage;
    using FileAccess = PCLStorage.FileAccess;

    public sealed class Settings: ISettingsSet
    {
        readonly IFolder folder;
        readonly IFreezerFactory freezerFactory;
        readonly ISerializerFactory serializerFactory;
        readonly IDeserializerFactory deserializerFactory;
        readonly Stack<ISettingsSet> loadedSettings = new Stack<ISettingsSet>();

        public Settings([NotNull] IFolder folder,
            [NotNull] IFreezerFactory freezerFactory,
            [NotNull] ISerializerFactory serializerFactory,
            [NotNull] IDeserializerFactory deserializerFactory)
        {
            this.folder = folder ?? throw new ArgumentNullException(nameof(folder));
            this.freezerFactory = freezerFactory ?? throw new ArgumentNullException(nameof(freezerFactory));
            this.serializerFactory = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));
            this.deserializerFactory = deserializerFactory ?? throw new ArgumentNullException(nameof(deserializerFactory));
        }

        public async Task<SettingsSet<T, TFreezed>> Load<T, TFreezed>([NotNull] string fileName) where T: class
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var file = await this.folder.GetFile(fileName).ConfigureAwait(false);
            if (file == null)
                return null;

            T value;
            using (var stream = await file.OpenAsync(FileAccess.Read).ConfigureAwait(false))
                value = await deserializerFactory.MakeDeserializer<T>()(stream).ConfigureAwait(false);

            var result = new SettingsSet<T, TFreezed>(file, value,
                this.freezerFactory.MakeFreezer<T, TFreezed>(),
                this.serializerFactory.MakeSerializer<TFreezed>());
            this.loadedSettings.Push(result);
            return result;
        }

        public Task<SettingsSet<T, TFreezed>> LoadOrCreate<T, TFreezed>([NotNull] string fileName)
            where T : class, new()
            => this.LoadOrCreate<T, TFreezed>(fileName, () => new T());

        public async Task<SettingsSet<T, TFreezed>> LoadOrCreate<T, TFreezed>([NotNull] string fileName, Func<T> defaultSettings)
            where T : class
        {
            if (defaultSettings == null)
                throw new ArgumentNullException(nameof(defaultSettings));

            var settings = await this.Load<T, TFreezed>(fileName).ConfigureAwait(false);
            if (settings != null)
                return settings;

            var file = await this.folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists).ConfigureAwait(false);
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
