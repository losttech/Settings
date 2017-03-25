namespace LostTech.App
{
    using PCLStorage;
    using LostTech.Checkpoint;

    public sealed class SettingsSet<T>
        where T: ICopyable<T>
    {
        readonly AsyncChainService autosaveService;
        public T Value { get; }
    }
}
