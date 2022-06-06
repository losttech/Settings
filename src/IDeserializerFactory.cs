namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Constructs deserializers
    /// </summary>
    public interface IDeserializerFactory
    {
        /// <summary>
        /// Create a deserializer for the given type.
        /// </summary>
        Func<Stream, Task<T>> MakeDeserializer<T>();
    }
}
