using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SingleTrackAI
{
    public struct TrackInfo
    {
        public readonly NetInfo NetInfo;
        public readonly VehicleInfo.VehicleType VehicleType;
        public readonly int Tracks;
        public readonly int Tracks1Way;
        public readonly int Tracks1WayForward;
        public readonly int Tracks1WayBackward;
        public readonly int Tracks2Way;
        public readonly int Platforms;

        public bool IsSingleTwoWayTrack => Tracks == 1 && Tracks2Way == 1;

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
