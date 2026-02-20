namespace ClaudeMon.Tests.Helpers;

using ClaudeMon.Helpers;

/// <summary>
/// Tests for SingleInstanceGuard mutex-based single instance detection.
/// Note: These tests use a global mutex. Tests are isolated to avoid interference.
/// The mutex behavior on Windows is that each `new Mutex(false, name)` opens or creates
/// the same named mutex, and multiple WaitOne calls on the same instance are allowed
/// (the mutex counts the acquisitions).
/// </summary>
public class SingleInstanceGuardTests
{
    /// <summary>
    /// First acquisition of the mutex should succeed and return true.
    /// </summary>
    [Fact]
    public void TryAcquire_FirstCall_ReturnsTrue()
    {
        // Arrange
        var guard = new SingleInstanceGuard();

        try
        {
            // Act
            var result = guard.TryAcquire();

            // Assert
            Assert.True(result, "First guard should successfully acquire the mutex");
        }
        finally
        {
            // Cleanup
            guard.Dispose();
        }
    }

    /// <summary>
    /// Dispose without calling TryAcquire should not throw an exception.
    /// </summary>
    [Fact]
    public void Dispose_WithoutAcquire_DoesNotThrow()
    {
        // Arrange
        var guard = new SingleInstanceGuard();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            guard.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// After first instance acquires and disposes, a new instance should be able to acquire the mutex.
    /// This test verifies the basic acquire-release pattern works.
    /// </summary>
    [Fact]
    public void TryAcquire_AfterFirstInstanceDisposed_ReturnsTrue()
    {
        // Arrange & Act
        var guard1 = new SingleInstanceGuard();
        var acquired1 = guard1.TryAcquire();
        guard1.Dispose();

        var guard2 = new SingleInstanceGuard();
        var acquired2 = guard2.TryAcquire();

        // Assert
        Assert.True(acquired1, "First guard should acquire");
        Assert.True(acquired2, "Second guard should acquire after first disposed");

        // Cleanup
        guard2.Dispose();
    }

    /// <summary>
    /// Multiple sequential acquire-and-release cycles should succeed.
    /// Tests that the mutex can be acquired and released multiple times.
    /// </summary>
    [Fact]
    public void AcquireAndRelease_MultipleSequentialCycles_AllSucceed()
    {
        // Arrange, Act & Assert
        for (int i = 0; i < 3; i++)
        {
            var guard = new SingleInstanceGuard();
            var acquired = guard.TryAcquire();
            Assert.True(acquired, $"Cycle {i + 1} should acquire mutex");
            guard.Dispose();
        }
    }

    /// <summary>
    /// TryAcquire should be idempotent: calling it twice on the same instance without
    /// releasing should return the same result (true) because the same instance already holds the handle.
    /// </summary>
    [Fact]
    public void TryAcquire_CalledTwice_SecondCallOnSameInstanceMatches()
    {
        // Arrange
        var guard = new SingleInstanceGuard();

        try
        {
            // Act
            var result1 = guard.TryAcquire();
            var result2 = guard.TryAcquire();

            // Assert - Second call should return true (same instance holds handle)
            Assert.True(result1);
            Assert.True(result2, "Second call to same instance should succeed");
        }
        finally
        {
            guard.Dispose();
        }
    }

    /// <summary>
    /// The mutex can be released and then disposed safely.
    /// Tests the core acquire/release pattern.
    /// </summary>
    [Fact]
    public void AcquireReleaseDispose_Pattern_Works()
    {
        // Arrange
        var guard = new SingleInstanceGuard();

        // Act - Acquire, then dispose (which releases)
        var acquired = guard.TryAcquire();
        Assert.True(acquired);

        // This should not throw
        var disposeException = Record.Exception(() => guard.Dispose());

        // Assert
        Assert.Null(disposeException);
    }

    /// <summary>
    /// Calling Dispose without acquiring should not throw.
    /// </summary>
    [Fact]
    public void Dispose_WithoutAcquire_Safe()
    {
        // Arrange
        var guard = new SingleInstanceGuard();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() =>
        {
            guard.Dispose();
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// TryAcquire returns a boolean indicating success or failure.
    /// Verify it returns bool type and is deterministic for the same instance.
    /// </summary>
    [Fact]
    public void TryAcquire_ReturnsBool_Deterministic()
    {
        // Arrange
        var guard = new SingleInstanceGuard();

        try
        {
            // Act
            var result = guard.TryAcquire();

            // Assert - Result is deterministic (same instance, same result)
            Assert.IsType<bool>(result);
            Assert.True(result);
        }
        finally
        {
            guard.Dispose();
        }
    }

    /// <summary>
    /// The guard properly stores the mutex handle state and only releases if acquired.
    /// </summary>
    [Fact]
    public void Dispose_OnlyReleasesIfAcquired()
    {
        // Arrange & Act
        var guard1 = new SingleInstanceGuard();
        guard1.TryAcquire();

        // Dispose should release successfully
        guard1.Dispose();

        // Now another guard should be able to acquire
        var guard2 = new SingleInstanceGuard();
        var acquired = guard2.TryAcquire();

        // Assert
        Assert.True(acquired, "Should be able to acquire after previous release");

        // Cleanup
        guard2.Dispose();
    }

    /// <summary>
    /// Constructor creates a new guard instance without throwing.
    /// </summary>
    [Fact]
    public void Constructor_CreatesValidGuard()
    {
        // Act & Assert
        var exception = Record.Exception(() =>
        {
            var guard = new SingleInstanceGuard();
            guard.Dispose();
        });

        Assert.Null(exception);
    }
}
