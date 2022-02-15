using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ColossalFramework.IO;

namespace SingleTrackAI
{
    internal static class Settings
    {
        private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings();

        private static readonly string SettingsDirectory = Path.Combine(DataLocation.modsPath, "SingleTrainTrackAI");
        private static readonly string SettingsFileName  = Path.Combine(SettingsDirectory, "SingleTrainTrackAISettings.xml");

        public static bool AllowPriorityQueue = true;
        public static bool AllowFollowing = true;
        public static bool AllowGoAsFarAsPossible = false;
        public static bool AllowSpawnSignals = false;
        public static bool NoCheckOverlapOnLastSegment = false;

        // Not in XML.
        public static bool FixReverseTrainSingleTrackStation = true;
        public static bool ExtendReservationAfterStopStation = true;
        public static bool IncludeDoubleTrackStationAfterSingleTrackSection = true;
        public static bool SlowSpeedTrains = false;

        public static void Initialize()
        {
            XmlSettings.Encoding = Encoding.Unicode;
            XmlSettings.Indent = true;
            XmlSettings.IndentChars = "\t";

            if (!ReadSettings())
                WriteSettings();
        }

        private static bool ReadSettings()
        {
            CheckDirectories();

            if (!File.Exists(SettingsFileName))
                return false;

            bool fileCorrectVersion = true;

            XmlReader xr = XmlReader.Create(SettingsFileName);
            try
            {
                while (xr.Read())
                {
                    //detect useful tags
                    if (xr.NodeType == XmlNodeType.Element && xr.HasAttributes)
                    {
                        bool value;
                        string val = xr.GetAttribute("Value");

                        switch (xr.Name)
                        {
                            case "SingleTrainTrackAI":
                                val = xr.GetAttribute("version");
                                fileCorrectVersion = val.Equals(Mod.Version);
                                break;
                            case "AllowProrityQueue":
                                if (Boolean.TryParse(val, out value))
                                {
                                    AllowPriorityQueue = value;
                                }

                                break;
                            case "AllowFollowing":
                                if (Boolean.TryParse(val, out value))
                                {
                                    AllowFollowing = value;
                                }

                                break;
                            case "GoAsFarAsPossible":
                                if (Boolean.TryParse(val, out value))
                                {
                                    AllowGoAsFarAsPossible = value;
                                }

                                break;
                            case "AutoSpawnSignals":
                                if (Boolean.TryParse(val, out value))
                                {
                                    AllowSpawnSignals = value;
                                }

                                break;
                            case "NoCheckOverlapOnLastSegment":
                                if (Boolean.TryParse(val, out value))
                                {
                                    NoCheckOverlapOnLastSegment = value;
                                }

                                break;
                            case "FixReverseTrainSingleTrackStation":
                                if (Boolean.TryParse(val, out value))
                                {
                                    FixReverseTrainSingleTrackStation = value;
                                }

                                break;
                            case "IncludeDoubleTrackStationAfterSingleTrackSection":
                                if (Boolean.TryParse(val, out value))
                                {
                                    IncludeDoubleTrackStationAfterSingleTrackSection = value;
                                }

                                break;
                            case "ExtendReservationAfterStopStation":
                                if (Boolean.TryParse(val, out value))
                                {
                                    ExtendReservationAfterStopStation = value;
                                }

                                break;
                            case "SlowSpeedTrains":
                                if (Boolean.TryParse(val, out value))
                                {
                                    SlowSpeedTrains = value;
                                }

                                break;

                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);

                return false;
            }
            finally
            {
                xr.Close();
            }

            return fileCorrectVersion;
        }

        private static void WriteSettings()
        {
            CheckDirectories();

            var xw = XmlWriter.Create(SettingsFileName, XmlSettings);

            //header
            xw.WriteStartDocument();
            xw.WriteComment("Automatically generated XML file for cities : skyline's SingleTrainTrackAI  Created by CoarxFlow.");
            xw.WriteStartElement("SingleTrainTrackAI");
            xw.WriteAttributeString("version", Mod.Version);

            xw.WriteStartElement("Settings");

            xw.WriteComment("Train waiting for a single track section to clear will be registered in a queue. First in the queue can go first.");
            xw.WriteStartElement("AllowProrityQueue");
            xw.WriteAttributeString("Value", AllowPriorityQueue.ToString());
            xw.WriteEndElement();

            xw.WriteComment("Train with the same route as the train currently on the single track section will follow immediately. Except if a train with a different route is in the priority queue.");
            xw.WriteStartElement("AllowFollowing");
            xw.WriteAttributeString("Value", AllowFollowing.ToString());
            xw.WriteEndElement();

            xw.WriteComment("Trains will proceed as long as their route does not overlap the one of another train. Useful in case of branching single tracks, this can however cause jams.");
            xw.WriteStartElement("GoAsFarAsPossible");
            xw.WriteAttributeString("Value", AllowGoAsFarAsPossible.ToString());
            xw.WriteEndElement();

            xw.WriteComment("Signals will be automatically spawned at junctions from two tracks to single track.");
            xw.WriteStartElement("AutoSpawnSignals");
            xw.WriteAttributeString("Value", AllowSpawnSignals.ToString());
            xw.WriteEndElement();

            xw.WriteComment("Set this to true in case trains briefly stop when leaving a single track section with a train waiting in the other direction.");
            xw.WriteComment("However, this can cause train to overlap with another train.");
            xw.WriteStartElement("NoCheckOverlapOnLastSegment");
            xw.WriteAttributeString("Value", NoCheckOverlapOnLastSegment.ToString());
            xw.WriteEndElement();

            xw.WriteComment("Fix case where train reversing at a single track station gets stuck.");
            xw.WriteStartElement("FixReverseTrainSingleTrackStation");
            xw.WriteAttributeString("Value", FixReverseTrainSingleTrackStation.ToString());
            xw.WriteEndElement();

            xw.WriteComment("Reserve double track station placed after a single track section. Useful in case train reverses and use the single track section again.");
            xw.WriteStartElement("IncludeDoubleTrackStationAfterSingleTrackSection");
            xw.WriteAttributeString("Value", IncludeDoubleTrackStationAfterSingleTrackSection.ToString());
            xw.WriteEndElement();

            xw.WriteComment("If station where the train stops has single tracks before and after it, the single track section placed after the station also gets reserved.");
            xw.WriteComment("This is just some additional logic to avoid creating a separate reservation after the station, in case the train continues.");
            xw.WriteStartElement("ExtendReservationAfterStopStation");
            xw.WriteAttributeString("Value", ExtendReservationAfterStopStation.ToString());
            xw.WriteEndElement();

            xw.WriteComment("In case the trains you use have slow speeds, wait 2x more time before canceling reservations.");
            xw.WriteStartElement("SlowSpeedTrains");
            xw.WriteAttributeString("Value", SlowSpeedTrains.ToString());
            xw.WriteEndElement();



            xw.WriteEndElement();

            //close file
            xw.WriteEndElement();
            xw.WriteEndDocument();
            xw.Close();
        }

        private static void CheckDirectories()
        {
            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);
        }
    }
}
