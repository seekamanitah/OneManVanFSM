namespace OneManVanFSM.Services;

/// <summary>
/// Stores pending mutations (writes) that couldn't reach the server due to
/// network unavailability. The queue is drained when connectivity returns,
/// typically at the start of a full sync.
/// </summary>
public interface IOfflineQueueService
{
    /// <summary>Number of pending mutations in the queue.</summary>
    int PendingCount { get; }

    /// <summary>Add a mutation to the offline queue.</summary>
    void Enqueue(OfflineQueueItem item);

    /// <summary>Attempt to replay all queued mutations against the server. Returns the number of successfully processed items.</summary>
    Task<int> ProcessQueueAsync();

    /// <summary>Get all pending items (for diagnostics / UI).</summary>
    List<OfflineQueueItem> GetPending();
}

public class OfflineQueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public string HttpMethod { get; set; } = "POST";
    public string Endpoint { get; set; } = "";
    public string? PayloadJson { get; set; }
    public string Description { get; set; } = "";
    public int RetryCount { get; set; }
}
