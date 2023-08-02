using FrooxEngine;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Events
{
    public class UploadTaskProgressEventArgs : UploadTaskEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of UploadTaskProgressEventArgs that contains the current
        /// stage and progress of a Neos Record sync.
        /// </summary>
        /// <param name="record">The associated Neos Record that is currently syncing.</param>
        /// <param name="progressState">The stage and progress the task is at.</param>
        public UploadTaskProgressEventArgs(Record record, UploadProgressState progressState) : base(record, progressState) { }
    }
}