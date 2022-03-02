using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using SingleTrackAI.AI;
using UnityEngine;

namespace SingleTrackAI.Patches
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    [HarmonyPatch(typeof(TrainAI), "UpdatePathTargetPositions")]
    public static class Patch_TrainAI_UpdatePathTargetPositions
    {
        public static bool Prefix(
            TrainAI __instance,
            ushort vehicleID,
            ref Vehicle vehicleData,
            Vector3 refPos1,
            Vector3 refPos2,
            ushort leaderID,
            ref Vehicle leaderData,
            ref int index,
            int max1,
            int max2,
            float minSqrDistanceA,
            float minSqrDistanceB)
        {
            if ((vehicleData.Info.m_vehicleType | TrackInfo.SupportedVehicleTypes) != TrackInfo.SupportedVehicleTypes)
                return true; // call original

            var hookedTrainAi = new TrainAiHook(__instance);

            UpdatePathTargetPositions_Modified(
                hookedTrainAi,
                vehicleID,
                ref vehicleData,
                refPos1,
                refPos2,
                leaderID,
                ref leaderData,
                ref index,
                max1,
                max2,
                minSqrDistanceA,
                minSqrDistanceB
            );

            return false; // skip original
        }

        private static void UpdatePathTargetPositions_Modified(
            TrainAiHook trainAI,
            ushort vehicleID,
            ref Vehicle vehicleData,
            Vector3 refPos1,
            Vector3 refPos2,
            ushort leaderID,
            ref Vehicle leaderData,
            ref int index,
            int max1,
            int max2,
            float minSqrDistanceA,
            float minSqrDistanceB)
        {
            PathManager instance = Singleton<PathManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;

            Vector4 vector = vehicleData.m_targetPos0;
            vector.w = 1000f;
            float num = minSqrDistanceA;
            float num2 = 0f;
            uint num3 = vehicleData.m_path;
            byte b = vehicleData.m_pathPositionIndex;
            byte b2 = vehicleData.m_lastPathOffset;
            if (b == 255)
            {
                b = 0;
                if (index <= 0)
                {
                    vehicleData.m_pathPositionIndex = 0;
                }
                if (!Singleton<PathManager>.instance.m_pathUnits.m_buffer[(int) ((UIntPtr) num3)].CalculatePathPositionOffset(b >> 1, vector, out b2))
                {
                    trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                    return;
                }
            }

            if (!instance.m_pathUnits.m_buffer[(int) ((UIntPtr) num3)].GetPosition(b >> 1, out var position))
            {
                trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                return;
            }
            uint num4 = PathManager.GetLaneID(position);
            //try to notify for all vehicles composing the train. however, UpdatePathTargetPositions is not called when train is stopped
            //NotifySingleTrack2Ways(vehicleID, vehicleData, num4);
            Bezier3 bezier;
            while (true)
            {
                if ((b & 1) == 0) //is pathPositionIndex even?
                {
                    bool flag = true;
                    while (b2 != position.m_offset)
                    {
                        if (flag)
                        {
                            flag = false;
                        }
                        else
                        {
                            float num5 = Mathf.Max(Mathf.Sqrt(num) - Vector3.Distance(vector, refPos1), Mathf.Sqrt(num2) - Vector3.Distance(vector, refPos2));
                            int num6;
                            if (num5 < 0f)
                            {
                                num6 = 4;
                            }
                            else
                            {
                                num6 = 4 + Mathf.CeilToInt(num5 * 256f / (instance2.m_lanes.m_buffer[(int) (UIntPtr) num4].m_length + 1f));
                            }
                            if (b2 > position.m_offset)
                            {
                                b2 = (byte) Mathf.Max(b2 - num6, position.m_offset);
                            }
                            else if (b2 < position.m_offset)
                            {
                                b2 = (byte) Mathf.Min(b2 + num6, position.m_offset);
                            }
                        }

                        trainAI.CalculateSegmentPosition(vehicleID, ref vehicleData, position, num4, b2, out var a, out _, out float b3);
                        vector.Set(a.x, a.y, a.z, Mathf.Min(vector.w, b3));
                        float sqrMagnitude = (a - refPos1).sqrMagnitude;
                        float sqrMagnitude2 = (a - refPos2).sqrMagnitude;
                        if (sqrMagnitude >= num && sqrMagnitude2 >= num2)
                        {
                            if (index <= 0)
                            {
                                vehicleData.m_lastPathOffset = b2;
                            }
                            vehicleData.SetTargetPos(index++, vector);
                            if (index < max1)
                            {
                                num = minSqrDistanceB;
                                refPos1 = vector;
                            }
                            else if (index == max1)
                            {
                                num = (refPos2 - refPos1).sqrMagnitude;
                                num2 = minSqrDistanceA;
                            }
                            else
                            {
                                num2 = minSqrDistanceB;
                                refPos2 = vector;
                            }
                            vector.w = 1000f;
                            if (index == max2)
                            {
                                return;
                            }
                        }
                    }
                    b += 1;
                    b2 = 0;
                    if (index <= 0)
                    {
                        vehicleData.m_pathPositionIndex = b;
                        vehicleData.m_lastPathOffset = b2;
                    }
                }
                int num7 = (b >> 1) + 1; //pathPositionIndex is divided by 2 to get the final position index
                uint num8 = num3;
                if (num7 >= instance.m_pathUnits.m_buffer[(int) (UIntPtr) num3].m_positionCount)
                {
                    num7 = 0;
                    num8 = instance.m_pathUnits.m_buffer[(int) (UIntPtr) num3].m_nextPathUnit;
                    if (num8 == 0u)
                    {
                        goto Block_19;
                    }
                }

                if (!instance.m_pathUnits.m_buffer[(int) (UIntPtr) num8].GetPosition(num7, out var position2))
                {
                    goto Block_21;
                }
                NetInfo info = instance2.m_segments.m_buffer[position2.m_segment].Info;
                if (info.m_lanes.Length <= position2.m_lane)
                {
                    goto Block_22;
                }
                uint laneID = PathManager.GetLaneID(position2);
                NetInfo.Lane lane = info.m_lanes[position2.m_lane];
                if (lane.m_laneType != NetInfo.LaneType.Vehicle)
                {
                    goto Block_23;
                }
                if (position2.m_segment != position.m_segment && leaderID != 0)
                {
                    leaderData.m_flags &= ~Vehicle.Flags.Leaving;
                }
                byte b4;

                if (num4 != laneID) //num4 is last path lane, laneID is the new one. This triggers checkNextLane.
                {
                    PathUnit.CalculatePathPositionOffset(laneID, vector, out b4);
                    bezier = default;
                    trainAI.CalculateSegmentPosition(vehicleID, ref vehicleData, position, num4, position.m_offset, out bezier.a, out var vector3, out _);
                    bool flag2;
                    //checkNextLane only for the vehicle in front of the train
                    if ((leaderData.m_flags & Vehicle.Flags.Reversed) != 0)
                    {
                        flag2 = vehicleData.m_trailingVehicle == 0;
                    }
                    else
                    {
                        flag2 = vehicleData.m_leadingVehicle == 0;
                    }

                    bool flag3 = flag2 && b2 == 0;
                    trainAI.CalculateSegmentPosition(vehicleID, ref vehicleData, position2, laneID, b4, out bezier.d, out var vector4, out float num10);
                    if (position.m_offset == 0)
                    {
                        vector3 = -vector3;
                    }
                    if (b4 < position2.m_offset)
                    {
                        vector4 = -vector4;
                    }
                    vector3.Normalize();
                    vector4.Normalize();
                    NetSegment.CalculateMiddlePoints(bezier.a, vector3, bezier.d, vector4, true, true, out bezier.b, out bezier.c, out float num11);

                    if (flag3)
                    {
                        /***** ***** ***** ***** ***** ***** ***** ***** ***** ***** CUSTOM CODE START ***** ***** ***** ***** ***** ***** ***** ***** ***** *****/
                        bool mayNeedFix = false;
                        if (!CheckSingleTrack2Ways(vehicleID, vehicleData, ref num10, laneID, num4, ref mayNeedFix))
                        {
                            float savedMaxSpeed = num10;
                            trainAI.CheckNextLane(vehicleID, ref vehicleData, ref num10, position2, laneID, b4, position, num4, position.m_offset, bezier);

                            //address bug where some train are blocked after reversing at a single train track station
                            if (Settings.FixReverseTrainSingleTrackStation && mayNeedFix && num10 < 0.01f)
                            {
                                ushort vehiclePreviouslyReservingSpace = leaderID;
                                if ((leaderData.m_flags & Vehicle.Flags.Reversed) != 0)
                                {
                                    //vehiclePreviouslyReservingSpace = vehicleData.GetFirstVehicle(vehicleID);
                                    //CODebug.Log(LogChannel.Modding, Mod.modName + " - attempt fix(1) " + instance2.m_lanes.m_buffer[(int)((UIntPtr)laneID)].CheckSpace(1000f, vehiclePreviouslyReservingSpace));

                                    //try checkspace again with carriage at the other end of the train (the one who has, by supposition, reserved the space previously)
                                    if (instance2.m_lanes.m_buffer[(int) ((UIntPtr) laneID)].CheckSpace(1000f, vehiclePreviouslyReservingSpace))
                                    {
                                        num10 = savedMaxSpeed;
                                    }
                                    else
                                    {
                                        Segment3 segment = new Segment3(bezier.Position(0.5f), bezier.d);
                                        //CODebug.Log(LogChannel.Modding, Mod.modName + " - attempt fix(2) " + CheckOverlap(vehicleID, ref vehicleData, segment, vehiclePreviouslyReservingSpace));

                                        if (trainAI.CheckOverlap(vehicleID, ref vehicleData, segment, vehiclePreviouslyReservingSpace))
                                            num10 = savedMaxSpeed;
                                    }
                                }
                            }
                        }
                        /***** ***** ***** ***** ***** ***** ***** ***** ***** ***** CUSTOM CODE END ***** ***** ***** ***** ***** ***** ***** ***** ***** *****/
                    }
                    if (flag2 && (num10 < 0.01f || (instance2.m_segments.m_buffer[position2.m_segment].m_flags & (NetSegment.Flags.Collapsed | NetSegment.Flags.Flooded)) != NetSegment.Flags.None))
                    {
                        goto IL_595;
                    }
                    if (num11 > 1f)
                    {
                        ushort num12;
                        if (b4 == 0)
                        {
                            num12 = instance2.m_segments.m_buffer[position2.m_segment].m_startNode;
                        }
                        else if (b4 == 255)
                        {
                            num12 = instance2.m_segments.m_buffer[position2.m_segment].m_endNode;
                        }
                        else
                        {
                            num12 = 0;
                        }
                        float num13 = 1.57079637f * (1f + Vector3.Dot(vector3, vector4));
                        if (num11 > 1f)
                        {
                            num13 /= num11;
                        }
                        num10 = Mathf.Min(num10, trainAI.CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num13));

                        while (b2 < 255)
                        {
                            float num14 = Mathf.Max(Mathf.Sqrt(num) - Vector3.Distance(vector, refPos1), Mathf.Sqrt(num2) - Vector3.Distance(vector, refPos2));
                            int num15;
                            if (num14 < 0f)
                            {
                                num15 = 8;
                            }
                            else
                            {
                                num15 = 8 + Mathf.CeilToInt(num14 * 256f / (num11 + 1f));
                            }
                            b2 = (byte) Mathf.Min(b2 + num15, 255);
                            Vector3 a2 = bezier.Position(b2 * 0.003921569f);
                            vector.Set(a2.x, a2.y, a2.z, Mathf.Min(vector.w, num10));
                            float sqrMagnitude3 = (a2 - refPos1).sqrMagnitude;
                            float sqrMagnitude4 = (a2 - refPos2).sqrMagnitude;
                            if (sqrMagnitude3 >= num && sqrMagnitude4 >= num2)
                            {
                                if (index <= 0)
                                {
                                    vehicleData.m_lastPathOffset = b2;
                                }
                                if (num12 != 0)
                                {
                                    trainAI.UpdateNodeTargetPos(vehicleID, ref vehicleData, num12, ref instance2.m_nodes.m_buffer[num12], ref vector, index);
                                }
                                vehicleData.SetTargetPos(index++, vector);
                                if (index < max1)
                                {
                                    num = minSqrDistanceB;
                                    refPos1 = vector;
                                }
                                else if (index == max1)
                                {
                                    num = (refPos2 - refPos1).sqrMagnitude;
                                    num2 = minSqrDistanceA;
                                }
                                else
                                {
                                    num2 = minSqrDistanceB;
                                    refPos2 = vector;
                                }
                                vector.w = 1000f;
                                if (index == max2)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    PathUnit.CalculatePathPositionOffset(laneID, vector, out b4);
                }
                if (index <= 0)
                {
                    if (num7 == 0)
                    {
                        Singleton<PathManager>.instance.ReleaseFirstUnit(ref vehicleData.m_path);
                    }
                    if (num7 >= instance.m_pathUnits.m_buffer[(int) ((UIntPtr) num8)].m_positionCount - 1 && instance.m_pathUnits.m_buffer[(int) ((UIntPtr) num8)].m_nextPathUnit == 0u && leaderID != 0)
                    {
                        trainAI.ArrivingToDestination(leaderID, ref leaderData);
                    }
                }
                num3 = num8;
                b = (byte) (num7 << 1);
                b2 = b4;
                if (index <= 0)
                {
                    vehicleData.m_pathPositionIndex = b;
                    vehicleData.m_lastPathOffset = b2;
                    vehicleData.m_flags = ((vehicleData.m_flags & ~(Vehicle.Flags.OnGravel | Vehicle.Flags.Underground | Vehicle.Flags.Transition)) | info.m_setVehicleFlags);
                    if ((vehicleData.m_flags2 & Vehicle.Flags2.Yielding) != 0)
                    {
                        vehicleData.m_flags2 &= ~Vehicle.Flags2.Yielding;
                        vehicleData.m_waitCounter = 0;
                    }
                }
                position = position2;
                num4 = laneID;
            }

            Block_19:
            if (index <= 0)
            {
                Singleton<PathManager>.instance.ReleasePath(vehicleData.m_path);
                vehicleData.m_path = 0u;
            }
            vector.w = 1f;
            vehicleData.SetTargetPos(index++, vector);
            return;
            Block_21:
            trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
            return;
            Block_22:
            trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
            return;
            Block_23:
            trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
            return;
            IL_595:
            if (index <= 0)
            {
                vehicleData.m_lastPathOffset = b2;
            }
            vector = bezier.a;
            vector.w = 0f;
            while (index < max2)
            {
                vehicleData.SetTargetPos(index++, vector);
            }
        }

        private static bool CheckSingleTrack2Ways(
            ushort vehicleID,
            Vehicle vehicleData,
            ref float maxSpeed,
            uint laneID,
            uint prevLaneID,
            ref bool mayNeedSingleTrackStationFix)
        {
            NetManager instance = Singleton<NetManager>.instance;
            ushort next_segment_id = instance.m_lanes.m_buffer[(int)((UIntPtr)laneID)].m_segment;
            ushort crt_segment_id = instance.m_lanes.m_buffer[(int)((UIntPtr)prevLaneID)].m_segment;

            ReservationManager instance2 = ReservationManager.instance;

            ushort leadingVehicleID = vehicleData.GetFirstVehicle(vehicleID);

            ReservationInfo ri = null;
            bool preventCheckNextLane = false;
            bool notifyFutureTrack = false;

            if (ReservationManager.RequireReservation(next_segment_id))
                ri = instance2.GetReservationOnSegment(next_segment_id);


            CreateReservation:
            if (ReservationManager.IsSingleTrack2WSegment(next_segment_id)) //train carriage will enter a one lane section
            {
                if (ri == null) //reserve track if it is not reserved by any train
                {
                    ushort blocking_segmentID = 0;

                    ri = ReservationManager.instance.CheckCachedReservation(next_segment_id, leadingVehicleID, ref blocking_segmentID);

                    if (ri == null) //no cached reservation found, create one
                    {
                        SingleTrack2WSection section = instance2.CreateSingleTrack2WSectionFromTrainPath(leadingVehicleID, next_segment_id);
                        if (section != null)
                        {
                            ri = ReservationManager.instance.RegisterNewReservation(section, leadingVehicleID, ref blocking_segmentID);
                        }
                    }

                    if (blocking_segmentID != 0) //reservation blocked by a further segment already reserved, get this reservation
                    {
                        ri = instance2.GetReservationOnSegment(blocking_segmentID);
                        /*ReservationManager.instance.EnqueueReservation(ri, leadingVehicleID);
                        maxSpeed = 0f;
                        return true;*/
                    }
                    else
                    {
                        mayNeedSingleTrackStationFix = true; //track reserved by this train
                    }
                }
            }

            if (ri != null)
            {

                if (ReservationManager.IsReservationForTrain(ri, leadingVehicleID)) //track is reserved for this vehicle
                {
                    notifyFutureTrack = true;

                    mayNeedSingleTrackStationFix = true; //track reserved by this train

                    //reset wait counter
                    /* if((vehicleData.m_flags2 & Vehicle.Flags2.Yielding) != (Vehicle.Flags2) 0)
                     {
                         vehicleData.m_flags2 &= ~Vehicle.Flags2.Yielding;
                         vehicleData.m_waitCounter = 0;
                     }*/


                    //return true so that CheckNextLane does not interfere (it causes train to stop when going from one track to double track with a train waiting in the opposite direction)
                    if (Settings.NoCheckOverlapOnLastSegment && next_segment_id == ri.section.segment_ids[ri.section.segment_ids.Count - 1])
                        preventCheckNextLane = true;
                }
                else //section reserved by another train
                {
                    //train has spawned on a station reserved to another train, though case...
                    //attempt destroy reservation and give priority to this train
                    if (ReservationManager.IsSingleTrackStation(crt_segment_id))
                    {
                        ReservationManager.instance.m_data.RemoveReservation(ri.ID, true);
                        ri = null;
                        goto CreateReservation;
                    }

                    SingleTrack2WSection section = instance2.CreateSingleTrack2WSectionFromTrainPath(leadingVehicleID, next_segment_id);
                    if (!(section != null && ReservationManager.instance.AttemptJoinReservation(ri, section, leadingVehicleID))) //can train follow the previous one?
                    {
                        if (!(section != null && ReservationManager.instance.AttemptReservationForNextPendingTrain(ri, section, leadingVehicleID))) //has section been cleared?
                        {
                            //not allowed on this track, stop
                            ReservationManager.instance.EnqueueReservation(ri, leadingVehicleID);
                            maxSpeed = 0f;

                            //increment wait counter
                            /*vehicleData.m_flags2 |= Vehicle.Flags2.Yielding;
                            vehicleData.m_waitCounter++;*/

                            //set traffic light state
                            /*NetSegment seg = instance.m_segments.m_buffer[crt_segment_id];
                            RoadBaseAI.SetTrafficLightState(seg.m_endNode, ref seg, 0, RoadBaseAI.TrafficLightState.Red, RoadBaseAI.TrafficLightState.Red, true, false);
                            RoadBaseAI.SetTrafficLightState(seg.m_startNode, ref seg, 0, RoadBaseAI.TrafficLightState.Red, RoadBaseAI.TrafficLightState.Red, true, false);
                            instance.m_nodes.m_buffer[seg.m_endNode].m_flags |= NetNode.Flags.TrafficLights;
                            instance.m_nodes.m_buffer[seg.m_startNode].m_flags |= NetNode.Flags.TrafficLights;

                            /*if (vehicleID == leadingVehicleID)
                            {
                                instance.m_segments.m_buffer[crt_segment_id].m_trafficLightState0 = (byte) RoadBaseAI.TrafficLightState.Red;
                            }*/
                            preventCheckNextLane = true;
                        }
                    }
                }

                //assess if single track station fix is necessary, before Notify which can cancel TrainAtStation status (if new front carriage is out of station for example)
                if (mayNeedSingleTrackStationFix && ri.status != ReservationStatus.TrainAtStation)
                    mayNeedSingleTrackStationFix = false;
            }

            if (ReservationManager.RequireReservation(crt_segment_id)) //train carriage is on a one lane section (or double track station which may belong to a single track section)
            {
                instance2.NotifyReservation(leadingVehicleID, crt_segment_id, true, vehicleData.m_flags);
            }

            if (notifyFutureTrack)
                instance2.NotifyReservation(leadingVehicleID, next_segment_id, false);

            return preventCheckNextLane;
        }
    }
}
