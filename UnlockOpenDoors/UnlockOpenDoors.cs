using HarmonyLib;
using System;
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
    /// Makes locked open doors/hatches/gates unlocked.
    /// </summary>
    [HarmonyPatch(typeof(TileEntitySecure))]
    [HarmonyPatch("SetLocked")]
    public class TileEntitySecure_SetLocked
    {
        public static void Postfix(TileEntitySecure __instance)
        {
            if (__instance.IsLocked() && __instance is TileEntitySecureDoor && __instance.GetOwner() == null && BlockDoor.IsDoorOpen(__instance.blockValue.meta))
            {
                //Debug.LogErrorFormat($"(SetLocked) Annoying door: {__instance.blockValue.Block?.GetBlockName()}, Position: {ToCompasPos(__instance.ToWorldPos())}");
                __instance.SetLocked(false);
            }
        }
    }

    /// <summary>
    /// Makes locked open doors/hatches/gates unlocked (for already discovered doors).
    /// </summary>
    [HarmonyPatch(typeof(TileEntity))]
    [HarmonyPatch("OnReadComplete")]
    public class TileEntity_OnReadComplete
    {
        public static void Postfix(TileEntity __instance)
        {
            if (__instance is TileEntitySecureDoor door && door.IsLocked())
            {
                if (BlockDoor.IsDoorOpen(door.blockValue.meta) && door.GetOwner() == null)
                {
                    //Debug.LogErrorFormat($"(OnReadComplete) Annoying door: {door.blockValue.Block?.GetBlockName()}, Position: {ToCompasPos(door.ToWorldPos())}");
                    door.SetLocked(false);
                }
            }
        }
    }

    /// <summary>
    /// Unlocks door/hatch/gates in case it was opened by a key or switch.
    /// </summary>
    [HarmonyPatch(typeof(BlockDoorSecure))]
    [HarmonyPatch("OnTriggered")]
    public class BlockDoorSecure_OnTriggered
    {
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
    /// Restores the initial locked state for previously opened doors with a special key or switch.
    /// </summary>
    [HarmonyPatch(typeof(QuestEventManager))]
    [HarmonyPatch("QuestLockPOI")]
    public class QuestEventManager_QuestLockPOI
    {
        public static void Postfix(Vector3 prefabPos, FastTags questTags)
        {
            var world = GameManager.Instance.World;
            var prefabs = GameManager.Instance
                .GetDynamicPrefabDecorator()
                .GetPrefabsFromWorldPosInside(prefabPos, questTags);

            foreach (var prefab in prefabs)
            {
                HashSetLong hashSetLong = prefab.GetOccupiedChunks();
                ChunkCluster chunkCluster = world.ChunkClusters[0];

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
                                    door.SetLocked((door.blockValue.meta & 4) > 0);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Makes world position user frienrly in N/S/W/E coordinates + height.
    /// </summary>
    private static string ToCompasPos(Vector3i p)
    {
        return (Math.Abs(p.x).ToString() + (p.x > 0 ? "E" : "W")) + ", " + (Math.Abs(p.z).ToString() + (p.z > 0 ? "N" : "S")) + ", " + p.y.ToString() + "h";
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
    /*
    [HarmonyPatch(typeof(TileEntitySecure))]
    [HarmonyPatch("LocalPlayerIsOwner")]
    public class TileEntitySecure_IsUserAllowed
    {
        public static void Postfix(TileEntitySecure __instance, ref bool __result)
        {
            if (__instance is TileEntitySecureDoor)
            {
                __result = true;
            }
        }
    }
    */
}
