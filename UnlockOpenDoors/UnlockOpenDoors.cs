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
    /// The Harmony patch for the method <see cref="TileEntitySecure.SetLocked"/>.
    /// </summary>
    [HarmonyPatch(typeof(TileEntitySecure))]
    [HarmonyPatch("SetLocked")]
    public class TileEntitySecure_SetLocked
    {
        /// <summary>
        /// The additional code to execute after the original method <see cref="TileEntitySecure.SetLocked"/>.
        /// Makes locked open doors/hatches/gates unlocked.
        /// </summary>
        public static void Postfix(TileEntitySecure __instance)
        {
            if (__instance.IsLocked() && __instance is TileEntitySecureDoor && __instance.GetOwner() == null && BlockDoor.IsDoorOpen(__instance.blockValue.meta))
            {
                //Debug.LogErrorFormat($"Annoying door: {__instance.blockValue.Block?.GetBlockName()}, {__instance.ToWorldPos()}");
                __instance.SetLocked(false);
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
    /// The Harmony patch for the method <see cref="QuestEventManager.QuestLockPOI"/>.
    /// </summary>
    [HarmonyPatch(typeof(QuestEventManager))]
    [HarmonyPatch("QuestLockPOI")]
    public class QuestEventManager_QuestLockPOI
    {
        /// <summary>
        /// The additional code to execute after the original method <see cref="QuestEventManager.QuestLockPOI"/>.
        /// Restores the initial locked state for previously opened doors with a special key or switch.
        /// </summary>
        public static void Postfix(Vector3 prefabPos, QuestTags questTags)
        {
            var world = GameManager.Instance.World;
            var prefabs = GameManager.Instance
                .GetDynamicPrefabDecorator()
                .GetPrefabsFromWorldPosInside((int)prefabPos.x, (int)prefabPos.y, (int)prefabPos.z, questTags);

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
