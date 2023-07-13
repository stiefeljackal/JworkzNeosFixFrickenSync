using FrooxEngine;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Events
{
    public class UploadTaskProgressEventArgs : UploadTaskEventArgsBase
    {
        /// <summary>
        /// The stage and progress of the sync when this event was triggered.
        /// </summary>
        public UploadProgressState ProgressState { get; }

        /// <summary>
        /// Initializes a new instance of UploadTaskProgressEventArgs that contains the current
        /// stage and progress of a Neos Record sync.
        /// </summary>
        /// <param name="record">The associated Neos Record that is currently syncing.</param>
        /// <param name="progressState">The stage and progress the task is at.</param>
        public UploadTaskProgressEventArgs(Record record, UploadProgressState progressState) : base(record)
        {
            ProgressState = progressState;
        }
    }
}