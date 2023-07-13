using FrooxEngine;

namespace JworkzNeosMod.Events
{
    public class UploadTaskSuccessEventArgs : UploadTaskEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of UploadTaskSuccessEventArgs that decalres the sync for
        /// this record was successful.
        /// </summary>
        /// <param name="record">The associated Neos Record that was synced successfully.</param>
        public UploadTaskSuccessEventArgs(Record record) : base(record) { }
    }
}