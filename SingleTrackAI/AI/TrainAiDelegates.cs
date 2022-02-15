using ColossalFramework.Math;
using UnityEngine;

namespace SingleTrackAI.AI
{
    public static class TrainAiDelegates
    {
        public delegate void ArrivingToDestinationDelegate(
            TrainAI trainAi,
            ushort leaderId,
            ref Vehicle leaderData
        );

        public delegate void CalculateSegmentPositionDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            PathUnit.Position position,
            uint num4,
            byte b2,
            out Vector3 p5,
            out Vector3 vector2,
            out float b3
        );

        public delegate float CalculateTargetSpeedDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            float f,
            float num13
        );

        public delegate void CheckNextLaneDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            ref float num10,
            PathUnit.Position position2,
            uint laneId,
            byte b4,
            PathUnit.Position position,
            uint num4,
            byte positionMOffset,
            Bezier3 bezier
        );

        public delegate bool CheckOverlapDelegate(
            ushort vehicleId,
            ref Vehicle vehicleData,
            Segment3 segment,
            ushort vehiclePreviouslyReservingSpace
        );

        public delegate void InvalidPathDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            ushort leaderId,
            ref Vehicle leaderData
        );

        public delegate void UpdateNodeTargetPosDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            ushort num12,
            ref NetNode netNode,
            ref Vector4 vector,
            int index
        );
    }
}
