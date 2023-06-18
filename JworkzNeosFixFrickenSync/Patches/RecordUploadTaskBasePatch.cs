using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using FrooxEngine;
using System.Threading;
using System.Reflection;
using CloudX.Shared;
using MonoMod.Utils;
using BaseX;
using NeosModLoader;

namespace JworkzNeosMod.Patches
{
    [HarmonyPatch(typeof(RecordUploadTaskBase<FrooxEngine.Record>))]
    internal static class RecordUploadTaskBasePatch
    {
        private static MethodInfo _runUploadInternalMethod;

        private static FieldInfo _completionSourceFieldInfo;

        private static MethodInfo _isFinishedGetterMethodInfo;

        private static MethodInfo _failMethodInfo;

        public static MethodInfo RunUploadInternalMethod
        {
            get
            {
                if (_runUploadInternalMethod == null)
                {
                    _runUploadInternalMethod = AccessTools.Method(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "RunUploadInternal", new[] { typeof(CancellationToken) });
                }

                return _runUploadInternalMethod;
            }
        }

        public static FieldInfo GetCompletionSourceInfo
        {
            get
            {
                if (_completionSourceFieldInfo == null)
                {
                    _completionSourceFieldInfo = AccessTools.Field(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "_completionSource");
                }

                return _completionSourceFieldInfo;
            }
        }

        public static MethodInfo IsFinishedGetterInfo
        {
            get
            {
                if (_isFinishedGetterMethodInfo == null)
                {
                    _isFinishedGetterMethodInfo = AccessTools.PropertyGetter(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "IsFinished");
                }
                return _isFinishedGetterMethodInfo;
            }
        }

        public static MethodInfo FailInfo
        {
            get
            {
                if (_failMethodInfo == null)
                {
                    _failMethodInfo = AccessTools.Method(typeof(RecordUploadTaskBase<FrooxEngine.Record>), "Fail");
                }
                return _failMethodInfo;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("RunUpload")]
        static bool RunUploadPrefix(ref RecordUploadTaskBase<FrooxEngine.Record> __instance, ref Task __result, CancellationToken cancellationToken)
        {
            var uploadTask = RunUploadInternalMethod.Invoke(__instance, new object[] { cancellationToken }) as Task;
            var completionSource = GetCompletionSourceInfo.GetValue(__instance) as TaskCompletionSource<bool>;
            var isFinishedGetter = IsFinishedGetterInfo.CreateDelegate(typeof(Func<bool>), __instance);
            var failMethod = FailInfo.CreateDelegate<Action<string>>(__instance);

            __result = Task.Run(async () =>
            {
                try
                {
                    NeosMod.Msg("Starting sync task...");
                    await uploadTask.ConfigureAwait(false);
                    NeosMod.Msg("Sync Upload Task is done");
                    if (completionSource != null && !completionSource.Task.IsCompleted)
                    {
                        var isFinished = (bool)isFinishedGetter.DynamicInvoke();
                        completionSource.SetResult(isFinished);
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
