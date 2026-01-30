using System;

using Alkahest.Streams.Tests.Mock;

namespace Alkahest.Streams.Tests;

public class HandlerTests
{
    [Fact]
    public void Ensure_Handler_Options_Wrapped()
    {
        // Assemble
        StallOptions defaultOptions = new() { Delay = TimeSpan.FromDays(1) };
        IHandlerOptions<StallOptions>? stallOptions = null;

        // Act
        stallOptions = HandlerOptions.Create(defaultOptions);

        // Assert
        Assert.NotNull(stallOptions);
        Assert.NotNull(stallOptions.Value);
        Assert.Equal(defaultOptions, stallOptions.Value);
    }
}
