using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework.Math;
using JetBrains.Annotations;
using SingleTrackAI.Reflection;
using UnityEngine;

namespace SingleTrackAI.AI
{
    // ReSharper disable once InconsistentNaming
    internal sealed class TrainAiHook
    {
        // TODO: Which method does this call? The TrainAI method, inherited from VehicleAI, or the PassengerTrainAI override?
        private static readonly TrainAiDelegates.ArrivingToDestinationDelegate ArrivingToDestinationDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.ArrivingToDestinationDelegate>(typeof(TrainAI), nameof(ArrivingToDestination), instance: true);

        private static readonly TrainAiDelegates.CalculateSegmentPositionDelegate CalculateSegmentPositionDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.CalculateSegmentPositionDelegate>(typeof(TrainAI), nameof(CalculateSegmentPosition), instance: true);

        private static readonly TrainAiDelegates.CalculateTargetSpeedDelegate CalculateTargetSpeedDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.CalculateTargetSpeedDelegate>(typeof(TrainAI), nameof(CalculateTargetSpeed), instance: true);

        private static readonly TrainAiDelegates.CheckNextLaneDelegate CheckNextLaneDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.CheckNextLaneDelegate>(typeof(TrainAI), nameof(CheckNextLane), instance: true);

        private static readonly TrainAiDelegates.CheckOverlapDelegate CheckOverlapDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.CheckOverlapDelegate>(typeof(TrainAI), nameof(CheckOverlap), instance: false);

        private static readonly TrainAiDelegates.InvalidPathDelegate InvalidPathDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.InvalidPathDelegate>(typeof(TrainAI), nameof(InvalidPath), instance: true);

        private static readonly TrainAiDelegates.UpdateNodeTargetPosDelegate UpdateNodeTargetPosDelegate =
            DelegateCreator.CreateDelegate<TrainAiDelegates.UpdateNodeTargetPosDelegate>(typeof(TrainAI), nameof(UpdateNodeTargetPos), instance: true);

        private readonly TrainAI _instance;

        public TrainAiHook([NotNull] TrainAI instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public void ArrivingToDestination(ushort vehicleId, ref Vehicle vehicleData)
        {
            ArrivingToDestinationDelegate(_instance, vehicleId, ref vehicleData);
        }

        public void CalculateSegmentPosition(
            ushort vehicleId,
            ref Vehicle vehicleData,
            PathUnit.Position position,
            uint laneId,
            byte offset,
            out Vector3 pos,
            out Vector3 dir,
            out float maxSpeed)
        {
            CalculateSegmentPositionDelegate(
                _instance,
                vehicleId,
                ref vehicleData,
                position,
                laneId,
                offset,
                out pos,
                out dir,
                out maxSpeed
            );
        }

        public float CalculateTargetSpeed(ushort vehicleId, ref Vehicle vehicleData, float speedLimit, float curve)
        {
            return CalculateTargetSpeedDelegate(_instance, vehicleId, ref vehicleData, speedLimit, curve);
        }

        public void CheckNextLane(
            ushort vehicleId,
            ref Vehicle vehicleData,
            ref float maxSpeed,
            PathUnit.Position position,
            uint laneId,
            byte offset,
            PathUnit.Position prevPos,
            uint prevLaneId,
            byte prevOffset,
            Bezier3 bezier)
        {
            CheckNextLaneDelegate(
                _instance,
                vehicleId,
                ref vehicleData,
                ref maxSpeed,
                position,
                laneId,
                offset,
                prevPos,
                prevLaneId,
                prevOffset,
                bezier
            );
        }

        public bool CheckOverlap(ushort vehicleId, ref Vehicle vehicleData, Segment3 segment, ushort ignoreVehicle)
        {
            return CheckOverlapDelegate(vehicleId, ref vehicleData, segment, ignoreVehicle);
        }

        public void InvalidPath(ushort vehicleId, ref Vehicle vehicleData, ushort leaderId, ref Vehicle leaderData)
        {
            InvalidPathDelegate(_instance, vehicleId, ref vehicleData, leaderId, ref leaderData);
        }

        public void UpdateNodeTargetPos(ushort vehicleId, ref Vehicle vehicleData, ushort nodeId, ref NetNode nodeData, ref Vector4 targetPos, int index)
        {
            UpdateNodeTargetPosDelegate(_instance, vehicleId, ref vehicleData, nodeId, ref nodeData, ref targetPos, index);
        }
    }
}
