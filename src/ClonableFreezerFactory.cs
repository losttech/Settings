namespace LostTech.App
{
    using System;
    using System.Linq;
    using System.Reflection;
    using LostTech.App.DataBinding;

    public sealed class ClonableFreezerFactory : IFreezerFactory
    {
        private ClonableFreezerFactory() { }

        public static IFreezerFactory Instance { get; } = new ClonableFreezerFactory();

        public Func<T, TFreezed> MakeFreezer<T, TFreezed>()
        {
            if (!typeof(T).GetTypeInfo().ImplementedInterfaces.Any(@interface => @interface == typeof(ICopyable<TFreezed>)))
                throw new NotSupportedException($"This factory requires {nameof(T)} to implement {nameof(ICopyable<TFreezed>)}");

            return value => ((ICopyable<TFreezed>)value).Copy();
        }
    }
}
