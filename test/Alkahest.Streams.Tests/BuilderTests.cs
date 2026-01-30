namespace Alkahest.Streams.Tests;

using Mock;

using Xunit.Runner.Common;

public class BuilderTests
{
    [Fact]
    public void Builder_Can_Create_Custom_Transformer()
    {
        // Assemble
        var builder = new TransformBuilder()
                .ProcessWith<PassThruHandler>()
                .UseStrategy(HandlerStrategy.Buffered);

        // Act
        TestTransformer customTransformer = builder.Build<TestTransformer>();

        // Assert
        Assert.True(customTransformer is not null);
        Assert.True(customTransformer is TestTransformer);
        Assert.Single(customTransformer.GetHandlers().OfType<PassThruHandler>());
        Assert.Equal(HandlerStrategy.Buffered, customTransformer.GetHandlerStrategy());
    }

    [Fact]
    public async Task Builder_Collects_EventHandlers_To_Assign()
    {
        // Assemble
        DateTimeOffset? transformStart,
                        handlerStart,
                        handlerEnd,
                        transformEnd;

        TestTransformBuilder builder = new();

        builder.AddTransformBeginEventListener((s, e) => transformStart = e.Timestamp)
               .AddHandlerBeginEventListener((s, e) => handlerStart = e.Timestamp)
               .AddHandlerEndEventListener((o, e) => handlerEnd = e.Timestamp)
               .AddTransformEndEventListener((s, e) => transformEnd = e.Timestamp);

        // Act
        TestTransformer transformer = builder.Build<TestTransformer>();

        // Assert
        Assert.NotNull(builder.GetTransformBeginEventBindings());
        Assert.NotEmpty(builder.GetTransformBeginEventBindings());
        Assert.NotNull(builder.GetHandlerBeginEventBindings());
        Assert.NotEmpty(builder.GetHandlerBeginEventBindings());
        Assert.NotNull(builder.GetHandlerEndEventBindings());
        Assert.NotEmpty(builder.GetHandlerEndEventBindings());
        Assert.NotNull(builder.GetTransformEndEventBindings());
        Assert.NotEmpty(builder.GetTransformEndEventBindings());
    }

    [Fact]
    public async Task Builder_Adds_Handlers()
    {
        // Assemble
        TestTransformBuilder builder = new();

        // Act
        // Adding handlers w/ chaining
        builder.ProcessWith<StallingHandler>()
               .ProcessWith<StallingHandler, StallOptions>(StallOptions.Default)
               .ProcessWith<StallingHandler, StallOptions>(opts =>
               {
                   opts.Id = Guid.NewGuid();
                   opts.Delay = TimeSpan.FromSeconds(20);
               });

        // Adding handlers directly using non-chaining method.
        builder.AddStreamHandler<StallingHandler>();
        builder.AddStreamHandler<StallingHandler, StallOptions>(StallOptions.Default);
        // NOTE: Configure works reliably with class types, not struct based options.
        builder.AddStreamHandler<StallingHandler, StallOptions>((opts) =>
        {
            opts.Id = Guid.NewGuid();
            opts.Delay = TimeSpan.FromSeconds(30);
        });

        // Assert
        Assert.NotNull(builder.GetHandlers());
        Assert.NotEmpty(builder.GetHandlers());
    }

    [Fact]
    public async Task Adding_Handler_With_Options_Persists_Configuration()
    {
        // Assemble
        Guid id = Guid.Parse("019c0f1a-d659-7d9b-8a21-3dde9d286588");
        TimeSpan delay = TimeSpan.FromMilliseconds(300);
        TestTransformBuilder builder = new();

        // Act
        builder.AddStreamHandler<StallingHandler, StallOptions>(opts =>
        {
            opts.Id = id;
            opts.Delay = delay;
        });

        var handlers = builder.GetHandlers();

        // Assert
        Assert.NotNull(handlers);
        Assert.NotEmpty(handlers);
        Assert.Single(handlers);
        Assert.NotNull((StallOptions?)handlers[0].Options.Value);
        Assert.Equal(id, ((StallOptions?)handlers[0].Options.Value!).Id);
        Assert.Equal(delay, ((StallOptions?)handlers[0].Options.Value!).Delay);
        Assert.NotEqual(StallOptions.Default, (StallOptions?)handlers[0].Options.Value);
        Assert.NotEqual(new StallOptions(), (StallOptions?)handlers[0].Options.Value);
    }

}
