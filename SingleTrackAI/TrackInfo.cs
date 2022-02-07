using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SingleTrackAI
{
    public struct TrackInfo
    {
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

        public readonly NetInfo NetInfo;
        public readonly VehicleInfo.VehicleType VehicleType;
        public readonly int Tracks;
        public readonly int Tracks1Way;
        public readonly int Tracks1WayForward;
        public readonly int Tracks1WayBackward;
        public readonly int Tracks2Way;
        public readonly int Platforms;

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

        public bool IsStationTrack => Platforms != 0 && (Tracks == 1 || Tracks == 2); // TODO: Maybe check if Tracks > 0? This will include quad-station tracks.

        public bool IsSingleStationTrack => Platforms != 0 && Tracks == 1;

        public bool IsDoubleStationTrack => Platforms != 0 && Tracks == 2;

        public TrackInfo(
            NetInfo netInfo,
            VehicleInfo.VehicleType vehicleType,
            int tracks,
            int tracks1Way,
            int tracks1WayForward,
            int tracks1WayBackward,
            int tracks2Way,
            int platforms)
        {
            NetInfo = netInfo;
            VehicleType = vehicleType;
            Tracks = tracks;
            Tracks1Way = tracks1Way;
            Tracks1WayForward = tracks1WayForward;
            Tracks1WayBackward = tracks1WayBackward;
            Tracks2Way = tracks2Way;
            Platforms = platforms;
        }
    }
}
