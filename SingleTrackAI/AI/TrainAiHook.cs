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

        public void ArrivingToDestination(ushort leaderId, ref Vehicle leaderData)
        {
            ArrivingToDestinationDelegate(_instance, leaderId, ref leaderData);
        }

        public void CalculateSegmentPosition(
            ushort vehicleId,
            ref Vehicle vehicleData,
            PathUnit.Position position,
            uint num4,
            byte b2,
            out Vector3 p5,
            out Vector3 vector2,
            out float b3)
        {
            CalculateSegmentPositionDelegate(
                _instance,
                vehicleId,
                ref vehicleData,
                position,
                num4,
                b2,
                out p5,
                out vector2,
                out b3
            );
        }

        public float CalculateTargetSpeed(ushort vehicleId, ref Vehicle vehicleData, float f, float num13)
        {
            return CalculateTargetSpeedDelegate(_instance, vehicleId, ref vehicleData, f, num13);
        }

        public void CheckNextLane(
            ushort vehicleId,
            ref Vehicle vehicleData,
            ref float num10,
            PathUnit.Position position2,
            uint laneId,
            byte b4,
            PathUnit.Position position,
            uint num4,
            byte positionMOffset,
            Bezier3 bezier)
        {
            CheckNextLaneDelegate(
                _instance,
                vehicleId,
                ref vehicleData,
                ref num10,
                position2,
                laneId,
                b4,
                position,
                num4,
                positionMOffset,
                bezier
            );
        }

        public bool CheckOverlap(ushort vehicleId, ref Vehicle vehicleData, Segment3 segment, ushort vehiclePreviouslyReservingSpace)
        {
            return CheckOverlapDelegate(vehicleId, ref vehicleData, segment, vehiclePreviouslyReservingSpace);
        }

        public void InvalidPath(ushort vehicleId, ref Vehicle vehicleData, ushort leaderId, ref Vehicle leaderData)
        {
            InvalidPathDelegate(_instance, vehicleId, ref vehicleData, leaderId, ref leaderData);
        }

        public void UpdateNodeTargetPos(ushort vehicleId, ref Vehicle vehicleData, ushort num12, ref NetNode netNode, ref Vector4 vector, int index)
        {
            UpdateNodeTargetPosDelegate(_instance, vehicleId, ref vehicleData, num12, ref netNode, ref vector, index);
        }
    }
}
