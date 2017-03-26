namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public interface IDeserializerFactory
    {
        Func<Stream, Task<T>> MakeDeserializer<T>();
    }
}
