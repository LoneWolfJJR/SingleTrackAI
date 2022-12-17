using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlgernonCommons;
using AlgernonCommons.Patching;
using ColossalFramework.Plugins;
using ICities;
using JetBrains.Annotations;
using SingleTrackAI.UI;

namespace SingleTrackAI
{
    [PublicAPI]
    public sealed class Mod : PatcherMod<OptionsPanel, Patcher>, IUserMod
    {
        /// <summary>
        /// Gets the mod's base display name (name only).
        /// </summary>
        public override string BaseName => "SingleTrainTrackAI";

        /// <summary>
        /// Gets the mod's unique Harmony identifier.
        /// </summary>
        public override string HarmonyID => "johnrambo.singletrackai";

        /// <summary>
        /// Gets the mod's description for display in the content manager.
        /// </summary>
        public string Description => "Train AI for two-way single tracks";

        /// <summary>
        /// Saves settings file.
        /// </summary>
        public override void SaveSettings() => Settings.Save();

        /// <summary>
        /// Loads settings file.
        /// </summary>
        public override void LoadSettings() => Settings.Load();

        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public override void OnEnabled()
        {
            var conflicts = DetermineConflictingMods();
            if (conflicts.Length != 0)
            {
                Logging.Error($"Conflicting mods found:\n{String.Join("\n", conflicts)}");

                var plugin = AssemblyUtils.ThisPlugin;
                if (plugin != null)
                {
                    Logger.Error("Mod disabled due to conflicting mods.");
                    plugin.isEnabled = false;
                }

                NotifyOfConflictingMods(conflicts);

                // Don't do anything further.
                return;
            }

            base.OnEnabled();
        }

        private static string[] DetermineConflictingMods()
        {
            var conflicts = new List<string>();

            var currentMod = Assembly.GetExecutingAssembly().GetName();

            var assemblies = from plugin in PluginManager.instance.GetPluginsInfo()
                             where plugin.isEnabled
                             from assembly in plugin.GetAssemblies()
                             let name = assembly.GetName()
                             where name != currentMod
                             select name;

            foreach (var assembly in assemblies)
            {
                switch (assembly.Name)
                {
                    case "SingleTrackAI":
                        if (assembly.Version < new Version("2.0.0.0"))
                            conflicts.Add($"SingleTrainTrackAI {assembly.Version}");

                        break;
                }
            }

            return conflicts.Distinct().OrderBy(name => name).ToArray();
        }

        private void NotifyOfConflictingMods(string[] conflicts)
        {
            var modConflictBox = MessageBoxBase.ShowModal<ListMessageBox>();

            modConflictBox.AddParas("Mod conflict detected!");
            modConflictBox.AddParas($"{Name} detected a conflict with at least one other mod.");
            modConflictBox.AddParas("This means that the mod is not able to operate and has shut down.");

            modConflictBox.AddParas("The conflicting mods are:");
            modConflictBox.AddList(conflicts);

            modConflictBox.AddParas($"These mods must be disabled or unsubscribed before {Name} can operate.");
        }
    }
}
