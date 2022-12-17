using System;
using System.Collections.Generic;
using System.Linq;
using AlgernonCommons.Patching;
using ICities;
using JetBrains.Annotations;
using SingleTrackAI.AI;
using SingleTrackAI.UI;

namespace SingleTrackAI
{
    [PublicAPI]
    public sealed class Loading : PatcherLoadingBase<OptionsPanel, Patcher>
    {
        /// <summary>
        /// Performs any actions upon successful creation of the mod.
        /// E.g. Can be used to patch any other mods.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game, editor, scenario, etc.)</param>
        protected override void CreatedActions(ILoading loading)
        {
            base.CreatedActions(loading);

            // Don't do anything if not in game (e.g. if we're going into an editor).
            if (loading.currentMode != AppMode.Game)
            {
                Logger.Debug("Not in-game; reverting Harmony patches.");
                CreatedAbortActions();
                return;
            }

            Logger.Debug("loaded.");
        }

        /// <summary>
        /// Performs actions upon successful level loading completion.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        protected override void LoadedActions(LoadMode mode)
        {
            base.LoadedActions(mode);

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
