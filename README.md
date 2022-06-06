[![NuGet Package](https://img.shields.io/nuget/v/LostTech.App.Settings)](https://www.nuget.org/packages/LostTech.App.Settings/)

## Quickstart

1. Implement `ICopyable` from `LostTech.App.DataBinding` on your settings objects
(required to create settings snapshot which can be saved while user continues to modify settings).

2. Install [XmlSettings NuGet package](https://www.nuget.org/packages/LostTech.App.XmlSettings/) (easy serialization into XML).

3. Choose a folder to store your settings and call `XmlSettings.Create(folder)`.

4. For each settings set (e.g. a group of settings), call `cfg = await settings.LoadOrCreate<T>(xmlFileName)`.
Handle errors here (abrupt shutdown or app crash could have corrupted the settings file).

5. Call `cfg.ScheduleSave()` to save the settings set asynchronously.

6. (optional) Set `cfg.Autosave = true` to enable asynchronous autosave.
Your settings must implement [INotifyPropertyChanged](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged)
or [INotifyCollectionChanged](https://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.inotifycollectionchanged).
You **must** implement these interfaces for any nested objects if you have a hierarchy.

6. When the app is getting closed, call `DisposeAsync` for every set of settings.
You **MUST** wait for it to finish before your app closes.