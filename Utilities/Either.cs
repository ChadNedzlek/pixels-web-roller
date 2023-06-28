using System;

namespace Utilities;

public readonly struct Either<T1, T2>
{
    private readonly bool _init;
    private readonly bool _isValue2;
    private readonly T1 _v1;
    private readonly T2 _v2;

    private Either(T1 v1)
    {
        _init = true;
        _isValue2 = false;
        _v1 = v1;
    }
    
    private Either(T2 v2)
    {
        _init = true;
        _isValue2 = true;
        _v2 = v2;
    }

    public TOutput Match<TOutput>(Func<T1, TOutput> f1, Func<T2, TOutput> f2)
    {
        if (!_init)
            throw new InvalidOperationException("Uninitialized Either");
        
        if (_isValue2)
            return f2(_v2);
        return f1(_v1);
    }

    public void Match<TOutput>(Action<T1> f1, Action<T2> f2)
    {
        if (!_init)
            throw new InvalidOperationException("Uninitialized Either");

        if (_isValue2)
            f2(_v2);
        else
            f1(_v1);
    }

    public static implicit operator Either<T1, T2>(T1 t) => new(t);
    public static implicit operator Either<T1, T2>(T2 u) => new(u);
}