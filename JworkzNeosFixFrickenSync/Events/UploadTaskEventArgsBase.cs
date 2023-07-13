using FrooxEngine;

namespace JworkzNeosMod.Events
{
    public abstract class UploadTaskEventArgsBase
    {
        /// <summary>
        /// The Neos Record associated with this upload task event.
        /// </summary>
        public Record Record { get; }

        /// <summary>
        /// Initializes a new instance that inherits UploadTaskEventArgsBase that holds information
        /// for a specified Neos Record.
        /// </summary>
        /// <param name="record">The associated Neos Record.</param>
        public UploadTaskEventArgsBase(Record record)
        {
            Record = record;
        }
    }
}