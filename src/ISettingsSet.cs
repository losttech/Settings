namespace LostTech.App
{
    using System.Threading.Tasks;

    /// <summary>
    /// A set of settings, that can be saved.
    /// </summary>
    /// <remarks>
    /// The set must be properly disposed using <see cref="DisposeAsync"/>.
    /// </remarks>
    public interface ISettingsSet
    {
        /// <summary>
        /// Makes a snapshot of the settings and schedules it to be saved.
        /// </summary>
        void ScheduleSave();
        /// <summary>
        /// Must be invoked in order to ensure, that settings are saved correctly.
        /// You <strong>must</strong> delay app shutdown until this completes.
        /// </summary>
        Task DisposeAsync();
    }
}
