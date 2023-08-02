using FrooxEngine;
using JworkzNeosMod.Models;

namespace JworkzNeosMod.Events
{
    public class UploadTaskStartEventArgs : UploadTaskEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of UploadTaskStartEventArgs that contains the record that is queued
        /// and ready to be synced.
        /// </summary>
        /// <param name="record">The associated Neos Record that is ready to be synced.</param>
        public UploadTaskStartEventArgs(Record record) : base(record, new UploadProgressState("Queued")) { }
    }
}
