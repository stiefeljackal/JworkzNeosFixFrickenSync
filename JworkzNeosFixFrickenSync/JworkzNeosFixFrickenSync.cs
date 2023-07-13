using System;
using NeosModLoader;
using HarmonyLib;
using FrooxEngine;
using JworkzNeosMod.Patches;
using JworkzNeosMod.Services;

namespace JworkzNeosMod
{
    public class JworkzNeosFixFrickenSync : NeosMod
    {
        public override string Name => nameof(JworkzNeosFixFrickenSync);
        public override string Author => "Stiefel Jackal";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/stiefeljackal/NeosFixFrickenSync";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_ENABLE =
            new ModConfigurationKey<bool>("enabled", $"Enables the {nameof(JworkzNeosFixFrickenSync)} mod.", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<byte> KEY_RETRY_COUNT =
            new ModConfigurationKey<byte>("retryCount", "The number of times to retry failed sync actions.", () => 3);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<TimeSpan> KEY_RETRY_DELAY =
            new ModConfigurationKey<TimeSpan>("retryDelay", "The delay between attempts to retry failed sync actions.", () => TimeSpan.Zero);

        private static ModConfiguration Config;

        public static ModConfiguration.ConfigurationChangedEventHandler OnBaseModConfigurationChanged;

        private Harmony _harmony;

        private bool _isPrevEnabled;

        public static bool IsEnabled => Config?.GetValue(KEY_ENABLE) ?? false;

        public static byte RetryCount => Config?.GetValue(KEY_RETRY_COUNT) ?? 0;

        public static TimeSpan RetryDelay => Config?.GetValue(KEY_RETRY_DELAY) ?? TimeSpan.Zero;


        /// <summary>
        /// Defines the metadata for the mod and other mod configurations.
        /// </summary>
        /// <param name="builder">The mod configuration definition builder responsible for building and adding details about this mod.</param>
        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(Version)
                .AutoSave(false);
        }

        /// <summary>
        /// Called when the engine initializes.
        /// </summary>
        public override void OnEngineInit()
        {
            _harmony = new Harmony($"jworkz.sjackal.{Name}");
            Config = GetConfiguration();
            Config.OnThisConfigurationChanged += OnConfigurationChanged;
            Engine.Current.OnReady += OnCurrentNeosEngineReady;

            _harmony.PatchAll();
        }

        /// <summary>
        /// Refreshes the current state of the mod.
        /// </summary>
        private void RefreshMod()
        {
            var isEnabled = Config.GetValue(KEY_ENABLE);
            ToggleHarmonyPatchState(isEnabled);
        }

        /// <summary>
        /// Toggls the Enabled and Disabled state of the mod depending on the passed state.
        /// </summary>
        /// <param name="isEnabled">true if the mod should be enabled; otherwise, false if the mod should be disabled.</param>
        private void ToggleHarmonyPatchState(bool isEnabled)
        {
            if (isEnabled == _isPrevEnabled) { return; }

            _isPrevEnabled = isEnabled;


            if (!IsEnabled)
            {
                TurnOffMod();
            }
            else
            {
                TurnOnMod();
            }
        }

        /// <summary>
        /// Enables the mod.
        /// </summary>
        private void TurnOnMod()
        {
            _harmony.PatchAll();
            RecordUploadTaskBasePatch.UploadTaskProgress += SyncLogger.LogUploadUpdate;
        }

        /// <summary>
        /// Disables the mod.
        /// </summary>
        private void TurnOffMod()
        {
            RecordUploadTaskBasePatch.UploadTaskProgress -= SyncLogger.LogUploadUpdate;

            _harmony.UnpatchAll(_harmony.Id);
        }

        /// <summary>
        /// Called when the configuration is changed.
        /// </summary>
        /// <param name="event">The event information that details the configuration change.</param>
        private void OnConfigurationChanged(ConfigurationChangedEvent @event) {
            RefreshMod();
            OnBaseModConfigurationChanged(@event);
        }

        /// <summary>
        /// Called when the Neos Engine is ready.
        /// </summary>
        private void OnCurrentNeosEngineReady() => RefreshMod();
    }
}
