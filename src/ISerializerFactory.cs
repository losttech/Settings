namespace LostTech.App
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Constructs deserializers
    /// </summary>
    public interface ISerializerFactory
    {
        /// <summary>
        /// Create a deserializer for the given type.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize.</typeparam>
        /// <returns>Function, that asynchronously reads an object of type
        /// <typeparamref name="T"/> from a given stream.</returns>
        Func<Stream, T, Task> MakeSerializer<T>();
    }
}
