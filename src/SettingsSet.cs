﻿namespace LostTech.App
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using JetBrains.Annotations;
    using LostTech.Checkpoint;
    using ThomasJaworski.ComponentModel;
    using Stream = System.IO.Stream;

    public sealed class SettingsSet<T, TFreezed>: ISettingsSet, INotifyPropertyChanged
        where T: class
    {
        readonly FileInfo file;
        readonly AsyncChainService autosaveService = new AsyncChainService();
        readonly ChangeListener changeListener;
        readonly Func<T, TFreezed> freezer;
        readonly Func<Stream, TFreezed, Task> serializer;
        bool autosave;

        internal SettingsSet([NotNull] FileInfo file, [NotNull] T value,
            [NotNull] Func<T, TFreezed> freezer, [NotNull] Func<Stream, TFreezed, Task> serializer)
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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Autosave
        {
            get {
                return this.autosave;
            }
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

            async Task<Exception> TrySave()
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

        static async Task Retry(Func<Task<Exception>> action, int attemptCount = 5, int initialRetryDelayMs = 250)
        {
            if (attemptCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(attemptCount));
            if (initialRetryDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(initialRetryDelayMs));

            Exception lastError = new InvalidProgramException();
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

        [NotNull]
        public T Value { get; }

        public Task DisposeAsync()
        {
            this.changeListener?.Dispose();
            return this.autosaveService.DisposeAsync();
        }

        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
