using System;
using System.Collections.Generic;
using System.Linq;
using ICities;
using JetBrains.Annotations;
using SingleTrackAI.AI;
using SingleTrackAI.UI;

namespace SingleTrackAI
{
    [PublicAPI]
    public sealed class Loading : LoadingExtensionBase
    {
        private string[] _conflictingMods;
        private bool ConflictingModsEnabled
        {
            get
            {
                if (_conflictingMods == null)
                    _conflictingMods = Mod.DetermineConflictingMods();

                return _conflictingMods.Length != 0;
            }
        }

        /// <summary>
        /// Called by the game when the mod is initialized at the start of the loading process.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            // Don't do anything if not in game (e.g. if we're going into an editor).
            if (loading.currentMode != AppMode.Game)
            {
                Logger.Debug("Not in-game; reverting Harmony patches.");
                Patcher.Revert();
                return;
            }

            if (!Patcher.Patched)
            {
                Logger.Error("Harmony patches not applied; aborting!");
                return;
            }

            if (ConflictingModsEnabled)
            {
                Logger.Error("Conflicting mods detected; reverting Harmony patches.");
                Patcher.Revert();
                return;
            }

            Logger.Debug("loaded.");
        }

        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (ConflictingModsEnabled)
            {
                var modConflictBox = MessageBoxBase.ShowModal<ListMessageBox>();

                modConflictBox.AddParas("Mod conflict detected!");
                modConflictBox.AddParas($"{Mod.Name} detected a conflict with at least one other mod.");
                modConflictBox.AddParas("This means that the mod is not able to operate and has shut down.");

                modConflictBox.AddParas("The conflicting mods are:");
                modConflictBox.AddList(_conflictingMods);

                modConflictBox.AddParas($"These mods must be disabled or unsubscribed before {Mod.Name} can operate.");
            }

            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.LoadGame:
                    if (ReservationManager.instance != null)
                        ReservationManager.instance.Initialize();

                    Logger.Debug("initialized.");

                    break;

                default:
                    Logger.Debug("Not in-game, AI will not be initialized.");
                    break;
            }
        }
    }
}
