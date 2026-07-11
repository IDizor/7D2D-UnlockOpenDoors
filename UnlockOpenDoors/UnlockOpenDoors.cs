using System.Reflection;
using HarmonyLib;

namespace UnlockOpenDoors
{
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

        /// <summary>
        /// Makes locked open doors/hatches/gates unlocked.
        /// </summary>
        [HarmonyPatch(typeof(TEFeatureLockable), nameof(TEFeatureLockable.SetLocked))]
        public static class TEFeatureLockable_SetLocked
        {
            public static void Prefix(TEFeatureLockable __instance, ref bool _isLocked)
            {
                if (_isLocked && __instance.GetOwner()== null)
                {
                    if (__instance.Parent.TryGetSelfOrFeature<TEFeatureDoor>(out var door) && door.IsOpen())
                    {
                        //Debug.LogError($"(SetLocked) Annoying door: {__instance.blockValue.Block.blockName}, Position: {Helper.ToCompasPos(__instance.ToWorldPos())}");
                        _isLocked = false;
                    }
                }
            }
        }

        /// <summary>
        /// Makes locked open doors/hatches/gates unlocked (for already discovered doors).
        /// </summary>
        [HarmonyPatch(typeof(TileEntity), nameof(TileEntity.OnReadComplete))]
        public static class TileEntity_OnReadComplete
        {
            public static void Postfix(TileEntity __instance)
            {
                if (__instance.TryGetSelfOrFeature<TEFeatureDoor>(out var door) && door.IsOpen() && door.lockFeature != null && door.lockFeature.IsLocked() && door.lockFeature.GetOwner() == null)
                {
                    //Debug.LogErrorFormat($"(OnReadComplete) Annoying door: {__instance.blockValue.Block.blockName}, Position: {Helper.ToCompasPos(door.ToWorldPos())}");
                    door.lockFeature.SetLocked(false);
                }
            }
        }

        /// <summary>
        /// Unlocks door/hatch/gates opened by a key or switch.
        /// </summary>
        [HarmonyPatch(typeof(TEFeatureLockable), nameof(TEFeatureLockable.OnBlockTriggered))]
        public static class TEFeatureLockablee_OnBlockTriggered
        {
            public static void Postfix(TEFeatureLockable __instance, BlockValue _blockValue)
            {
                if (__instance.IsLocked() && __instance.GetOwner() == null && __instance.Parent.TryGetSelfOrFeature<TEFeatureDoor>(out var door) && door.IsOpen())
                {
                    //Debug.LogErrorFormat($"(OnBlockTriggered) Annoying door: {__instance.blockValue.Block.blockName}, Position: {Helper.ToCompasPos(__instance.ToWorldPos())}");
                    __instance.SetLocked(false);
                }
            }
        }

        /// <summary>
        /// Fix locked state once Prefab created.
        /// Then the correct locked states will be used everywhere.
        /// </summary>
        [HarmonyPatch(typeof(Prefab))]
        [HarmonyPatch(methodType: MethodType.Constructor)]
        [HarmonyPatch([typeof(Prefab), typeof(bool)])]
        public static class Prefab_ctor
        {
            public static void Postfix(Prefab __instance)
            {
                foreach (var te in __instance.tileEntities)
                {
                    if (te.Value is TileEntityComposite composite)
                    {
                        if (composite.TryGetSelfOrFeature(out TEFeatureDoor door) && door.IsOpen() && door.lockFeature != null && door.lockFeature.IsLocked())
                        {
                            door.lockFeature.SetLocked(false);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Prefab), nameof(Prefab.readTileEntities))]
        public static class Prefab_readTileEntities
        {
            public static void Postfix(Prefab __instance) => Prefab_ctor.Postfix(__instance);
        }
    }
}
