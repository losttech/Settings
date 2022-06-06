namespace LostTech.App
{
    using System;

    /// <summary>
    /// Constructs freezers (functions, that create readonly snapshots)
    /// </summary>
    public interface IFreezerFactory
    {
        /// <summary>
        /// Create a freezer for the given type
        /// </summary>
        /// <typeparam name="T">Type of the objects, that needs to be snapshotted.</typeparam>
        /// <typeparam name="TFrozen">Type of the resulting snapshot (usually same as <typeparamref name="T"/>)</typeparam>
        /// <returns>
        /// Function, that creates readonly (frozen, <typeparamref name="TFrozen"/>)
        /// copies of the original objects of type <typeparamref name="T"/>.</returns>
        Func<T, TFrozen> MakeFreezer<T, TFrozen>();
    }
}
