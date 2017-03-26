namespace LostTech.App
{
    using System;

    public interface IFreezerFactory
    {
        Func<T, TFreezed> MakeFreezer<T, TFreezed>();
    }
}
