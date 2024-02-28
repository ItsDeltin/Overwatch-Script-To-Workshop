#nullable enable

namespace Deltin.Deltinteger.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

static class Result
{
    /// <summary>
    /// Converts `Result(T, E)?` to `Result(T?, E)`.
    /// </summary>
    public static Result<T?, E> Maybe<T, E>(Result<T, E>? result)
    {
        // ok (no value)
        if (result is null)
        {
            return Result<T?, E>.Ok(default);
        }
        // ok (with value)
        else if (result.Value.Get(out var value, out var error))
        {
            return value;
        }
        // error
        else
        {
            return error;
        }
    }

    /// <summary>Applies a selector to each element in a enumerable until the end is reached or an error occurs.</summary>
    /// <typeparam name="TSource">The type of the collection.</typeparam>
    /// <typeparam name="TResult">The new type the collection will be converted into.</typeparam>
    /// <typeparam name="E">The error type that may appear when converting elements.</typeparam>
    /// <param name="collection">This enumerable will be iterated.</param>
    /// <param name="selector">Converts TSource to TResult or E.</param>
    /// <returns></returns>
    public static Result<IEnumerable<TResult>, E> SelectResult<TSource, TResult, E>(this IEnumerable<TSource> collection, Func<TSource, Result<TResult, E>> selector)
    {
        var items = new List<TResult>();
        foreach (var item in collection)
        {
            if (selector(item).Get(out var next, out var error))
                items.Add(next);
            else
                return error;
        }
        return items;
    }

    public static Result<T, string> Try<T>(Func<T> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static T Unwrap<T>(this Result<T, string> result)
    {
        if (result.IsOk)
            return result.Value;
        else
            throw new Exception(result.Err);
    }
}

public readonly struct Result<T, E>
{
    public T Value
    {
        get
        {
            if (!_isOk)
                throw new Exception("Can't get Value of Error result");
            return _value!;
        }
    }
    public E Err
    {
        get
        {
            if (_isOk)
                throw new Exception("Can't get Err of Ok result");
            return _error!;
        }
    }
    public bool IsOk => _isOk;

    readonly T? _value;
    readonly E? _error;
    readonly bool _isOk;

    public Result(T? value, E? error, bool isOk) => (_value, _error, _isOk) = (value, error, isOk);

    public static Result<T, E> Ok(T value) => new(value, default, true);
    public static Result<T, E> Error(E error)
    {
        Debugger.Break();
        return new(default, error, false);
    }

    public void Match(Action<T> onValue, Action<E> onError)
    {
        if (_isOk)
            onValue(_value!);
        else
            onError(_error!);
    }

    public R Match<R>(Func<T, R> onValue, Func<E, R> onError)
    {
        if (_isOk)
            return onValue(_value!);
        else
            return onError(_error!);
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = _value;
        return _isOk;
    }

    public bool Get([NotNullWhen(true)] out T? value, [NotNullWhen(false)] out E? error)
    {
        if (_isOk)
        {
            value = _value!;
            error = default;
            return true;
        }
        else
        {
            value = default;
            error = _error!;
            return false;
        }
    }

    public Result<U, E> MapValue<U>(Func<T, U> map)
    {
        if (_isOk)
            return Result<U, E>.Ok(map(_value!));
        else
            return Result<U, E>.Error(_error!);
    }

    public Result<U, V> Map<U, V>(Func<T, U> mapValue, Func<E, V> mapError)
    {
        if (_isOk)
            return Result<U, V>.Ok(mapValue(_value!));
        else
            return Result<U, V>.Error(mapError(_error!));
    }

    public Result<U, E> AndThen<U>(Func<T, Result<U, E>> then)
    {
        if (_isOk)
            return then(_value!);
        else
            return Result<U, E>.Error(_error!);
    }

    public Result<(T a, U b), E> And<U>(Result<U, E> other)
    {
        if (_isOk)
        {
            if (other._isOk)
                return (_value!, other._value!);
            return other._error!;
        }
        else
            return _error!;
    }

    public void Then(Action<T> onValue)
    {
        if (_isOk)
            onValue(_value!);
    }

    public static implicit operator Result<T, E>(T value) => Ok(value);
    public static implicit operator Result<T, E>(E error) => Error(error);
}