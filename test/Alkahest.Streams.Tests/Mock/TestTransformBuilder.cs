using System;

using Alkahest.Streams.Events;

namespace Alkahest.Streams.Tests.Mock;

/// <summary>
/// Test Implementation of <see cref="TransformBuilder"/> as a convenience method to confirm internal configuration;
/// </summary>
internal class TestTransformBuilder : TransformBuilder
{
    public HandlerStrategy GetHandlerStrategy() => Strategy;
    public List<ITransformHandler> GetHandlers() => Handlers;
    public List<EventHandler<TransformBeginEventArgs>> GetTransformBeginEventBindings() => TransformBeginEventBindings;
    public List<EventHandler<HandlerBeginEventArgs>> GetHandlerBeginEventBindings() => HandlerBeginEventBindings;
    public List<EventHandler<HandlerEndEventArgs>> GetHandlerEndEventBindings() => HandlerEndEventBindings;
    public List<EventHandler<TransformEndEventArgs>> GetTransformEndEventBindings() => TransformEndEventBindings;
}
