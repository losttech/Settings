namespace LostTech.App
{
    using System;
    using System.Linq;
    using System.Reflection;
    using LostTech.App.DataBinding;

    /// <summary>
    /// An <see cref="IFreezerFactory"/> for types, that implement <see cref="ICopyable{T}"/>
    /// </summary>
    public sealed class ClonableFreezerFactory : IFreezerFactory
    {
        private ClonableFreezerFactory() { }

        /// <summary>
        /// Singleton of the <see cref="ClonableFreezerFactory"/>
        /// </summary>
        public static IFreezerFactory Instance { get; } = new ClonableFreezerFactory();

        /// <inheritdoc/>
        public Func<T, TFrozen> MakeFreezer<T, TFrozen>()
        {
            if (!typeof(T).GetTypeInfo().ImplementedInterfaces.Any(@interface => @interface == typeof(ICopyable<TFrozen>)))
                throw new NotSupportedException($"This factory requires {nameof(T)} to implement {nameof(ICopyable<TFrozen>)}");

            return value => value is null
                                ? throw new ArgumentNullException(nameof(value))
                                : ((ICopyable<TFrozen>)value).Copy();
        }
    }
}
