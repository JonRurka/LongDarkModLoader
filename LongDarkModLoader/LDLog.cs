// -----------------------------------------------------------------------
// <copyright file="LDLog.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace LongDarkModLoader {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class LDLog {
        public static void Log(object message) {
            Debug.Log("LDModLoader: " + message.ToString());
            LDConsole.Log("LDModLoader: " + message.ToString());
        }

        public static void LogWarning(object message) {
            Debug.Log("LongDarkModLoader Warning: " + message.ToString());
            LDConsole.Log("LongDarkModLoader Warning: " + message.ToString());
        }

        public static void LogError(object message) {
            Debug.Log("LDModLoader error: " + message.ToString());
            LDConsole.Log("LDModLoader error: " + message.ToString());
        }

        public static void LogError(Exception e) {
            Debug.Log("LDModLoader error: message: " + e.Message + ",\nSource: " + e.Source + ",\nStackTrace: " + e.StackTrace);
            LDConsole.Log("LDModLoader error: message: " + e.Message + ",\nSource: " + e.Source);
        }
    }
}
