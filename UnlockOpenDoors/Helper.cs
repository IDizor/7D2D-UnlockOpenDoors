using System;
using System.Collections;
using System.Text.RegularExpressions;
using UniLinq;
using UnityEngine;

namespace UnlockOpenDoors
{
    public static class Helper
    {
        /// <summary>
        /// Gets call stack methods order.
        /// </summary>
        public static string GetCallStackPath(int limit = 5)
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var path = string.Join(" <- ", stackTrace.GetFrames()
                .Skip(4)
                .Take(limit)
                .Select(f => f.GetMethod())
                .Select(m =>
                {
                    var match = Regex.Match(m.Name, @".*?\s(\w+:\w+)\(.+");
                    if (match.Success)
                    {
                        return $"{match.Groups[1].Value}()";
                    }
                    return $"{m.DeclaringType.Name}.{m.Name}()";
                }));

            return path;
        }

        public static string ToCompasPos(Vector3i p)
        {
            return (Math.Abs(p.x).ToString() + (p.x > 0 ? "E" : "W")) + ", " + (Math.Abs(p.z).ToString() + (p.z > 0 ? "N" : "S")) + ", " + p.y.ToString() + "h";
        }

        public static bool IsLockableDoor(Block block)
        {
            var composite = block.Properties.GetClass("CompositeFeatures");
            return composite != null && composite.Classes.ContainsKey(nameof(TEFeatureDoor)) && composite.Classes.ContainsKey(nameof(TEFeatureLockable));
        }

        public static bool IsDoorVisuallyOpen(byte blockValue_meta)
        {
            return (blockValue_meta & 1) != 0;
        }

        public static bool IsDoorLocked(byte blockValue_meta)
        {
            return (blockValue_meta & 4) != 0;
        }
    }
}
