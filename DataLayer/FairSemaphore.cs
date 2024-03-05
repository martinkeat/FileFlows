using System.Collections.Concurrent;

namespace FileFlows.DataLayer;


/// <summary>
/// Represents a fair semaphore that ensures threads are granted access in the order they requested it.
/// </summary>
public class FairSemaphore
{
    private readonly SemaphoreSlim semaphore;

    private readonly ConcurrentQueue<TaskCompletionSource<bool>> queue =
        new ConcurrentQueue<TaskCompletionSource<bool>>();

    /// <summary>
    /// Initializes a new instance of the FairSemaphore class with the specified initial count.
    /// </summary>
    /// <param name="initialCount">The initial number of slots that can be acquired concurrently.</param>
    public FairSemaphore(int initialCount)
    {
        semaphore = new SemaphoreSlim(initialCount);
    }

    /// <summary>
    /// Initializes a new instance of the FairSemaphore class with the specified initial and maximum counts.
    /// </summary>
    /// <param name="initialCount">The initial number of slots that can be acquired concurrently.</param>
    /// <param name="maxCount">The maximum number of slots that can be acquired concurrently.</param>
    public FairSemaphore(int initialCount, int maxCount)
    {
        semaphore = new SemaphoreSlim(initialCount, maxCount);
    }

    /// <summary>
    /// Asynchronously waits to enter the semaphore.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of acquiring the semaphore.</returns>
    public async Task WaitAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        queue.Enqueue(tcs);
        await semaphore.WaitAsync();
        TaskCompletionSource<bool> popped;
        if (queue.TryDequeue(out popped))
            popped.SetResult(true);
    }

    /// <summary>
    /// Releases one slot of the semaphore, allowing one waiting thread to enter.
    /// </summary>
    public void Release()
    {
        semaphore.Release();
    }
}