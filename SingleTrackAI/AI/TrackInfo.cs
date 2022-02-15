using System;
using System.Collections.Generic;
using System.Linq;

namespace SingleTrackAI.AI
{
    public struct TrackInfo
    {
        private const VehicleInfo.VehicleType SupportedVehicleTypes = VehicleInfo.VehicleType.Train | VehicleInfo.VehicleType.Metro;

        // ReSharper disable once IdentifierTypo
        // ReSharper disable once InconsistentNaming
        // TODO: Find a way to dynamically detect these tracks to avoid hardcoding them.
        private static readonly HashSet<string> MOM_TWOWAY_TRACKS = new HashSet<string>
        {
            "Metro Track Ground Small Two-Way",
            "Metro Track Ground Small Two-Way NoBar",
            "Steel Metro Track Ground Small Two-Way",
            "Steel Metro Track Ground Small Two-Way NoBar",
        };

        public NetInfo NetInfo { get; }

        public VehicleInfo.VehicleType VehicleType { get; }

        public int Tracks { get; private set; }

        public int Tracks1Way { get; private set; }

        public int Tracks1WayForward { get; private set; }

        public int Tracks1WayBackward { get; private set; }

        public int Tracks2Way { get; private set; }

        public int Platforms { get; private set; }

        public bool IsSingleTwoWayTrack => IsGenericSingleTwoWayTrack || IsMetroOverhaulSingleTwoWayTrack;

        public bool IsGenericSingleTwoWayTrack => Tracks == 1 && Tracks2Way == 1;

        // MOM two-way single tracks are actually double tracks in different directions,
        // but rendered overlapping, so it appears there is only a single track.
        // This makes is quite difficult (impossible?) to detect using the network info lanes,
        // as its lanes are identical to the regular two-way double track.
        public bool IsMetroOverhaulSingleTwoWayTrack =>
            Tracks == 2 &&
            Tracks1Way == 2 &&
            Tracks1WayForward == 1 &&
            Tracks1WayBackward == 1 &&
            VehicleType == VehicleInfo.VehicleType.Metro &&
            MOM_TWOWAY_TRACKS.Contains(NetInfo.name);

        public bool IsStationTrack => Platforms != 0 && Tracks > 0;

        public bool IsSingleStationTrack => Platforms != 0 && Tracks == 1;

        public bool IsDoubleStationTrack => Platforms != 0 && Tracks == 2;

        public TrackInfo(NetInfo netInfo)
        {
            if (netInfo == null)
                throw new ArgumentNullException(nameof(netInfo));

            if (netInfo.m_vehicleTypes == VehicleInfo.VehicleType.None)
                throw new ArgumentOutOfRangeException(nameof(netInfo), "Network has no vehicle types; this is not supported.");

            if ((netInfo.m_vehicleTypes | SupportedVehicleTypes) != SupportedVehicleTypes)
                throw new ArgumentOutOfRangeException(nameof(netInfo), "Network has unsupported vehicle types.");

            if ((netInfo.m_vehicleTypes ^ SupportedVehicleTypes) == VehicleInfo.VehicleType.None)
                throw new ArgumentOutOfRangeException(nameof(netInfo), "Network has multiple vehicle types; this is not supported.");

            NetInfo = netInfo;

            VehicleType = NetInfo.m_vehicleTypes;

            Tracks = 0;

            Tracks1Way = 0;
            Tracks2Way = 0;

            Tracks1WayForward  = 0;
            Tracks1WayBackward = 0;

            Platforms = 0;

            // Use for instead of foreach since we don't want to allocate memory for the enumerator.
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < NetInfo.m_lanes.Length; i++)
            {
                NetInfo.Lane lane = NetInfo.m_lanes[i];

                ProcessLane(lane);
            }
        }

        private void ProcessLane(NetInfo.Lane lane)
        {
            switch (lane.m_laneType)
            {
                case NetInfo.LaneType.Pedestrian:
                    ProcessPedestrianLane(lane);
                    break;

                case NetInfo.LaneType.Vehicle:
                    ProcessVehicleLane(lane);
                    break;
            }
        }

        private void ProcessPedestrianLane(NetInfo.Lane lane)
        {
            bool stopTypeSupported = (lane.m_stopType & SupportedVehicleTypes) == lane.m_stopType;
            if (stopTypeSupported)
                Platforms++;
        }

        private void ProcessVehicleLane(NetInfo.Lane lane)
        {
            bool vehicleTypeSupported = (lane.m_vehicleType & SupportedVehicleTypes) == lane.m_vehicleType;
            if (!vehicleTypeSupported)
                return;

            Tracks++;

            switch (lane.m_direction)
            {
                case NetInfo.Direction.Forward:
                case NetInfo.Direction.AvoidForward:
                    Tracks1Way++;
                    Tracks1WayForward++;
                    break;

                case NetInfo.Direction.Backward:
                case NetInfo.Direction.AvoidBackward:
                    Tracks1Way++;
                    Tracks1WayBackward++;
                    break;

                case NetInfo.Direction.Both:
                case NetInfo.Direction.AvoidBoth:
                    Tracks2Way++;
                    break;
            }
        }
    }
}
