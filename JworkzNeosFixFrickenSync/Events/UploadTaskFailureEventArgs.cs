using FrooxEngine;

namespace JworkzNeosMod.Events
{
    public class UploadTaskFailureEventArgs : UploadTaskEventArgsBase
    {
        /// <summary>
        /// The reason for the upload task failure.
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Initializes a new instances of UploadTaskFailureEventArgs that contains the reason why
        /// the upload task failed.
        /// </summary>
        /// <param name="record">The associated Neos Record that encountered a sync failure.</param>
        /// <param name="failureReason">The reason why the sync failed for this record.</param>
        public UploadTaskFailureEventArgs(Record record, string failureReason) : base(record)
        {
            FailureReason = failureReason;
        }
    }
}