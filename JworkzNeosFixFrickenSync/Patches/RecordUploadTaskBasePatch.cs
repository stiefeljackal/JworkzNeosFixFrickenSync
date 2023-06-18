using System;
using System.Threading.Tasks;
using HarmonyLib;
using System.Threading;
using System.Reflection;
using CloudX.Shared;
using MonoMod.Utils;
using NeosModLoader;

namespace JworkzNeosMod.Patches
{
    [HarmonyPatch(typeof(RecordUploadTaskBase<FrooxEngine.Record>))]
    internal static class RecordUploadTaskBasePatch
    {
        private static readonly MethodInfo _runUploadInternalMethod = AccessTools.Method(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "RunUploadInternal");
        private static readonly FieldInfo _completionSourceField = AccessTools.Field(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "_completionSource");
        private static readonly MethodInfo _failMethod = AccessTools.Method(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "Fail");

        internal static byte MaxUploadRetries { get; set; }
        internal static TimeSpan RetryDelay { get; set; }

        [HarmonyPrefix]
        [HarmonyPatch("RunUpload")]
        static bool RunUploadPrefix(RecordUploadTaskBase<FrooxEngine.Record> __instance, out Task __result, CancellationToken cancellationToken)
        {
            var uploadTask = (Task)_runUploadInternalMethod.Invoke(__instance, new object[] { cancellationToken });
            var completionSource = (TaskCompletionSource<bool>)_completionSourceField.GetValue(__instance);
            var failMethod = _failMethod.CreateDelegate<Action<string>>(__instance);

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
                        }

                        ++retryCount;
                        await Task.Delay(delay, CancellationToken.None);
                    }

                    // we may not have signalled completion at this point, so if we haven't, notify the system of failure
                    if (!completionSource.Task.IsCompleted)
                    {
                        failMethod
                        (
                            cancellationToken.IsCancellationRequested
                                ? "Record upload task cancelled"
                                : "Exception during sync."
                        );
                    }
                },
                CancellationToken.None
            );

            return false;
        }
    }
}
