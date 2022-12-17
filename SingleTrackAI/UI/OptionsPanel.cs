using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AlgernonCommons.UI;
using ColossalFramework.UI;
using JetBrains.Annotations;
using SingleTrackAI.AI;
using SingleTrackAI.Patches;

namespace SingleTrackAI.UI
{
    /// <summary>
    /// The mod's options panel.
    /// </summary>
    [UsedImplicitly]
    public sealed class OptionsPanel : UIPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedVariable")]
        public OptionsPanel()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            var helper = new UIHelper(this);

            // Main options
            var mainGroup = helper.AddGroup("AI options");

            var priorityQueueCheckBox = (UICheckBox)mainGroup.AddCheckbox(
                "Allow trains on the single track in the order they arrived (at either end).",
                ReservationManager.AllowPriorityQueue,
                isChecked => ReservationManager.AllowPriorityQueue = isChecked);

            var followingCheckBox = (UICheckBox)mainGroup.AddCheckbox(
                "Allow trains to follow each other on the single track.",
                ReservationManager.AllowFollowing,
                isChecked => ReservationManager.AllowFollowing = isChecked);

            var goAsFarAsPossibleCheckBox = (UICheckBox)mainGroup.AddCheckbox(
                "Reserve single track as far as possible (beyond branches). May cause jams.",
                ReservationManager.AllowGoAsFarAsPossible,
                isChecked => ReservationManager.AllowGoAsFarAsPossible = isChecked);

            var autoSpawnSignalsCheckBox = (UICheckBox)mainGroup.AddCheckbox(
                "Automatically spawn signals at single track junctions.",
                ReservationManager.AllowSpawnSignals,
                isChecked => ReservationManager.AllowSpawnSignals = isChecked);

            // Advanced options
            var advancedGroup = helper.AddGroup("Advanced");

            var noCheckOverlapCheckBox = (UICheckBox)advancedGroup.AddCheckbox(
                "Don't check for trains overlapping on the last single track segment.",
                Transpiler_TrainAI_UpdatePathTargetPositions.NoCheckOverlapOnLastSegment,
                isChecked => Transpiler_TrainAI_UpdatePathTargetPositions.NoCheckOverlapOnLastSegment = isChecked);
            noCheckOverlapCheckBox.tooltip = "Set this to true in case trains briefly stop when leaving a single " +
                                             "track section with a train waiting in the other direction. However, " +
                                             "this can cause train to overlap with another train.";
            noCheckOverlapCheckBox.tooltipBox = UIToolTips.WordWrapToolTip;

            var fixReversingCheckBox = (UICheckBox)advancedGroup.AddCheckbox(
                "Fix trains getting stuck when reversing out of a single track station.",
                Transpiler_TrainAI_UpdatePathTargetPositions.FixReverseTrainSingleTrackStation,
                isChecked => Transpiler_TrainAI_UpdatePathTargetPositions.FixReverseTrainSingleTrackStation = isChecked);

            var extendReservationCheckBox = (UICheckBox)advancedGroup.AddCheckbox(
                "Extend single track reservation beyond station.",
                ReservationManager.ExtendReservationAfterStopStation,
                isChecked => ReservationManager.ExtendReservationAfterStopStation = isChecked);
            extendReservationCheckBox.tooltip = "If station where the train stops has single tracks before and after it, " +
                                                "the single track section placed after the station also gets reserved.";
            extendReservationCheckBox.tooltipBox = UIToolTips.WordWrapToolTip;

            var slowSpeedTrainsCheckBox = (UICheckBox)advancedGroup.AddCheckbox(
                "Increase duration of single track reservations.",
                ReservationManager.SlowSpeedTrains,
                isChecked => ReservationManager.SlowSpeedTrains = isChecked);
            slowSpeedTrainsCheckBox.tooltip = "May be necessary on long single track sections if trains have low maximum speeds, " +
                                              "the track has a low speed limit, has many curves, hills or junctions.";
            slowSpeedTrainsCheckBox.tooltipBox = UIToolTips.WordWrapToolTip;
        }
    }
}
