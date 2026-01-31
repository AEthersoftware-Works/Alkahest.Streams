# Alkahest.Streams
Alkahest: Universal stream transmutation. High-throughput pipelines for compression, encryption, and beyond.


---
## Handler Strategy: Buffered vs Pipelined Strategy

- **Buffered**

  Uses `PooledMemoryStream` intermediates. Best for small/medium data <= 25 MB with a maximum threshold set 1.5 GB.

- **Pipelined**

  Uses `System.IO.Pipelines` for constant-memory streaming. Best for large/unknown-size data.

- **Auto** (default)

  Chooses buffered if stream is seekable and <= 25 MB; otherwise pipelined.

**Recommendation**:
- Use `Pipelined` explicitly for larger streams e.g. large S3 blob uploads.
- Override with `.UseStrategy(HandlerStrategy.Pipelined)` when in doubt.

> Note: If you see `BufferedStrategyNotSuitableException`, use `HandlerStrategy.Auto` or explicitly use `HandlerStrategy.Pipelined`


```csharp
builder.UseStrategy(HandlerStrategy.Pipelined);
```
