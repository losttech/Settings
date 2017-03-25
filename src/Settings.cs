namespace LostTech.App
{
    using System;
    using System.Threading.Tasks;

    public sealed class Settings
    {
        public async Task<SettingsSet<T>> Load<T>(string fileName) where T:ICopyable<T>
        {
            throw new NotImplementedException();
        }
    }
}
