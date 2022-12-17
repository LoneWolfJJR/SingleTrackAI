using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace SingleTrackAI
{
    internal static class Logger
    {
        public static void Debug(string message) => LogMessage("[DEBUG] " + message);

        public static void Info(string message) => LogMessage(message);

        public static void Error([NotNull] string message) => LogError(null, message);
        public static void Error([NotNull] Exception exception) => LogError(exception, null);
        public static void Error([NotNull] Exception exception, [CanBeNull] string message) => LogError(exception, message);

        private static void LogError([CanBeNull] Exception exception, [CanBeNull] string message)
        {
            if (exception == null && String.IsNullOrEmpty(message))
                return; // WTF! Oh well, nothing to log ¯\_(ツ)_/¯

            var builder = new StringBuilder($"{Mod.Instance.Name}: ");

            if (message != null)
                builder.AppendLine(message);

            if (exception != null)
            {
                builder.AppendLine("Exception: ");
                builder.AppendLine(exception.Message);
                builder.AppendLine(exception.Source);
                builder.AppendLine(exception.StackTrace);

                while (exception.InnerException != null)
                {
                    builder.AppendLine("Inner exception:");
                    builder.AppendLine(exception.InnerException.Message);
                    builder.AppendLine(exception.InnerException.Source);
                    builder.AppendLine(exception.InnerException.StackTrace);

                    exception = exception.InnerException;
                }
            }

            UnityEngine.Debug.Log(builder);
        }

        private static void LogMessage([NotNull] string message)
        {
            if (String.IsNullOrEmpty(message))
                return; // WTF! Oh well, nothing to log ¯\_(ツ)_/¯\

            UnityEngine.Debug.Log($"{Mod.Instance.Name}: {message}");
        }
    }
}