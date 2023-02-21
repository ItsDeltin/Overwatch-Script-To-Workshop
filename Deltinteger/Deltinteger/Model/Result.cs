namespace Deltin.Model;
using System;

struct Result<T, E>
{
    public static Result<T, E> Ok(T value) => new Result<T, E>(value);
    public static Result<T, E> Error(E error) => new Result<T, E>(error);

    readonly bool success;
    readonly T value;
    readonly E error;

    public Result(T value)
    {
        this.success = true;
        this.value = value;
        this.error = default(E);
    }

    public Result(E error)
    {
        this.success = false;
        this.value = default(T);
        this.error = error;
    }

    public void Match(Action<T> ok, Action<E> error)
    {
        if (this.success)
            ok(this.value);
        else
            error(this.error);
    }
}