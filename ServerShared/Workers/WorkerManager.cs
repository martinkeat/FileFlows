namespace FileFlows.Server.Workers;

using FileFlows.ServerShared.Workers;

/// <summary>
/// A manager for the workers that run in the system
/// </summary>
public class WorkerManager
{
    static readonly List<Worker> Workers = new List<Worker>();

    /// <summary>
    /// Starts a list of workers and keeps track of them 
    /// </summary>
    /// <param name="workers">A list of workers to start</param>
    public static void StartWorkers(params Worker[] workers)
    {
        if (workers?.Any() != true)
            return; // workers already running
        foreach (var worker in workers)
        {
            if (worker == null)
                continue;
            Workers.Add(worker);
            worker.Start();
        }
    }
    
    /// <summary>
    /// Stops all the currently running workers
    /// </summary>
    public static void StopWorkers()
    {
        foreach (var worker in Workers)
        {
            if (worker == null)
                continue;
            Logger.Instance.ILog("Stopping worker: " + worker.GetType().Name);
            worker.Stop();
        }
        Workers.Clear();
    }

    /// <summary>
    /// Gets a worker
    /// </summary>
    /// <typeparam name="T">the type of worker to get</typeparam>
    /// <returns>the worker or null if not found</returns>
    public static T? GetWorker<T>() where T : Worker
    {
        var foundWorker = Workers.FirstOrDefault(x => x != null && x.GetType() == typeof(T));
        return foundWorker as T;
    }
}
