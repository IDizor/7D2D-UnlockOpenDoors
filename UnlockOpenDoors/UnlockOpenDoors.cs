using System;
using System.Reflection;
using HarmonyLib;
using UniLinq;
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
        public static void Prefix(TileEntitySecure __instance, ref bool _isLocked)
        {
            if (_isLocked && __instance is TileEntitySecureDoor && __instance.GetOwner() == null && BlockDoor.IsDoorOpen(__instance.blockValue.meta))
            {
                //Debug.LogError($"(SetLocked) Annoying door: {__instance.blockValue.Block?.GetBlockName()}, Position: {ToCompasPos(__instance.ToWorldPos())}");
                _isLocked = false;
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
    /// Unlocks door/hatch/gates opened by a key or switch.
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
    /// Restores the initial locked state for closed doors during TileEntity/POI reset.
    /// </summary>
    [HarmonyPatch(typeof(TileEntityLootContainer))]
    [HarmonyPatch("Reset")]
    public class TileEntityLootContainer_Reset
    {
        public static void Postfix(TileEntityLootContainer __instance)
        {
            if (__instance is TileEntitySecureDoor door && door.GetOwner() == null && !BlockDoor.IsDoorOpen(door.blockValue.meta))
            {
                door.SetLocked((door.blockValue.meta & 4) > 0);
            }
        }
    }

    private static string ToCompasPos(Vector3i p)
    {
        return (Math.Abs(p.x).ToString() + (p.x > 0 ? "E" : "W")) + ", " + (Math.Abs(p.z).ToString() + (p.z > 0 ? "N" : "S")) + ", " + p.y.ToString() + "h";
    }

    private static string GetCallStackPath(int limit = 5)
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var path = string.Join(" <-- ", stackTrace.GetFrames()
            .Skip(3)
            .Take(limit)
            .Select(f => f.GetMethod())
            .Select(m => m.DeclaringType.Name + "." + m.Name + "()"));
        return path;
    }
}
