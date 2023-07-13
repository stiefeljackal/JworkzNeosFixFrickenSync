using CloudX.Shared;
using JworkzNeosMod.Events;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngineRecord = FrooxEngine.Record;

namespace JworkzNeosMod.Services
{
    internal class SyncLogger
    {
        /// <summary>
        /// Logs the current progress of the upload task.
        /// </summary>
        /// <param name="_">The object that triggered the event.</param>
        /// <param name="event">The information of the current upload task's progress.</param>
        public static void LogUploadUpdate(object _, UploadTaskProgressEventArgs @event)
        {
            var record = @event.Record;
            var progressState = @event.ProgressState;

            NeosMod.Msg($"Record '{record.Name} ({record.RecordId})' | Progress: {progressState.Progress * 100}% | Stage: {progressState.Stage}");
        }
    }
}
