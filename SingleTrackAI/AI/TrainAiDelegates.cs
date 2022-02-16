using ColossalFramework.Math;
using UnityEngine;

namespace SingleTrackAI.AI
{
    public static class TrainAiDelegates
    {
        public delegate void ArrivingToDestinationDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData
        );

        public delegate void CalculateSegmentPositionDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            PathUnit.Position position,
            uint laneId,
            byte offset,
            out Vector3 pos,
            out Vector3 dir,
            out float maxSpeed
        );

        public delegate float CalculateTargetSpeedDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            float speedLimit,
            float curve
        );

        public delegate void CheckNextLaneDelegate(
            TrainAI trainAi,
            ushort vehicleId,
            ref Vehicle vehicleData,
            ref float maxSpeed,
            PathUnit.Position position,
            uint laneId,
            byte offset,
            PathUnit.Position prevPos,
            uint prevLaneId,
            byte prevOffset,
            Bezier3 bezier
        );

        public delegate bool CheckOverlapDelegate(
            ushort vehicleId,
            ref Vehicle vehicleData,
            Segment3 segment,
            ushort ignoreVehicle
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
            ushort nodeId,
            ref NetNode nodeData,
            ref Vector4 targetPos,
            int index
        );
    }
}
