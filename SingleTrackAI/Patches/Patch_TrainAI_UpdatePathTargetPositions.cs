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
            NetManager netManager = Singleton<NetManager>.instance;

            Vector4 targetPos0 = vehicleData.m_targetPos0;
            targetPos0.w = 1000f;
            float minSqrDistanceAcopy = minSqrDistanceA;
            float minSqrDistance = 0f;
            uint path = vehicleData.m_path;
            byte pathPositionIndex = vehicleData.m_pathPositionIndex;
            byte lastPathOffset = vehicleData.m_lastPathOffset;
            if (pathPositionIndex == 255)
            {
                pathPositionIndex = 0;
                if (index <= 0)
                {
                    vehicleData.m_pathPositionIndex = 0;
                }
                if (!Singleton<PathManager>.instance.m_pathUnits.m_buffer[(int) ((UIntPtr) path)].CalculatePathPositionOffset(pathPositionIndex >> 1, targetPos0, out lastPathOffset))
                {
                    trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                    return;
                }
            }

            if (!instance.m_pathUnits.m_buffer[(int) ((UIntPtr) path)].GetPosition(pathPositionIndex >> 1, out var prevPathPos))
            {
                trainAI.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                return;
            }
            uint prevLaneID = PathManager.GetLaneID(prevPathPos);
            //try to notify for all vehicles composing the train. however, UpdatePathTargetPositions is not called when train is stopped
            //NotifySingleTrack2Ways(vehicleID, vehicleData, num4);
            Bezier3 bezier;
            while (true)
            {
                if ((pathPositionIndex & 1) == 0) //is pathPositionIndex even?
                {
                    bool flag = true;
                    while (lastPathOffset != prevPathPos.m_offset)
                    {
                        if (flag)
                        {
                            flag = false;
                        }
                        else
                        {
                            float num5 = Mathf.Max(Mathf.Sqrt(minSqrDistanceAcopy) - Vector3.Distance(targetPos0, refPos1), Mathf.Sqrt(minSqrDistance) - Vector3.Distance(targetPos0, refPos2));
                            int num6;
                            if (num5 < 0f)
                            {
                                num6 = 4;
                            }
                            else
                            {
                                num6 = 4 + Mathf.CeilToInt(num5 * 256f / (netManager.m_lanes.m_buffer[(int) (UIntPtr) prevLaneID].m_length + 1f));
                            }
                            if (lastPathOffset > prevPathPos.m_offset)
                            {
                                lastPathOffset = (byte) Mathf.Max(lastPathOffset - num6, prevPathPos.m_offset);
                            }
                            else if (lastPathOffset < prevPathPos.m_offset)
                            {
                                lastPathOffset = (byte) Mathf.Min(lastPathOffset + num6, prevPathPos.m_offset);
                            }
                        }

                        trainAI.CalculateSegmentPosition(vehicleID, ref vehicleData, prevPathPos, prevLaneID, lastPathOffset, out var a, out _, out float b3);
                        targetPos0.Set(a.x, a.y, a.z, Mathf.Min(targetPos0.w, b3));
                        float sqrMagnitude = (a - refPos1).sqrMagnitude;
                        float sqrMagnitude2 = (a - refPos2).sqrMagnitude;
                        if (sqrMagnitude >= minSqrDistanceAcopy && sqrMagnitude2 >= minSqrDistance)
                        {
                            if (index <= 0)
                            {
                                vehicleData.m_lastPathOffset = lastPathOffset;
                            }
                            vehicleData.SetTargetPos(index++, targetPos0);
                            if (index < max1)
                            {
                                minSqrDistanceAcopy = minSqrDistanceB;
                                refPos1 = targetPos0;
                            }
                            else if (index == max1)
                            {
                                minSqrDistanceAcopy = (refPos2 - refPos1).sqrMagnitude;
                                minSqrDistance = minSqrDistanceA;
                            }
                            else
                            {
                                minSqrDistance = minSqrDistanceB;
                                refPos2 = targetPos0;
                            }
                            targetPos0.w = 1000f;
                            if (index == max2)
                            {
                                return;
                            }
                        }
                    }
                    pathPositionIndex += 1;
                    lastPathOffset = 0;
                    if (index <= 0)
                    {
                        vehicleData.m_pathPositionIndex = pathPositionIndex;
                        vehicleData.m_lastPathOffset = lastPathOffset;
                    }
                }
                int num7 = (pathPositionIndex >> 1) + 1; //pathPositionIndex is divided by 2 to get the final position index
                uint num8 = path;
                if (num7 >= instance.m_pathUnits.m_buffer[(int) (UIntPtr) path].m_positionCount)
                {
                    num7 = 0;
                    num8 = instance.m_pathUnits.m_buffer[(int) (UIntPtr) path].m_nextPathUnit;
                    if (num8 == 0u)
                    {
                        goto Block_19;
                    }
                }

                if (!instance.m_pathUnits.m_buffer[(int) (UIntPtr) num8].GetPosition(num7, out var pathPos))
                {
                    goto Block_21;
                }
                NetInfo info = netManager.m_segments.m_buffer[pathPos.m_segment].Info;
                if (info.m_lanes.Length <= pathPos.m_lane)
                {
                    goto Block_22;
                }
                uint laneID = PathManager.GetLaneID(pathPos);
                NetInfo.Lane lane = info.m_lanes[pathPos.m_lane];
                if (lane.m_laneType != NetInfo.LaneType.Vehicle)
                {
                    goto Block_23;
                }
                if (pathPos.m_segment != prevPathPos.m_segment && leaderID != 0)
                {
                    leaderData.m_flags &= ~Vehicle.Flags.Leaving;
                }
                byte pathPosOffset;

                if (prevLaneID != laneID) //num4 is last path lane, laneID is the new one. This triggers checkNextLane.
                {
                    PathUnit.CalculatePathPositionOffset(laneID, targetPos0, out pathPosOffset);
                    bezier = default;
                    trainAI.CalculateSegmentPosition(vehicleID, ref vehicleData, prevPathPos, prevLaneID, prevPathPos.m_offset, out bezier.a, out var vector3, out _);
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

                    bool flag3 = flag2 && lastPathOffset == 0;
                    trainAI.CalculateSegmentPosition(vehicleID, ref vehicleData, pathPos, laneID, pathPosOffset, out bezier.d, out var vector4, out float maxSpeed);
                    if (prevPathPos.m_offset == 0)
                    {
                        vector3 = -vector3;
                    }
                    if (pathPosOffset < pathPos.m_offset)
                    {
                        vector4 = -vector4;
                    }
                    vector3.Normalize();
                    vector4.Normalize();
                    NetSegment.CalculateMiddlePoints(bezier.a, vector3, bezier.d, vector4, true, true, out bezier.b, out bezier.c, out float num11);

                    if (flag3)
                    {
                        /***** ***** ***** ***** ***** ***** ***** ***** ***** ***** CUSTOM CODE START ***** ***** ***** ***** ***** ***** ***** ***** ***** *****/
                        CustomCode(
                            trainAI,
                            vehicleID,
                            ref vehicleData,
                            leaderID,
                            leaderData,
                            ref maxSpeed,
                            laneID,
                            prevLaneID,
                            pathPos,
                            pathPosOffset,
                            prevPathPos,
                            bezier,
                            netManager);
                        /***** ***** ***** ***** ***** ***** ***** ***** ***** ***** CUSTOM CODE END ***** ***** ***** ***** ***** ***** ***** ***** ***** *****/
                    }
                    if (flag2 && (maxSpeed < 0.01f || (netManager.m_segments.m_buffer[pathPos.m_segment].m_flags & (NetSegment.Flags.Collapsed | NetSegment.Flags.Flooded)) != NetSegment.Flags.None))
                    {
                        goto IL_595;
                    }
                    if (num11 > 1f)
                    {
                        ushort num12;
                        if (pathPosOffset == 0)
                        {
                            num12 = netManager.m_segments.m_buffer[pathPos.m_segment].m_startNode;
                        }
                        else if (pathPosOffset == 255)
                        {
                            num12 = netManager.m_segments.m_buffer[pathPos.m_segment].m_endNode;
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
                        maxSpeed = Mathf.Min(maxSpeed, trainAI.CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num13));

                        while (lastPathOffset < 255)
                        {
                            float num14 = Mathf.Max(Mathf.Sqrt(minSqrDistanceAcopy) - Vector3.Distance(targetPos0, refPos1), Mathf.Sqrt(minSqrDistance) - Vector3.Distance(targetPos0, refPos2));
                            int num15;
                            if (num14 < 0f)
                            {
                                num15 = 8;
                            }
                            else
                            {
                                num15 = 8 + Mathf.CeilToInt(num14 * 256f / (num11 + 1f));
                            }
                            lastPathOffset = (byte) Mathf.Min(lastPathOffset + num15, 255);
                            Vector3 a2 = bezier.Position(lastPathOffset * 0.003921569f);
                            targetPos0.Set(a2.x, a2.y, a2.z, Mathf.Min(targetPos0.w, maxSpeed));
                            float sqrMagnitude3 = (a2 - refPos1).sqrMagnitude;
                            float sqrMagnitude4 = (a2 - refPos2).sqrMagnitude;
                            if (sqrMagnitude3 >= minSqrDistanceAcopy && sqrMagnitude4 >= minSqrDistance)
                            {
                                if (index <= 0)
                                {
                                    vehicleData.m_lastPathOffset = lastPathOffset;
                                }
                                if (num12 != 0)
                                {
                                    trainAI.UpdateNodeTargetPos(vehicleID, ref vehicleData, num12, ref netManager.m_nodes.m_buffer[num12], ref targetPos0, index);
                                }
                                vehicleData.SetTargetPos(index++, targetPos0);
                                if (index < max1)
                                {
                                    minSqrDistanceAcopy = minSqrDistanceB;
                                    refPos1 = targetPos0;
                                }
                                else if (index == max1)
                                {
                                    minSqrDistanceAcopy = (refPos2 - refPos1).sqrMagnitude;
                                    minSqrDistance = minSqrDistanceA;
                                }
                                else
                                {
                                    minSqrDistance = minSqrDistanceB;
                                    refPos2 = targetPos0;
                                }
                                targetPos0.w = 1000f;
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
                    PathUnit.CalculatePathPositionOffset(laneID, targetPos0, out pathPosOffset);
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
                path = num8;
                pathPositionIndex = (byte) (num7 << 1);
                lastPathOffset = pathPosOffset;
                if (index <= 0)
                {
                    vehicleData.m_pathPositionIndex = pathPositionIndex;
                    vehicleData.m_lastPathOffset = lastPathOffset;
                    vehicleData.m_flags = ((vehicleData.m_flags & ~(Vehicle.Flags.OnGravel | Vehicle.Flags.Underground | Vehicle.Flags.Transition)) | info.m_setVehicleFlags);
                    if ((vehicleData.m_flags2 & Vehicle.Flags2.Yielding) != 0)
                    {
                        vehicleData.m_flags2 &= ~Vehicle.Flags2.Yielding;
                        vehicleData.m_waitCounter = 0;
                    }
                }
                prevPathPos = pathPos;
                prevLaneID = laneID;
            }

            Block_19:
            if (index <= 0)
            {
                Singleton<PathManager>.instance.ReleasePath(vehicleData.m_path);
                vehicleData.m_path = 0u;
            }
            targetPos0.w = 1f;
            vehicleData.SetTargetPos(index++, targetPos0);
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
                vehicleData.m_lastPathOffset = lastPathOffset;
            }
            targetPos0 = bezier.a;
            targetPos0.w = 0f;
            while (index < max2)
            {
                vehicleData.SetTargetPos(index++, targetPos0);
            }
        }

        private static void CustomCode(
            TrainAiHook trainAI,
            ushort vehicleID,
            ref Vehicle vehicleData,
            ushort leaderID,
            Vehicle leaderData,
            ref float maxSpeed,
            uint laneID,
            uint prevLaneID,
            PathUnit.Position pathPos,
            byte pathPosOffset,
            PathUnit.Position prevPathPos,
            Bezier3 bezier,
            NetManager netManager)
        {
            bool mayNeedFix = false;
            if (!CheckSingleTrack2Ways(vehicleID, vehicleData, ref maxSpeed, laneID, prevLaneID, ref mayNeedFix))
            {
                float savedMaxSpeed = maxSpeed;
                trainAI.CheckNextLane(vehicleID, ref vehicleData, ref maxSpeed, pathPos, laneID, pathPosOffset, prevPathPos,
                    prevLaneID, prevPathPos.m_offset, bezier);

                //address bug where some train are blocked after reversing at a single train track station
                if (Settings.FixReverseTrainSingleTrackStation && mayNeedFix && maxSpeed < 0.01f)
                {
                    ushort vehiclePreviouslyReservingSpace = leaderID;
                    if ((leaderData.m_flags & Vehicle.Flags.Reversed) != 0)
                    {
                        //vehiclePreviouslyReservingSpace = vehicleData.GetFirstVehicle(vehicleID);
                        //CODebug.Log(LogChannel.Modding, Mod.modName + " - attempt fix(1) " + instance2.m_lanes.m_buffer[(int)((UIntPtr)laneID)].CheckSpace(1000f, vehiclePreviouslyReservingSpace));

                        //try checkspace again with carriage at the other end of the train (the one who has, by supposition, reserved the space previously)
                        if (netManager.m_lanes.m_buffer[(int) ((UIntPtr) laneID)]
                            .CheckSpace(1000f, vehiclePreviouslyReservingSpace))
                        {
                            maxSpeed = savedMaxSpeed;
                        }
                        else
                        {
                            Segment3 segment = new Segment3(bezier.Position(0.5f), bezier.d);
                            //CODebug.Log(LogChannel.Modding, Mod.modName + " - attempt fix(2) " + CheckOverlap(vehicleID, ref vehicleData, segment, vehiclePreviouslyReservingSpace));

                            if (trainAI.CheckOverlap(vehicleID, ref vehicleData, segment, vehiclePreviouslyReservingSpace))
                                maxSpeed = savedMaxSpeed;
                        }
                    }
                }
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
