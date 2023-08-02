using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CloudX.Shared;
using HarmonyLib;
using NeosModLoader;
using JworkzNeosMod.Events;
using JworkzNeosMod.Models;
using FrooxEngine;
using JworkzNeosMod.Utilities;
using FrooxEngineRecord = FrooxEngine.Record;

namespace JworkzNeosMod.Patches
{
    [HarmonyPatch(typeof(RecordUploadTaskBase<FrooxEngineRecord>))]
    public static class RecordUploadTaskBasePatch
    {
        private const byte MIN_MAX_RETRIES = 1;
        private const int PROGRESS_TIMER_INTERVAL_BY_MILLI = 3000;

        private static readonly FieldInfo _completionSourceField = AccessTools.Field(typeof(RecordUploadTaskBase<FrooxEngineRecord>), "_completionSource");

        private static readonly MethodInfo _runUploadInternalMethod = AccessTools.Method(typeof(RecordUploadTaskBase<FrooxEngineRecord>), "RunUploadInternal");
        private static readonly MethodInfo _failedSetter = AccessTools.PropertySetter(typeof(RecordUploadTaskBase<FrooxEngineRecord>), "Failed");
        private static readonly MethodInfo _failReasonSetter = AccessTools.PropertySetter(typeof(RecordUploadTaskBase<FrooxEngineRecord>), "FailReason");
        private static readonly MethodInfo _isFinishedSetter = AccessTools.PropertySetter(typeof(RecordUploadTaskBase<FrooxEngineRecord>), "IsFinished");

        private static readonly Regex _clientErrorMatcher = new Regex(@"state: 4\d\d", RegexOptions.Compiled);
        private static readonly Regex _terminalServerErrorMatcher = new Regex(@"state: (501|505|511)", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<RecordUploadTaskBase<FrooxEngineRecord>, UploadProgressState> _uploadPreviousStagePair =
            new ConcurrentDictionary<RecordUploadTaskBase<FrooxEngineRecord>, UploadProgressState>();


        public static event EventHandler<UploadTaskProgressEventArgs> UploadTaskProgress;
        public static event EventHandler<UploadTaskSuccessEventArgs> UploadTaskSuccess;
        public static event EventHandler<UploadTaskFailureEventArgs> UploadTaskFailure;

        /// <summary>
        /// Patches the RunUpload method of the original by replacing it entirely with a new method that can gracefully handle
        /// thrown errors and initiate retries on appropriate fail states.
        /// </summary>
        /// <param name="__instance">The record upload task that is currently syncing the record.</param>
        /// <param name="__result">The upload task returned by this method.</param>
        /// <param name="cancellationToken">The token that determines if the running task should continue or stop.</param>
        /// <returns>false to replace the original method entirely.</returns>
        [HarmonyPrefix]
        [HarmonyPatch("RunUpload")]
        private static bool RunUploadPrefix(RecordUploadTaskBase<FrooxEngineRecord> __instance, out Task __result, CancellationToken cancellationToken)
        {
            var uploadTask = (Task)_runUploadInternalMethod.Invoke(__instance, new object[] { cancellationToken });
            var completionSource = (TaskCompletionSource<bool>)_completionSourceField.GetValue(__instance);

            // We don't want to let the task terminate early, so the cancellation token is not passed on
            __result = Task.Run
            (
                async () =>
                {
                    var maxRetryCount = JworkzNeosFixFrickenSync.RetryCount <= 0 ? MIN_MAX_RETRIES : JworkzNeosFixFrickenSync.RetryCount;
                    var delay = JworkzNeosFixFrickenSync.RetryDelay;
                    var shouldRetry = true;
                    var record = __instance.Record;
                    var timer = CreateProgressTimer(__instance);

                    for (var retryCount = 0;  retryCount < maxRetryCount && !cancellationToken.IsCancellationRequested; retryCount++)
                    {
                        try
                        {
                            await uploadTask.ConfigureAwait(false);
                            _ = completionSource.TrySetResult(__instance.IsFinished);

                            timer.Dispose();

                            if (!__instance.Failed)
                            {
                                OnUploadTaskSuccess(__instance);
                                return;
                            };
                        }
                        catch (Exception ex)
                        {
                            NeosMod.Error($"Exception during record upload task (attempt {retryCount + 1} out of {maxRetryCount}): {ex}");
                            FailPrefix(__instance, $"{ex.Message}\n{ex.StackTrace}");
                        }

                        shouldRetry = __instance.ShouldRetry();

                        if (!shouldRetry) { break; }

                        await Task.Delay(delay, CancellationToken.None);

                    }

                    // We may not have signalled completion at this point, so if we haven't, notify the system of failure
                    if (!completionSource.Task.IsCompleted)
                    {
                        FailPrefix(__instance,
                        (
                            cancellationToken.IsCancellationRequested
                                ? "Record upload task cancelled"
                                : "Exception during sync."
                        ));
                    }

                    timer.Dispose();
                    OnUploadTaskFailure(__instance, __instance.FailReason);

                },
                CancellationToken.None
            );

            return false;
        }

        /// <summary>
        /// Patches the Fail method by replacing it entirely via Prefix. This was done due to some issues with UniLog in Linux.
        /// </summary>
        /// <param name="__instance">The record upload task instance.</param>
        /// <param name="error">The fail message to set for the task outcome.</param>
        /// <returns>false to replace the entire method.</returns>
        [HarmonyPrefix]
        [HarmonyPatch("Fail")]
        private static bool FailPrefix(RecordUploadTaskBase<FrooxEngineRecord> __instance, string error) 
        {        
            _failedSetter.Invoke(__instance, new object[] { true });
            _failReasonSetter.Invoke(__instance, new object[] { error });
            _isFinishedSetter.Invoke(__instance, new object[] { true });

            var completionSource = (TaskCompletionSource<bool>)_completionSourceField.GetValue(__instance);
            _ = completionSource.TrySetResult(result: false);

            return false;
        }

        /// <summary>
        /// Patches the setter for StageDescription by adding a postfix that will fire an event if the stage
        /// description should update to a newer value.
        /// </summary>
        /// <param name="__instance">The reocrd upload task instance.</param>
        /// <param name="value">The new vlue for StageDescription.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(RecordUploadTaskBase<FrooxEngineRecord>.StageDescription), MethodType.Setter)]
        private static void StageDescriptionSetterPostFix(RecordUploadTaskBase<FrooxEngineRecord> __instance, string value)
        {
            var currentState = new UploadProgressState(value, null, __instance.Progress);
            if (!CanTriggerProgressEvent(__instance, currentState)) { return; }
            OnUploadTaskProgress(__instance, currentState);
        }

        /// <summary>
        /// Determines if an UploadTaskProgress event can be triggered based on the previous state.
        /// </summary>
        /// <param name="task">The upload task that is an candidate for triggering the event.</param>
        /// <param name="currentState">The current progress state of the task.</param>
        /// <returns></returns>
        private static bool CanTriggerProgressEvent(RecordUploadTaskBase<FrooxEngineRecord> task, UploadProgressState currentState)
        {
            UploadProgressState previousState;
            var hasUploadKey = _uploadPreviousStagePair.TryGetValue(task, out previousState);

            if (hasUploadKey && previousState == currentState) { return false; }

            bool canTrigger;
            /// Need to cache as this is called multiple times on different threads (?).
            if (!hasUploadKey)
            {
                canTrigger = _uploadPreviousStagePair.TryAdd(task, currentState);
            }
            else
            {
                canTrigger = _uploadPreviousStagePair.TryUpdate(task, currentState, previousState);
            }

            return canTrigger;
        }

        /// <summary>
        /// Create the timer job that will trigger an UploadTaskProgress event at a certain rate.
        /// </summary>
        /// <param name="task">The upload task that is performing the record sync.</param>
        /// <returns>The Timer instance that will run at a certain rate.</returns>
        private static Timer CreateProgressTimer(RecordUploadTaskBase<FrooxEngineRecord> task) => new Timer((object _) =>
        {
            var currentState = new UploadProgressState(task.StageDescription, null, task.Progress);
            if (!CanTriggerProgressEvent(task, currentState)) { return; }
            OnUploadTaskProgress(task, currentState);
        }, null, PROGRESS_TIMER_INTERVAL_BY_MILLI, PROGRESS_TIMER_INTERVAL_BY_MILLI);

        /// <summary>
        /// Returns a boolean to indicate that the upload task should retry based on the task's fail reason.
        /// </summary>
        /// <param name="uploadTask">The upload task instance</param>
        /// <returns>true if the task should retry due to the fail reason; otherwise, false.</returns>
        private static bool ShouldRetry(this RecordUploadTaskBase<FrooxEngineRecord> uploadTask)
        {
            var failReason = uploadTask.FailReason?.Trim().ToLowerInvariant();

            if (failReason == null) { return false; }

            switch (failReason)
            {
                case var r1 when r1.Contains("conflict"):
                case var r2 when r2.Contains("preprocessing failed"):
                    return false;
                case var r6 when r6.Contains("state: 429"):
                case var r3 when r3.Contains("toomanyrequests"):
                    return true; // catch this before the next line
                case var r4 when _clientErrorMatcher.IsMatch(r4):
                case var r5 when _terminalServerErrorMatcher.IsMatch(r5):
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Triggers an UploadTaskSuccess event for all listeners to indicate that an upload task was successful.
        /// </summary>
        /// <param name="uploadTask">The upload task that was successful.</param>
        private static void OnUploadTaskSuccess(RecordUploadTaskBase<FrooxEngineRecord> uploadTask)
        {
            var record = uploadTask.Record;

            if (record == null) { return; }

            UploadTaskSuccess?.Invoke(uploadTask, new UploadTaskSuccessEventArgs(record));
        }

        /// <summary>
        /// Triggers an UploadTaskFailure event for all listeners to indicate that an upload task has failed.
        /// </summary>
        /// <param name="uploadTask">The upload task that failed.</param>
        /// <param name="failureReason">The failure reason of the upload sync.</param>
        private static void OnUploadTaskFailure(RecordUploadTaskBase<FrooxEngineRecord> uploadTask, string failureReason)
        {
            var record = uploadTask.Record;

            if (record == null || string.IsNullOrEmpty(failureReason)) { return; }

            UploadTaskFailure?.Invoke(uploadTask, new UploadTaskFailureEventArgs(record, failureReason));
        }

        /// <summary>
        /// Triggers an UploadTaskProgress event for all listeners to indicate that an upload task has made some progress.
        /// </summary>
        /// <param name="uploadTask">The upload task that has made sync progress.</param>
        /// <param name="currentState">The current stage and progress the task is at.</param>
        private static void OnUploadTaskProgress(RecordUploadTaskBase<FrooxEngineRecord> uploadTask, UploadProgressState currentState)
        {
            var record = uploadTask.Record;
            var stage = currentState.Stage;
            var progress = currentState.Progress;

            if (record == null || (string.IsNullOrEmpty(stage) && progress < 0.01f)) { return; }

            UploadTaskProgress?.Invoke(uploadTask, new UploadTaskProgressEventArgs(record, currentState));
        }
    }
}
