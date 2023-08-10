using FrooxEngine;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Events
{
    public abstract class UploadTaskEventArgsBase
    {
        /// <summary>
        /// The Neos Record associated with this upload task event.
        /// </summary>
        public Record Record { get; }

        /// <summary>
        /// The stage and progress of the sync when this event was triggered.
        /// </summary>
        public UploadProgressState ProgressState { get; }

        /// <summary>
        /// Initializes a new instance that inherits UploadTaskEventArgsBase that holds information
        /// for a specified Neos Record.
        /// </summary>
        /// <param name="record">The associated Neos Record.</param>
        /// <param name="progressState">The current stage and progress that is associated with the sync.</param>
        public UploadTaskEventArgsBase(Record record, UploadProgressState progressState)
        {
            Record = record;
            ProgressState = progressState;
        }
    }
}