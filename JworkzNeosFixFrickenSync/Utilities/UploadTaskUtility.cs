using BaseX;
using FrooxEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JworkzNeosMod.Utilities
{
    public static class UploadTaskUtility
    {
        private static FieldInfo _recordsToSyncField = AccessTools.Field(typeof(RecordManager), "recordsToSync");

        private static FieldInfo _rawSpinQueue = AccessTools.Field(typeof(SpinQueue<EngineRecordUploadTask>), "queue");

        public static IEnumerable<EngineRecordUploadTask> GetRecordsToSync(this RecordManager recordManager)
        {
            var recordsQueue = (SpinQueue<EngineRecordUploadTask>) _recordsToSyncField.GetValue(recordManager);
            var rawQueue = (Queue<EngineRecordUploadTask>) _rawSpinQueue.GetValue(recordsQueue);

            return rawQueue?.ToList() ?? new EngineRecordUploadTask[0].AsEnumerable();
        }
    }
}
