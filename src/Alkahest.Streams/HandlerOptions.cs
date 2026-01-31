using System.Diagnostics.CodeAnalysis;

namespace Alkahest.Streams;

/// <summary>
/// Represents handler options with a generic value.
/// </summary>
public interface IHandlerOptions
{
    /// <summary>
    /// Gets the value of the handler options.
    /// </summary>
    object? Value { get; }
}

/// <summary>
/// Represents typed handler options.
/// </summary>
/// <typeparam name="THandlerOptions">The type of the handler options.</typeparam>
public interface IHandlerOptions<[DynamicallyAccessedMembers(HandlerOptions.DynamicallyAccessedMembers)] out THandlerOptions> : IHandlerOptions
    //where THandlerOptions : class
{
    /// <summary>
    /// Gets the typed value of the handler options.
    /// </summary>
    new THandlerOptions? Value { get; }

    object? IHandlerOptions.Value => Value;
}

/// <summary>
/// Wraps handler options of a specific type.
/// </summary>
/// <typeparam name="THandlerOptions">The type of the handler options.</typeparam>
public class HandlerOptionsWrapper<[DynamicallyAccessedMembers(HandlerOptions.DynamicallyAccessedMembers)] THandlerOptions>(THandlerOptions options) : IHandlerOptions<THandlerOptions>
    //where THandlerOptions : class
{
    /// <summary>
    /// Gets the wrapped handler options value.
    /// </summary>
    public THandlerOptions? Value { get; } = options;
}

/// <summary>
/// Provides factory methods for creating handler options.
/// </summary>
public static class HandlerOptions
{
    internal const DynamicallyAccessedMemberTypes DynamicallyAccessedMembers = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

    /// <summary>
    /// Gets an empty handler options instance.
    /// </summary>
    public static IHandlerOptions Empty => Create<object>(new { });

    /// <summary>
    /// Creates handler options from the specified options object.
    /// </summary>
    /// <typeparam name="THandlerOptions">The type of the handler options.</typeparam>
    /// <param name="options">The options object.</param>
    /// <returns>A handler options wrapper containing the specified options.</returns>
    public static IHandlerOptions<THandlerOptions> Create<[DynamicallyAccessedMembers(DynamicallyAccessedMembers)] THandlerOptions>(THandlerOptions options)
        // where THandlerOptions : class
    {
        return new HandlerOptionsWrapper<THandlerOptions>(options);
    }
}
