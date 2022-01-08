using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Makes locked open doors unlocked.
/// </summary>
public class UnlockOpenDoors : IModApi
{
    /// <summary>
    /// Mod initialization.
    /// </summary>
    /// <param name="_modInstance"></param>
    public void InitMod(Mod _modInstance)
    {
        Debug.Log("Loading mod: " + GetType().ToString());
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// The Harmony patch for the method <see cref="TileEntity.OnReadComplete"/>.
    /// </summary>
    [HarmonyPatch(typeof(TileEntity))]
    [HarmonyPatch("OnReadComplete")]
    public class TileEntity_OnReadComplete
    {
        /// <summary>
        /// The additional code to execute after the original method <see cref="TileEntity.OnReadComplete"/>.
        /// Makes locked open doors/hatches/gates unlocked.
        /// </summary>
        public static void Postfix(TileEntity __instance)
        {
            if (__instance is TileEntitySecureDoor door && door.GetOwner() == null)
            {
                if (door.IsLocked() && BlockDoor.IsDoorOpen(door.blockValue.meta))
                {
                    door.SetLocked(false);
                    //Debug.LogErrorFormat($"Annoying door: {door.blockValue.Block?.GetBlockName()}, {door.ToWorldPos()}");
                }
            }
        }
    }
    
    /// <summary>
    /// The Harmony patch for the method <see cref="BlockDoorSecure.OnTriggered"/>.
    /// </summary>
    [HarmonyPatch(typeof(BlockDoorSecure))]
    [HarmonyPatch("OnTriggered")]
    public class BlockDoorSecure_OnTriggered
    {
        /// <summary>
        /// The additional code to execute after the original method <see cref="BlockDoorSecure.OnTriggered"/>.
        /// Unlocks door/hatch/gates in case it was opened by key or switch.
        /// </summary>
        public static void Postfix(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue)
        {
            if (BlockDoor.IsDoorOpen(_blockValue.meta))
            {
                var door = _world.GetTileEntity(_cIdx, _blockPos) as TileEntitySecureDoor;

                if (door != null && door.IsLocked() && door.GetOwner() == null)
                {
                    door.SetLocked(false);
                }
            }
        }
    }

    /// <summary>
    /// The Harmony patch for the method <see cref="PrefabInstance.ResetBlocksAndRebuild"/>.
    /// </summary>
    [HarmonyPatch(typeof(PrefabInstance))]
    [HarmonyPatch("ResetBlocksAndRebuild")]
    public class PrefabInstance_ResetBlocksAndRebuild
    {
        /// <summary>
        /// The additional code to execute before the original method <see cref="PrefabInstance.ResetBlocksAndRebuild"/>.
        /// Removes all doors/hatches/gates before POI reset (when start quest).
        /// It forces the game to create new doors with the correct locked state for closed doors that are supposed to be opened by a special key or switch.
        /// </summary>
        public static bool Prefix(World _world, PrefabInstance __instance)
        {
            HashSetLong hashSetLong = __instance.GetOccupiedChunks();
            ChunkCluster chunkCluster = _world.ChunkClusters[0];
            
            foreach (long current in hashSetLong)
            {
                Chunk chunkSync = chunkCluster.GetChunkSync(current);
                if (chunkSync != null)
                {
                    var list = chunkSync.GetTileEntities().list;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var tile = list[i];
                        if (chunkSync.GetBlock(tile.localChunkPos).Block.HasTileEntity)
                        {
                            if (tile is TileEntitySecureDoor)
                            {
                                chunkSync.SetBlock(_world, tile.localChunkPos.x, tile.localChunkPos.y, tile.localChunkPos.z, BlockValue.Air, true, true);
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// The additional code to execute after the original method <see cref="PrefabInstance.ResetBlocksAndRebuild"/>.
        /// After POI reset (when quest started) makes locked open doors/hatches/gates unlocked.
        /// </summary>
        public static void Postfix(World _world, PrefabInstance __instance)
        {
            HashSetLong hashSetLong = __instance.GetOccupiedChunks();
            ChunkCluster chunkCluster = _world.ChunkClusters[0];

            foreach (long current in hashSetLong)
            {
                Chunk chunkSync = chunkCluster.GetChunkSync(current);
                if (chunkSync != null)
                {
                    var list = chunkSync.GetTileEntities().list;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var tile = list[i];
                        if (chunkSync.GetBlock(tile.localChunkPos).Block.HasTileEntity)
                        {
                            if (tile is TileEntitySecureDoor door && door.GetOwner() == null)
                            {
                                if (door.IsLocked() && BlockDoor.IsDoorOpen(door.blockValue.meta))
                                {
                                    door.SetLocked(false);
                                    //Debug.LogErrorFormat($"Annoying door: {door.blockValue.Block?.GetBlockName()}, {door.ToWorldPos()}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /*
    [HarmonyPatch(typeof(TileEntitySecure))]
    [HarmonyPatch("IsUserAllowed")]
    public class TileEntitySecure_IsUserAllowed
    {
        public static void Postfix(TileEntitySecure __instance, System.Collections.Generic.List<PlatformUserIdentifierAbs> ___allowedUserIds, PlatformUserIdentifierAbs ___ownerID, ref bool __result)
        {
            if (__instance is TileEntitySecureDoor && ___ownerID == null && __instance.IsLocked())
            {
                __result = true;
            }
        }
    }
    */
}
