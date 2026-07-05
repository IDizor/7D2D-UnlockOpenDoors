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
    public void InitMod(Mod _modInstance)
    {
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public static bool IsDoorOpen(byte _metadata)
    {
        return (_metadata & 1) != 0;
    }

    /// <summary>
    /// Makes locked open doors/hatches/gates unlocked.
    /// </summary>
    [HarmonyPatch(typeof(TEFeatureLockable))]
    [HarmonyPatch(nameof(TEFeatureLockable.SetLocked))]
    public static class TEFeatureLockable_SetLocked
    {
        public static void Prefix(TEFeatureLockable __instance, ref bool _isLocked)
        {
            if (_isLocked)
            {
                if (__instance.TryGetSelfOrFeature<TEFeatureDoor>(out var _) && IsDoorOpen(__instance.blockValue.meta))
                {
                    //Debug.LogError($"(SetLocked) Annoying door: {__instance.blockValue.Block.GetBlockName()}, Position: {ToCompasPos(__instance.ToWorldPos())}");
                    _isLocked = false;
                }
            }
        }
    }

    /// <summary>
    /// Makes locked open doors/hatches/gates unlocked (for already discovered doors).
    /// </summary>
    [HarmonyPatch(typeof(TileEntity))]
    [HarmonyPatch(nameof(TileEntity.OnReadComplete))]
    public static class TileEntity_OnReadComplete
    {
        public static void Postfix(TileEntity __instance)
        {
            if (__instance.TryGetSelfOrFeature<TEFeatureDoor>(out var door) && door.lockFeature != null && door.lockFeature.IsLocked())
            {
                if (IsDoorOpen(__instance.blockValue.meta) && door.lockFeature.GetOwner() == null)
                {
                    //Debug.LogErrorFormat($"(OnReadComplete) Annoying door: {__instance.blockValue.Block.GetBlockName()}, Position: {ToCompasPos(door.ToWorldPos())}");
                    door.lockFeature.SetLocked(false);
                }
            }
        }
    }

    /// <summary>
    /// Unlocks door/hatch/gates opened by a key or switch.
    /// </summary>
    [HarmonyPatch(typeof(TEFeatureLockable))]
    [HarmonyPatch(nameof(TEFeatureLockable.OnBlockTriggered))]
    public static class TEFeatureLockablee_OnBlockTriggered
    {
        public static void Postfix(TEFeatureLockable __instance, BlockValue _blockValue)
        {
            if (IsDoorOpen(_blockValue.meta) && __instance.TryGetSelfOrFeature<TEFeatureDoor>(out var _))
            {
                if (__instance.IsLocked() && __instance.GetOwner() == null)
                {
                    //Debug.LogErrorFormat($"(OnBlockTriggered) Annoying door: {__instance.blockValue.Block.GetBlockName()}, Position: {ToCompasPos(__instance.ToWorldPos())}");
                    __instance.SetLocked(false);
                }
            }
        }
    }

    /// <summary>
    /// Restores the initial locked state for closed doors during TileEntity/POI reset.
    /// </summary>
    [HarmonyPatch(typeof(TEFeatureDoor))]
    [HarmonyPatch(nameof(TEFeatureDoor.OnBlockReset))]
    public static class TEFeatureDoor_OnBlockReset
    {
        public static void Postfix(TEFeatureDoor __instance)
        {
            if (__instance.lockFeature != null && __instance.lockFeature.GetOwner() == null && !IsDoorOpen(__instance.blockValue.meta))
            {
                //Debug.LogErrorFormat($"(OnBlockReset) Annoying door: {__instance.blockValue.Block.GetBlockName()}, Position: {ToCompasPos(__instance.ToWorldPos())}");
                __instance.lockFeature.SetLocked((__instance.blockValue.meta & 4) > 0);
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
