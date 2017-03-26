namespace LostTech.App
{
    using System;
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using PCLStorage;
    using FileAccess = PCLStorage.FileAccess;

    public sealed class Settings
    {
        readonly IFolder folder;
        readonly IFreezerFactory freezerFactory;
        readonly ISerializerFactory serializerFactory;
        readonly IDeserializerFactory deserializerFactory;

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
            T value;
            using (var stream = await file.OpenAsync(FileAccess.Read).ConfigureAwait(false))
                value = await deserializerFactory.MakeDeserializer<T>()(stream).ConfigureAwait(false);

            return new SettingsSet<T, TFreezed>(file, value,
                this.freezerFactory.MakeFreezer<T, TFreezed>(),
                this.serializerFactory.MakeSerializer<TFreezed>());
        }
    }
}
