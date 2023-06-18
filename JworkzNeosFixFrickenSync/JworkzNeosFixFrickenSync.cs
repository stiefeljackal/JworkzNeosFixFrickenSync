using System;
using NeosModLoader;
using HarmonyLib;
using FrooxEngine;
using JworkzNeosMod.Patches;

namespace JworkzNeosMod
{
    public class JworkzNeosFixFrickenSync : NeosMod
    {
        public override string Name => nameof(JworkzNeosFixFrickenSync);
        public override string Author => "Stiefel Jackal";
        public override string Version => "0.1.2";
        public override string Link => "https://github.com/stiefeljackal/NeosFixFrickenSync";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_ENABLE =
            new ModConfigurationKey<bool>("enabled", $"Enables the {nameof(JworkzNeosFixFrickenSync)} mod", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<byte> KEY_RETRY_COUNT =
            new ModConfigurationKey<byte>("retryCount", "The number of times to retry failed sync actions.", () => 3);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<TimeSpan> KEY_RETRY_DELAY =
            new ModConfigurationKey<TimeSpan>("retryDelay", "The delay between attempts to retry failed sync actions.", () => TimeSpan.Zero);

        private static ModConfiguration Config;

        private Harmony _harmony;

        public bool IsEnabled { get; private set; }


        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(Version)
                .AutoSave(false);
        }

        public override void OnEngineInit()
        {
            _harmony = new Harmony($"jworkz.sjackal.{Name}");
            Config = GetConfiguration();
            Config.OnThisConfigurationChanged += OnConfigurationChanged;
            Engine.Current.OnReady += OnCurrentNeosEngineReady;

            RecordUploadTaskBasePatch.MaxUploadRetries = Config.GetValue(KEY_RETRY_COUNT);
            RecordUploadTaskBasePatch.RetryDelay = Config.GetValue(KEY_RETRY_DELAY);

            _harmony.PatchAll();
        }

        private void RefreshMod()
        {
            RecordUploadTaskBasePatch.MaxUploadRetries = Config.GetValue(KEY_RETRY_COUNT);
            RecordUploadTaskBasePatch.RetryDelay = Config.GetValue(KEY_RETRY_DELAY);

            var isEnabled = Config.GetValue(KEY_ENABLE);
            ToggleHarmonyPatchState(isEnabled);
        }

        private void ToggleHarmonyPatchState(bool isEnabled)
        {
            if (isEnabled == IsEnabled) { return; }

            IsEnabled = isEnabled;

            if (!IsEnabled)
            {
                TurnOffMod();
            }
            else
            {
                TurnOnMod();
            }
        }

        private void TurnOnMod()
        {
            _harmony.PatchAll();
        }

        private void TurnOffMod()
        {
            _harmony.UnpatchAll(_harmony.Id);
        }

        private void OnConfigurationChanged(ConfigurationChangedEvent @event) => RefreshMod();


        private void OnCurrentNeosEngineReady() => RefreshMod();
    }
}
