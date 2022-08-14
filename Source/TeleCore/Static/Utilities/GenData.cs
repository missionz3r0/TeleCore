﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public static class GenData
    {
        public static string Location(this Texture texture)
        {
            if (texture is not Texture2D tx2D)
            {
                TLog.Error($"Tried to find {texture} location as non Texture2D");
                return null;
            }
            return LoadedModManager.RunningMods.SelectMany(m => m.textures.contentList).First(t => t.Value == tx2D).Key;
        }

        public static string Location(this Shader shader)
        {
            return DefDatabase<ShaderTypeDef>.AllDefs.First(t => t.Shader == shader).shaderPath;
        }

        public static IEnumerable<T> AllFlags<T>(this T enumType) where T : Enum
        {
            return enumType.GetAllSelectedItems<T>();
        }

        public static T ObjectValue<T>(this XmlNode node, bool doPostLoad = true)
        {
            return DirectXmlToObject.ObjectFromXml<T>(node, doPostLoad);
        }

        public static bool IsCustomLinked(this Graphic graphic)
        {
            if (graphic is Graphic_LinkedWithSame or Graphic_LinkedNetworkStructure)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers an action to be ticked every single tick.
        /// </summary>
        public static void RegisterTickAction(this Action action)
        {
            TeleUpdateManager.Notify_AddNewTickAction(action);
        }

        /// <summary>
        /// Enqueues an action to be run once on the main thread when available.
        /// </summary>
        public static void EnqueueActionForMainThread(this Action action)
        {
            TeleUpdateManager.Notify_EnqueueNewSingleAction(action);
        }

        /// <summary>
        /// Defines whether a structure is powered by electricity and returns whether it actually uses power.
        /// </summary>
        public static bool IsElectricallyPowered(this ThingWithComps thing, out bool usesPower)
        {
            var comp = thing.GetComp<CompPowerTrader>();
            usesPower = comp != null;
            return usesPower && comp.PowerOn;
        }
        
        /// <summary>
        /// If the thing uses a PowerComp, returns the PowerOn property, otherwise returns true if no PowerComp exists.
        /// </summary>
        public static bool IsPoweredOn(this ThingWithComps thing)
        {
            return thing.IsElectricallyPowered(out var usesPower) || !usesPower;
        }

        /// <summary>
        /// Checks whether a thing is reserved by any pawn.
        /// </summary>
        public static bool IsReserved(this Thing thing, Map onMap, out Pawn reservedBy)
        {
            reservedBy = null;
            if (thing == null) return false;
            var reservations = onMap.reservationManager;
            reservedBy = reservations.ReservationsReadOnly.Find(r => r.Target == thing)?.Claimant;
            return reservedBy != null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Room NeighborRoomOf(this Building building, Room room)
        {
            for (int i = 0; i < 4; i++)
            {
                Room newRoom = (building.Position + GenAdj.CardinalDirections[i]).GetRoom(room.Map);
                if (newRoom == null || newRoom == room) continue;
                return newRoom;
            }
            return null;
        }

        /// <summary>
        /// Returns the current room at a position.
        /// </summary>
        public static Room GetRoomFast(this IntVec3 pos, Map map)
        {
            Region validRegion = map.regionGrid.GetValidRegionAt_NoRebuild(pos);
            if (validRegion != null && validRegion.type.Passable())
            {
                return validRegion.Room;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Room GetRoomIndirect(this Thing thing)
        {
            var room = thing.GetRoom();
            if (room == null)
            {
                room = thing.CellsAdjacent8WayAndInside().Select(c => c.GetRoom(thing.Map)).First(r => r != null);
            }
            return room;
        }

        /// <summary>
        /// Get the desired <see cref="MapInformation"/> based on type of <typeparamref name="T"/>.
        /// </summary>
        public static T GetMapInfo<T>(this Map map) where T : MapInformation
        {
            return map.TeleCore().GetMapInfo<T>();
        }

        /// <summary>
        /// Get the desired <see cref="Designator"/> based on type of <typeparamref name="T"/>.
        /// </summary>
        public static T GetDesignatorFor<T>(ThingDef def) where T : Designator
        {
            if (StaticData.CachedDesignators.TryGetValue(def, out var des))
            {
                return (T)des;
            }

            des = (Designator)Activator.CreateInstance(typeof(T), def);
            des.icon = def.uiIcon;
            StaticData.CachedDesignators.Add(def, des);
            return (T)des;
        }

        //Room Tracking
        /// <returns>The main <see cref="TeleCore.RoomTracker"/> object of the <paramref name="room"/>.</returns>
        public static RoomTracker RoomTracker(this Room room)
        {
            return room.Map.GetMapInfo<RoomTrackerMapInfo>()[room];
        }

        /// <summary>
        /// Get the desired <see cref="RoomComponent"/> based on type of <typeparamref name="T"/>.
        /// </summary>
        public static T GetRoomComp<T>(this Room room) where T : RoomComponent
        {
            return room.RoomTracker()?.GetRoomComp<T>();
        }

        public static IEnumerable<Thing> OfType<T>(this ListerThings lister) where T : Thing
        {
            return lister.AllThings.Where(t => t is T);
        }
    }
}
