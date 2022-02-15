using System;
using System.Collections.Generic;
using System.Linq;
using CitiesHarmony.API;
using HarmonyLib;

namespace SingleTrackAI
{
    internal static class Patcher
    {
        private const string HarmonyId = "johnrambo.singletrackai";

        private static Harmony Harmony => new Harmony(HarmonyId);

        public static bool Patched
        {
            get;
            private set;
        }

        public static void Apply()
        {
            if (Patched)
                return;

            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Logger.Debug("Applying Harmony patches...");

                Harmony.PatchAll();

                Logger.Debug("Harmony patches applied!");

                Patched = true;
            }
            else
            {
                Logger.Error("Harmony not installed!");
            }
        }


        public static void Revert()
        {
            if (Patched)
            {
                Logger.Debug("Reverting Harmony patches...");

                Harmony.UnpatchAll(HarmonyId);

                Logger.Debug("Harmony patches reverted!");

                Patched = false;
            }
        }
    }
}
