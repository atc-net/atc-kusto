namespace Atc.Kusto.Tests.Handlers.Internal;

/// <summary>
/// A <see cref="ProgressiveDataSet"/> that records whether the data set and its frame enumerator were disposed.
/// </summary>
internal sealed class TrackedProgressiveDataSet : IDisposable
{
    private readonly TrackingFrameEnumerator frames;

    public TrackedProgressiveDataSet(params string[] values)
        : this(ProgressiveDataSetBuilder.BuildPrimaryResultFrames(values))
    {
    }

    public TrackedProgressiveDataSet(
        IEnumerable<ProgressiveDataSetFrame> frames)
    {
        this.frames = new TrackingFrameEnumerator(frames);
        DataSet = new ProgressiveDataSet(this.frames);
        DataSet.Disposed += () => IsDataSetDisposed = true;
    }

    public ProgressiveDataSet DataSet { get; }

    public bool IsDataSetDisposed { get; private set; }

    public bool AreFramesDisposed => frames.IsDisposed;

    public void Dispose()
    {
        frames.Dispose();
        DataSet.Dispose();
    }

    private sealed class TrackingFrameEnumerator : IEnumerator<ProgressiveDataSetFrame>
    {
        private readonly IEnumerator<ProgressiveDataSetFrame> inner;

        public TrackingFrameEnumerator(
            IEnumerable<ProgressiveDataSetFrame> frames)
            => inner = frames.GetEnumerator();

        public bool IsDisposed { get; private set; }

        public ProgressiveDataSetFrame Current => inner.Current;

        object IEnumerator.Current => inner.Current;

        public bool MoveNext() => inner.MoveNext();

        public void Reset() => inner.Reset();

        public void Dispose()
        {
            IsDisposed = true;
            inner.Dispose();
        }
    }
}