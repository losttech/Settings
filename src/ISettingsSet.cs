namespace LostTech.App
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    public interface ISettingsSet
    {
        void ScheduleSave();
        [NotNull]
        Task DisposeAsync();
    }
}
