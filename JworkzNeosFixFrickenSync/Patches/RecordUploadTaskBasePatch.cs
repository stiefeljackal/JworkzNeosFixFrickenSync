using System;
using System.Threading.Tasks;
using HarmonyLib;
using System.Threading;
using System.Reflection;
using System.Text.RegularExpressions;
using CloudX.Shared;
using NeosModLoader;

namespace JworkzNeosMod.Patches
{
    [HarmonyPatch(typeof(RecordUploadTaskBase<FrooxEngine.Record>))]
    internal static class RecordUploadTaskBasePatch
    {
        private static readonly FieldInfo _completionSourceField = AccessTools.Field(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "_completionSource");

        private static readonly MethodInfo _runUploadInternalMethod = AccessTools.Method(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "RunUploadInternal");
        private static readonly MethodInfo _failedSetter = AccessTools.PropertySetter(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "Failed");
        private static readonly MethodInfo _failReasonSetter = AccessTools.PropertySetter(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "FailReason");
        private static readonly MethodInfo _isFinishedSetter = AccessTools.PropertySetter(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "IsFinished");

        private static readonly Regex _clientErrorMatcher = new Regex(@"state: 4\d\d", RegexOptions.Compiled);
        private static readonly Regex _terminalServerErrorMatcher = new Regex(@"state: (501|505|511)", RegexOptions.Compiled);

        internal static byte MaxUploadRetries { get; set; }
        internal static TimeSpan RetryDelay { get; set; }

        [HarmonyPrefix]
        [HarmonyPatch("RunUpload")]
        static bool RunUploadPrefix(RecordUploadTaskBase<FrooxEngine.Record> __instance, out Task __result, CancellationToken cancellationToken)
        {
            var uploadTask = (Task)_runUploadInternalMethod.Invoke(__instance, new object[] { cancellationToken });
            var completionSource = (TaskCompletionSource<bool>)_completionSourceField.GetValue(__instance);

            // We don't want to let the task terminate early, so the cancellation token is not passed on
            __result = Task.Run
            (
                async () =>
                {
                    var retryCount = 0;
                    var maxRetryCount = MaxUploadRetries <= 0 ? 1 : MaxUploadRetries;
                    var delay = RetryDelay;

                    while (retryCount < maxRetryCount && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await uploadTask.ConfigureAwait(false);
                            _ = completionSource.TrySetResult(__instance.IsFinished);

                            return;
                        }
                        catch (Exception ex)
                        {
                            NeosMod.Error($"Exception during record upload task (attempt {retryCount + 1} out of {maxRetryCount}): {ex}");
                            FailPrefix(__instance, $"{ex.Message}\n{ex.StackTrace}");
                        }

                            if (!__instance.ShouldRetry())
                            {
                                break;
                            }
                        }

                        ++retryCount;
                        await Task.Delay(delay, CancellationToken.None);
                    }

                    // we may not have signalled completion at this point, so if we haven't, notify the system of failure
                    if (!completionSource.Task.IsCompleted)
                    {
                        FailPrefix(__instance,
                        (
                            cancellationToken.IsCancellationRequested
                                ? "Record upload task cancelled"
                                : "Exception during sync."
                        ));
                    }
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
        static bool FailPrefix(RecordUploadTaskBase<FrooxEngine.Record> __instance, string error) 
        {
            var record = __instance.Record;
            NeosMod.Error($"Failed sync for {record.OwnerId}:{record.RecordId}. Local: {record.LocalVersion}, Global: {record.GlobalVersion}:\n{error}");

            _failedSetter.Invoke(__instance, new object[] { true });
            _failReasonSetter.Invoke(__instance, new object[] { error });
            _isFinishedSetter.Invoke(__instance, new object[] { true });

            var completionSource = (TaskCompletionSource<bool>)_completionSourceField.GetValue(__instance);
            _ = completionSource.TrySetResult(result: false);

            return false;
        }

        /// <summary>
        /// Returns a boolean to indicate that the upload task should retry based on the task's fail reason.
        /// </summary>
        /// <param name="uploadTask">The upload task instance</param>
        /// <returns>true if the task should retry due to the fail reason; otherwise, false.</returns>
        private static bool ShouldRetry(this RecordUploadTaskBase<FrooxEngine.Record> uploadTask)
        {
            switch (uploadTask.FailReason.Trim().ToLowerInvariant())
            {
                case var r1 when r1.Contains("conflict"):
                case var r2 when r2.Contains("preprocessing failed"):
                    return false;
                case var r3 when r3.Contains("state: 429"):
                    return true; // catch this before the next line
                case var r4 when _clientErrorMatcher.IsMatch(r4):
                case var r5 when _terminalServerErrorMatcher.IsMatch(r5):
                    return false;
                default:
                    return true;
            }
        }
    }
}
