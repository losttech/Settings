namespace LostTech.App
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using LostTech.Checkpoint;
    using ThomasJaworski.ComponentModel;
    using Stream = System.IO.Stream;

    public sealed class SettingsSet<T, TFreezed>: ISettingsSet, INotifyPropertyChanged
        where T: class
    {
        readonly FileInfo file;
        readonly AsyncChainService autosaveService = new();
        readonly ChangeListener? changeListener;
        readonly Func<T, TFreezed> freezer;
        readonly Func<Stream, TFreezed, Task> serializer;
        bool autosave;

        internal SettingsSet(FileInfo file, T value,
            Func<T, TFreezed> freezer, Func<Stream, TFreezed, Task> serializer)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
            this.freezer = freezer ?? throw new ArgumentNullException(nameof(freezer));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            if (value is INotifyPropertyChanged trackable)
            {
                trackable.PropertyChanged += this.SettingChanged;
                this.changeListener = ChangeListener.Create(trackable);
                this.changeListener.PropertyChanged += this.SettingChanged;
                this.changeListener.CollectionChanged += this.SettingChanged;
            }
            this.autosaveService.TaskException += this.AutosaveService_TaskException;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<UnobservedTaskExceptionEventArgs>? SaveException;

        public bool Autosave {
            get => this.autosave;
            set {
                this.autosave = value;
                this.OnPropertyChanged();
            }
        }

        void SettingChanged(object sender, EventArgs e) => this.AutosaveCheckpoint();

        void AutosaveCheckpoint() {
            if (this.Autosave)
                this.ScheduleSave();
        }

        const int FileShareViolation = unchecked((int)0x80070020);

        public void ScheduleSave()
        {
            var frozenCopy = this.freezer(this.Value);

            async Task<Exception?> TrySave()
            {
                try {
                    using (var stream = this.file.Open(FileMode.Create)) {
                        await this.serializer(stream, frozenCopy).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);
                    }
                    return null;
                } catch (IOException e) when (e.HResult == FileShareViolation) {
                    return e;
                }
            }
            this.autosaveService.Chain(() => Retry(TrySave));
        }

        static async Task Retry(Func<Task<Exception?>> action, int attemptCount = 5, int initialRetryDelayMs = 250)
        {
            if (attemptCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(attemptCount));
            if (initialRetryDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(initialRetryDelayMs));

            Exception? lastError = new InvalidProgramException();
            for (int i = 0; i < attemptCount; i++)
            {
                lastError = await action().ConfigureAwait(false);
                if (lastError == null)
                    return;

                await Task.Delay(initialRetryDelayMs).ConfigureAwait(false);
                initialRetryDelayMs *= 2;
            }

            throw lastError;
        }

        public T Value { get; }

        public Task DisposeAsync()
        {
            this.changeListener?.Dispose();
            return this.autosaveService.DisposeAsync();
        }

        void AutosaveService_TaskException(object sender, UnobservedTaskExceptionEventArgs e)
            => this.SaveException?.Invoke(this, e);

        void OnPropertyChanged([CallerMemberName] string propertyName = null!) {
            if (propertyName is null) throw new ArgumentNullException(nameof(propertyName));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
