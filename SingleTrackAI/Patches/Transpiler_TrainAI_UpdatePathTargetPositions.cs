using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using JetBrains.Annotations;
using SingleTrackAI.AI;
using UnityEngine;

namespace SingleTrackAI.Patches
{
    [HarmonyPatch]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class Transpiler_TrainAI_UpdatePathTargetPositions
    {
        public static bool NoCheckOverlapOnLastSegment { get; set; } = false;

        public static bool FixReverseTrainSingleTrackStation { get; set; } = true;

        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {

            var updatePathTargetPositions = typeof(TrainAI).GetMethod(
                name: "UpdatePathTargetPositions",
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(ushort), // vehicleID,
                    typeof(Vehicle).MakeByRefType(), // vehicleData,
                    typeof(Vector3), // refPos1,
                    typeof(Vector3), // refPos2,
                    typeof(ushort), // leaderID,
                    typeof(Vehicle).MakeByRefType(), // leaderData,
                    typeof(int).MakeByRefType(), // index,
                    typeof(int), // max1,
                    typeof(int), // max2,
                    typeof(float), // minSqrDistanceA,
                    typeof(float) // minSqrDistanceB)
                },
                modifiers: null);

            if (updatePathTargetPositions == null)
                Logger.Debug("UpdatePathTargetPositions not found");

            return updatePathTargetPositions;
        }

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions)
        {
            var calculateMiddlePointsCall = typeof(NetSegment).GetMethod(
                name: nameof(NetSegment.CalculateMiddlePoints),
                bindingAttr: BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(Vector3), // startPos
                    typeof(Vector3), // startDir
                    typeof(Vector3), // endPos
                    typeof(Vector3), // endDir
                    typeof(bool), // smoothStart
                    typeof(bool), // smoothEnd
                    typeof(Vector3).MakeByRefType(), // middlePos1
                    typeof(Vector3).MakeByRefType(), // middlePos2
                    typeof(float).MakeByRefType() // distance
                },
                modifiers: null
            );

            bool found = false;

            var instructionsList = instructions.ToList();

            for (var i = 0; i < instructionsList.Count; i++)
            {
                yield return instructionsList[i];

                if (instructionsList[i].opcode == OpCodes.Call && instructionsList[i].operand == calculateMiddlePointsCall)
                {
                    found = true;

                    yield return instructionsList[i + 1]; // ldloc.s
                    yield return instructionsList[i + 2]; // brfalse

                    i += 2;

                    yield return new CodeInstruction(OpCodes.Ldarg_0); // trainAI
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // vehicleID
                    yield return new CodeInstruction(OpCodes.Ldarg_2); // vehicleData
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5); // leaderID
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 6); // leaderData
                    yield return new CodeInstruction(OpCodes.Ldobj, typeof(Vehicle)); // ['Assembly-CSharp']Vehicle
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 33); // maxSpeed
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 26);  // laneId
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 9);   // prevLaneID / index2
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 24);  // pathPos / position2
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 28);  // pathPosOffset / offset2
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 8);   // prevPathPos / position1
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 29);  // bezier
                    yield return new CodeInstruction(OpCodes.Ldloc_1, 1);   // netManager / instance2
                    yield return CodeInstruction.Call(typeof(Transpiler_TrainAI_UpdatePathTargetPositions), nameof(CustomCode));

                    /*
                      IL_05d7: ldarg.0      trainAI
                      IL_05d8: ldarg.1      vehicleID
                      IL_05d9: ldarg.2      vehicleData
                      IL_05da: ldarg.s      leaderID
                      IL_05dc: ldarg.s      leaderData
                      IL_05de: ldobj        ['Assembly-CSharp']Vehicle
                      IL_05e3: ldloca.s     maxSpeed_V_54
                      IL_05e5: ldloc.s      laneId
                      IL_05e7: ldloc.s      prevLaneID
                      IL_05e9: ldloc.s      pathPos
                      IL_05eb: ldloc.s      pathPosOffset
                      IL_05ed: ldloc.s      prevPathPos
                      IL_05ef: ldloc.s      bezier
                      IL_05f1: ldloc.1      netManager
                      IL_05f2: call         void CustomCode(...)
                     */
                }
            }

            if (!found)
                throw new Exception($"Could not find {nameof(NetSegment)}.{nameof(NetSegment.CalculateMiddlePoints)} call or instructions have been patched.");
        }

        private static void CustomCode(
            TrainAI trainAI,
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
            TrainAiHook hookedTrainAI = new TrainAiHook(trainAI);

            bool mayNeedFix = false;
            if (!CheckSingleTrack2Ways(vehicleID, vehicleData, ref maxSpeed, laneID, prevLaneID, ref mayNeedFix))
            {
                float savedMaxSpeed = maxSpeed;
                hookedTrainAI.CheckNextLane(vehicleID, ref vehicleData, ref maxSpeed, pathPos, laneID, pathPosOffset, prevPathPos,
                    prevLaneID, prevPathPos.m_offset, bezier);

                //address bug where some train are blocked after reversing at a single train track station
                if (FixReverseTrainSingleTrackStation && mayNeedFix && maxSpeed < 0.01f)
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

                            if (hookedTrainAI.CheckOverlap(vehicleID, ref vehicleData, segment, vehiclePreviouslyReservingSpace))
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
                    if (NoCheckOverlapOnLastSegment && next_segment_id == ri.section.segment_ids[ri.section.segment_ids.Count - 1])
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
