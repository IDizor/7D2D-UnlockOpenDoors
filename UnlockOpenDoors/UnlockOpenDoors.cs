using HarmonyLib;
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
    /// The Harmony patch for the <see cref="TileEntity.OnReadComplete"/> method.
    /// </summary>
    [HarmonyPatch(typeof(TileEntity))]
    [HarmonyPatch("OnReadComplete")]
    public class TileEntity_OnReadComplete
    {
        /// <summary>
        /// The additional code to execute after the original <see cref="TileEntity.OnReadComplete"/> method.
        /// </summary>
        /// <param name="__instance"></param>
        public static void Postfix(TileEntity __instance)
        {
            if (__instance is TileEntitySecureDoor door && door.GetOwner() == null)
            {
                if (door.IsLocked() && BlockDoor.IsDoorOpen(door.blockValue.meta))
                {
                    //Debug.LogErrorFormat($"Annoying door: {door.blockValue.Block?.GetBlockName()}, {door.ToWorldPos()}");
                    door.SetLocked(false);
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
