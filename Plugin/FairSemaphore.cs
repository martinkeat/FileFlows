using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FileFlows.Plugin;


/// <summary>
/// Represents a fair semaphore that ensures threads are granted access in the order they requested it.
/// </summary>
public class FairSemaphore
{
    private readonly SemaphoreSlim semaphore;

    private readonly ConcurrentQueue<TaskCompletionSource<bool>> queue = new();

    public Queue<string> StackTraces = new Queue<string>();
    
    /// <summary>
    /// Gets the current queue length
    /// </summary>
    public int CurrentQueueLength => queue.Count();

    /// <summary>
    /// Gets the number currently in use
    /// </summary>
    public int CurrentInUse => max - semaphore.CurrentCount;

    /// <summary>
    /// Gets if the current semaphore is locked and no more available
    /// </summary>
    public bool IsLocked => semaphore.CurrentCount < 1;

    private int max;
    
    /// <summary>
    /// Initializes a new instance of the FairSemaphore class with the specified initial count.
    /// </summary>
    /// <param name="initialCount">The initial number of slots that can be acquired concurrently.</param>
    public FairSemaphore(int initialCount)
    {
        max = initialCount;
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
        StackTraces.Enqueue(GetStackTrace());
        while (StackTraces.Count > 5)
            StackTraces.Dequeue();
        queue.Enqueue(tcs);
        await semaphore.WaitAsync();
        if (queue.TryDequeue(out var popped))
            popped.SetResult(true);
    }


    private string GetStackTrace()
    {
        // Get the stack trace
        StackTrace stackTrace = new StackTrace(true);

        // Convert the stack trace to a string
        return stackTrace.ToString();
    }
    
    /// <summary>
    /// Releases one slot of the semaphore, allowing one waiting thread to enter.
    /// </summary>
    public void Release()
    {
        semaphore.Release();
    }
}