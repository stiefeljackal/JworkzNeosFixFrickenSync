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

        [HarmonyPrefix]
        [HarmonyPatch("RunUpload")]
        static bool RunUploadPrefix(RecordUploadTaskBase<FrooxEngine.Record> __instance, ref Task __result, CancellationToken cancellationToken)
        {
            var uploadTask = (Task)_runUploadInternalMethod.Invoke(__instance, new object[] { cancellationToken });
            var completionSource = (TaskCompletionSource<bool>)_completionSourceField.GetValue(__instance);
            var failMethod = _failMethod.CreateDelegate<Action<string>>(__instance);

            __result = Task.Run(async () =>
            {
                try
                {
                    NeosMod.Msg("Starting sync task...");
                    await uploadTask.ConfigureAwait(false);
                    NeosMod.Msg("Sync Upload Task is done");
                    if (completionSource != null && !completionSource.Task.IsCompleted)
                    {
                        completionSource.SetResult(__instance.IsFinished);
                    }
                }
                catch (Exception ex)
                {
                    NeosMod.Error($"Exception during record upload task:\n {ex?.ToString()}");
                    failMethod($"Exception during sync.");
                }
            });
            return false;
        }
    }
}
