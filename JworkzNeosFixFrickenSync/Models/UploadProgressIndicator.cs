namespace JworkzNeosMod.Models
{
    /// <summary>
    /// Indicator values for an upload sync task that defines the current state of the sync task.
    /// </summary>
    public enum UploadProgressIndicator
    {
        InProgress,
        Success,
        Failure,
        Canceled
    }
}
