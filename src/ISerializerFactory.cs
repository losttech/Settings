namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public interface ISerializerFactory
    {
        Func<Stream, T, Task> MakeSerializer<T>();
    }
}
