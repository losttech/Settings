namespace LostTech.App
{
    using System.Threading.Tasks;

    public interface ISettingsSet
    {
        void ScheduleSave();
        Task DisposeAsync();
    }
}
