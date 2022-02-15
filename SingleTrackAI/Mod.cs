using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CitiesHarmony.API;
using ColossalFramework.Plugins;
using ICities;
using JetBrains.Annotations;

namespace SingleTrackAI
{
    [PublicAPI]
    public sealed class Mod : IUserMod
    {
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        private static readonly AssemblyName AssemblyName = Assembly.GetName();

        public static string Name => "SingleTrainTrackAI";
        public static string Description => "Train AI for two-way single tracks";
        public static string Version => AssemblyName.Version.ToString(2);

        string IUserMod.Name => Name;
        string IUserMod.Description => Description;

        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(Patcher.Apply);

            Settings.Initialize();
        }

        /// <summary>
        /// Called by the game when the mod is disabled.
        /// </summary>
        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
                Patcher.Revert();
        }

        internal static string[] DetermineConflictingMods()
        {
            var conflicts = new HashSet<string>();

            var assemblies = from plugin in PluginManager.instance.GetPluginsInfo()
                             where plugin.isEnabled
                             from assembly in plugin.GetAssemblies()
                             select assembly.GetName();

            foreach (var assembly in assemblies)
            {
                switch (assembly.Name)
                {
                    case "SingleTrackAI":
                        if (assembly == AssemblyName)
                            break; // It's us! :)

                        if (assembly.Version < new Version("2.0.0.0"))
                            conflicts.Add($"SingleTrainTrackAI {assembly.Version}");

                        break;
                }
            }

            return conflicts.OrderBy(name => name)
                            .ToArray();
        }
    }
}

