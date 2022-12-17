using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AlgernonCommons.XML;
using ColossalFramework.IO;
using SingleTrackAI.AI;
using SingleTrackAI.Patches;

namespace SingleTrackAI
{
    [XmlRoot("SingleTrackAI")]
    public sealed class Settings : SettingsXMLBase
    {
        private static readonly string OldSettingsFilePath = Path.Combine(DataLocation.modsPath, $"SingleTrainTrackAI{Path.DirectorySeparatorChar}SingleTrainTrackAISettings.xml");
        private static readonly string NewSettingsFilePath = Path.Combine(DataLocation.localApplicationData, "SingleTrackAI.xml");

        /// <summary>
        /// Train waiting for a single track section to clear will be registered in a queue. First in the queue can go first.
        /// </summary>
        [XmlElement("AllowProrityQueue")]
        public bool AllowPriorityQueue
        {
            get => ReservationManager.AllowPriorityQueue;
            set => ReservationManager.AllowPriorityQueue = value;
        }

        /// <summary>
        /// Train with the same route as the train currently on the single track section will follow immediately. Except if a train with a different route is in the priority queue.
        /// </summary>
        [XmlElement("AllowFollowing")]
        public bool AllowFollowing
        {
            get => ReservationManager.AllowFollowing;
            set => ReservationManager.AllowFollowing = value;
        }

        /// <summary>
        /// Trains will proceed as long as their route does not overlap the one of another train. Useful in case of branching single tracks, this can however cause jams.
        /// </summary>
        [XmlElement("GoAsFarAsPossible")]
        public bool AllowGoAsFarAsPossible
        {
            get => ReservationManager.AllowGoAsFarAsPossible;
            set => ReservationManager.AllowGoAsFarAsPossible = value;
        }

        /// <summary>
        /// Signals will be automatically spawned at junctions from two tracks to single track.
        /// </summary>
        [XmlElement("AutoSpawnSignals")]
        public bool AllowSpawnSignals
        {
            get => ReservationManager.AllowSpawnSignals;
            set => ReservationManager.AllowSpawnSignals = value;
        }

        /// <summary>
        /// Set this to true in case trains briefly stop when leaving a single track section with a train waiting in the other direction.
        /// However, this can cause train to overlap with another train.
        /// </summary>
        [XmlElement("NoCheckOverlapOnLastSegment")]
        public bool NoCheckOverlapOnLastSegment
        {
            get => Transpiler_TrainAI_UpdatePathTargetPositions.NoCheckOverlapOnLastSegment;
            set => Transpiler_TrainAI_UpdatePathTargetPositions.NoCheckOverlapOnLastSegment = value;
        }

        /// <summary>
        /// Fix case where train reversing at a single track station gets stuck.
        /// </summary>
        [XmlElement("FixReverseTrainSingleTrackStation")]
        public bool FixReverseTrainSingleTrackStation
        {
            get => Transpiler_TrainAI_UpdatePathTargetPositions.FixReverseTrainSingleTrackStation;
            set => Transpiler_TrainAI_UpdatePathTargetPositions.FixReverseTrainSingleTrackStation = value;
        }

        /// <summary>
        /// If station where the train stops has single tracks before and after it, the single track section placed after the station also gets reserved.
        /// This is just some additional logic to avoid creating a separate reservation after the station, in case the train continues.
        /// </summary>
        [XmlElement("ExtendReservationAfterStopStation")]
        public bool ExtendReservationAfterStopStation
        {
            get => ReservationManager.ExtendReservationAfterStopStation;
            set => ReservationManager.ExtendReservationAfterStopStation = value;
        }

        /// <summary>
        /// Reserve double track station placed after a single track section. Useful in case train reverses and use the single track section again.
        /// </summary>
        ///
        /// <remarks>
        /// TODO: Not implemented :)
        /// </remarks>
        [XmlElement("IncludeDoubleTrackStationAfterSingleTrackSection")]
        public bool IncludeDoubleTrackStationAfterSingleTrackSection { get; set; } = true;

        /// <summary>
        /// In case the trains you use have slow speeds, wait 2x more time before canceling reservations.
        /// </summary>
        [XmlElement("SlowSpeedTrains")]
        public bool SlowSpeedTrains
        {
            get => ReservationManager.SlowSpeedTrains;
            set => ReservationManager.SlowSpeedTrains = value;
        }

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load()
        {
            try
            {
                var file = new FileInfo(OldSettingsFilePath);
                if (file.Exists)
                {
                    var directory = file.Directory;
                    if (directory != null && directory.Exists && directory.GetFiles().Length == 1)
                    {
                        file.Delete();
                        directory.Delete();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Error deleting old settings file: {OldSettingsFilePath}");
            }

            XMLFileUtils.Load<Settings>(NewSettingsFilePath);
        }

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<Settings>(NewSettingsFilePath);
    }
}
