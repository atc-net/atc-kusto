namespace Atc.Kusto.Ingestion.Internal;

/// <summary>
/// Wraps a caller's stream so that disposing or closing the wrapper leaves the stream open.
/// </summary>
/// <remarks>
/// The Ingest V2 SDK (0.0.9) closes the stream of a <c>StreamSource</c> when it ingests it queued, even
/// when the source is created with <c>leaveOpen: true</c>. The caller owns the stream, so the SDK gets
/// this wrapper instead. <see cref="Stream.Dispose()"/>, <see cref="Stream.Close"/> and
/// <see cref="Stream.DisposeAsync"/> are not passed through, as <see cref="Stream"/>'s own implementations
/// release nothing; everything else is.
/// </remarks>
internal sealed class NonClosingStream : Stream
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "OK - the caller owns the stream, and keeping it open is the purpose of this class.")]
    private readonly Stream inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="NonClosingStream"/> class.
    /// </summary>
    /// <param name="inner">The caller's stream, which stays open when this wrapper is disposed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is <see langword="null"/>.</exception>
    public NonClosingStream(Stream inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        this.inner = inner;
    }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => inner.CanWrite;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override void Flush()
        => inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => inner.FlushAsync(cancellationToken);

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
        => inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer)
        => inner.Read(buffer);

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
        => inner.ReadAsync(buffer, cancellationToken);

    public override int ReadByte()
        => inner.ReadByte();

    public override void CopyTo(
        Stream destination,
        int bufferSize)
        => inner.CopyTo(destination, bufferSize);

    public override Task CopyToAsync(
        Stream destination,
        int bufferSize,
        CancellationToken cancellationToken)
        => inner.CopyToAsync(destination, bufferSize, cancellationToken);

    public override long Seek(
        long offset,
        SeekOrigin origin)
        => inner.Seek(offset, origin);

    public override void SetLength(long value)
        => inner.SetLength(value);

    public override void Write(
        byte[] buffer,
        int offset,
        int count)
        => inner.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer)
        => inner.Write(buffer);

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => inner.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
        => inner.WriteAsync(buffer, cancellationToken);

    public override void WriteByte(byte value)
        => inner.WriteByte(value);
}