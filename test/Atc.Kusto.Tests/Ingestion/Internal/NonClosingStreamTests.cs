namespace Atc.Kusto.Tests.Ingestion.Internal;

public sealed class NonClosingStreamTests
{
    [Fact]
    public void Dispose_Leaves_The_Inner_Stream_Open()
    {
        // Arrange
        using var inner = new MemoryStream([1, 2, 3]);
        var sut = new NonClosingStream(inner);

        // Act
        sut.Dispose();

        // Assert
        inner.CanRead.Should().BeTrue();
    }

    [Fact]
    public void Close_Leaves_The_Inner_Stream_Open()
    {
        // Arrange
        using var inner = new MemoryStream([1, 2, 3]);
        var sut = new NonClosingStream(inner);

        // Act
        sut.Close();

        // Assert
        inner.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_Leaves_The_Inner_Stream_Open()
    {
        // Arrange
        await using var inner = new MemoryStream([1, 2, 3]);
        var sut = new NonClosingStream(inner);

        // Act
        await sut.DisposeAsync();

        // Assert
        inner.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task Reads_And_Seeks_The_Inner_Stream()
    {
        // Arrange
        await using var inner = new MemoryStream([1, 2, 3, 4]);
        inner.Position = 1;
        await using var sut = new NonClosingStream(inner);

        // Act
        var buffer = new byte[2];
        var read = await sut.ReadAsync(buffer, TestContext.Current.CancellationToken);

        // Assert
        read.Should().Be(2);
        buffer.Should().Equal(2, 3);
        sut.Length.Should().Be(4);
        sut.CanSeek.Should().BeTrue();
        sut.Position.Should().Be(3);
        inner.Position.Should().Be(3);

        sut.Seek(0, SeekOrigin.Begin);
        inner.Position.Should().Be(0);
    }

    [Fact]
    public void Constructor_Throws_For_A_Null_Stream()
    {
        // Act
        var act = () => new NonClosingStream(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}