namespace ProductBundles.Sdk;

/// <summary>
/// Represents a recurring background job with a name and cron schedule
/// </summary>
public class RecurringBackgroundJob
{
    /// <summary>
    /// Gets the name of the recurring background job
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the cron expression that defines when this job should run
    /// Format: "minute hour day month day-of-week"
    /// Examples: "*/5 * * * *" (every 5 minutes), "0 9 * * 1-5" (weekdays at 9 AM)
    /// </summary>
    public string CronSchedule { get; }
    
    /// <summary>
    /// Gets the optional description of what this background job does
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Gets additional parameters or configuration for the job
    /// </summary>
    public Dictionary<string, object?> Parameters { get; }
    
    /// <summary>
    /// Initializes a new instance of the RecurringBackgroundJob class
    /// </summary>
    /// <param name="name">The name of the recurring background job</param>
    /// <param name="cronSchedule">The cron expression for scheduling</param>
    /// <param name="description">Optional description of the job</param>
    /// <param name="parameters">Optional parameters for the job</param>
    public RecurringBackgroundJob(string name, string cronSchedule, string? description = null, Dictionary<string, object?>? parameters = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CronSchedule = cronSchedule ?? throw new ArgumentNullException(nameof(cronSchedule));
        Description = description;
        Parameters = parameters ?? new Dictionary<string, object?>();
    }
    
    /// <summary>
    /// Returns a string representation of the recurring background job
    /// </summary>
    public override string ToString()
    {
        return $"{Name} ({CronSchedule})";
    }
}
