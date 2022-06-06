namespace LostTech.App
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents settings stored the particular directory.
    /// Individual files in that directory are represented by <see cref="SettingsSet{T, TFrozen}">SettingsSets</see>.
    /// </summary>
    public sealed class Settings: ISettingsSet
    {
        readonly DirectoryInfo folder;
        readonly IFreezerFactory freezerFactory;
        readonly ISerializerFactory serializerFactory;
        readonly IDeserializerFactory deserializerFactory;
        readonly Stack<ISettingsSet> loadedSettings = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="folder">The directory where the settings are stored</param>
        /// <param name="freezerFactory">Object, that creates readonly snapshots of the settings.</param>
        /// <param name="serializerFactory">Object, that creates serializers for snapshots.</param>
        /// <param name="deserializerFactory">Object, that creates deserializers for settings.</param>
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

        /// <summary>
        /// Asynchronously loads a set of settings from the specified file in the settings directory.
        /// </summary>
        /// <typeparam name="T">Type of the settings set</typeparam>
        /// <param name="fileName">Name of the file to load the set of settings from.</param>
        /// <returns>A <see cref="SettingsSet{T, TFrozen}">SettingsSet</see></returns>
        public Task<SettingsSet<T, T>?> Load<T>(string fileName) where T : class
            => this.Load<T, T>(fileName);
        /// <summary>
        /// Asynchronously loads a set of settings from the specified file in the settings directory.
        /// </summary>
        /// <typeparam name="T">Type of the settings set</typeparam>
        /// <typeparam name="TFrozen">Type of the snapshot of the settings set.</typeparam>
        /// <param name="fileName">Name of the file to load the set of settings from.</param>
        /// <returns>A <see cref="SettingsSet{T, TFrozen}">SettingsSet</see></returns>
        public async Task<SettingsSet<T, TFrozen>?> Load<T, TFrozen>(string fileName) where T: class
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var file = new FileInfo(Path.Combine(this.folder.FullName, fileName));
            if (!file.Exists)
                return null;

            T value;
            using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                value = await this.deserializerFactory.MakeDeserializer<T>()(stream).ConfigureAwait(false);

            var result = new SettingsSet<T, TFrozen>(file, value,
                this.freezerFactory.MakeFreezer<T, TFrozen>(),
                this.serializerFactory.MakeSerializer<TFrozen>());
            this.loadedSettings.Push(result);
            return result;
        }

        /// <summary>
        /// Asynchronously loads a set of settings from the specified file in the settings directory.
        /// If the file does not exist, a default object is created.
        /// </summary>
        /// <typeparam name="T">Type of the settings set</typeparam>
        /// <param name="fileName">Name of the file to load the set of settings from.</param>
        /// <returns>A <see cref="SettingsSet{T, TFrozen}">SettingsSet</see></returns>
        public Task<SettingsSet<T, T>> LoadOrCreate<T>(string fileName) where T : class, new()
            => this.LoadOrCreate<T, T>(fileName, () => new T());
        /// <summary>
        /// Asynchronously loads a set of settings from the specified file in the settings directory.
        /// If the file does not exist, a default object is created.
        /// </summary>
        /// <typeparam name="T">Type of the settings set</typeparam>
        /// <typeparam name="TFrozen">Type of the snapshot of the settings set.</typeparam>
        /// <param name="fileName">Name of the file to load the set of settings from.</param>
        /// <returns>A <see cref="SettingsSet{T, TFrozen}">SettingsSet</see></returns>
        public Task<SettingsSet<T, TFrozen>> LoadOrCreate<T, TFrozen>(string fileName)
            where T : class, new()
            => this.LoadOrCreate<T, TFrozen>(fileName, () => new T());

        /// <summary>
        /// Asynchronously loads a set of settings from the specified file in the settings directory.
        /// If the file does not exist, <paramref name="defaultSettings"/> function is called to
        /// construct the default set.
        /// </summary>
        /// <typeparam name="T">Type of the settings set</typeparam>
        /// <param name="fileName">Name of the file to load the set of settings from.</param>
        /// <param name="defaultSettings">A factory that is invoked when the file does not exist to produce settings with default values.</param>
        /// <returns>A <see cref="SettingsSet{T, TFrozen}">SettingsSet</see></returns>
        public Task<SettingsSet<T, T>> LoadOrCreate<T>(string fileName, Func<T> defaultSettings)
            where T : class
            => this.LoadOrCreate<T, T>(fileName, defaultSettings);
        /// <summary>
        /// Asynchronously loads a set of settings from the specified file in the settings directory.
        /// If the file does not exist, <paramref name="defaultSettings"/> function is called to
        /// construct the default set.
        /// </summary>
        /// <typeparam name="T">Type of the settings set</typeparam>
        /// <typeparam name="TFrozen">Type of the snapshot of the settings set.</typeparam>
        /// <param name="fileName">Name of the file to load the set of settings from.</param>
        /// <param name="defaultSettings">A factory that is invoked when the file does not exist to produce settings with default values.</param>
        /// <returns>A <see cref="SettingsSet{T, TFrozen}">SettingsSet</see></returns>
        public async Task<SettingsSet<T, TFrozen>> LoadOrCreate<T, TFrozen>(string fileName, Func<T> defaultSettings)
            where T : class
        {
            if (defaultSettings == null)
                throw new ArgumentNullException(nameof(defaultSettings));

            var settings = await this.Load<T, TFrozen>(fileName).ConfigureAwait(false);
            if (settings != null)
                return settings;

            var file = new FileInfo(Path.Combine(this.folder.FullName, fileName));
            file.Create().Dispose();
            var result = new SettingsSet<T, TFrozen>(file, defaultSettings(),
                this.freezerFactory.MakeFreezer<T, TFrozen>(),
                this.serializerFactory.MakeSerializer<TFrozen>());
            result.ScheduleSave();
            this.loadedSettings.Push(result);
            return result;
        }

        /// <inheritdoc/>
        public void ScheduleSave()
        {
            foreach (var item in this.loadedSettings)
                item.ScheduleSave();
        }

        /// <inheritdoc/>
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
