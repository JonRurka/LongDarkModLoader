// -----------------------------------------------------------------------
// <copyright file="Monitor.cs" company="">
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
    public class LDMonitor : MonoBehaviour {

        void Awake() {
            DontDestroyOnLoad(this);
            LDLog.Log(string.Format("Scene: {0}, {1}", Application.loadedLevel, Application.loadedLevelName));
            LDConsole.Execute("ListObjects -t -c -w");

        }

        void Start() {

        }

        void Update() {

        }

        void OnLevelWasLoaded(int level) {
            LDLog.Log(string.Format("new scene: {0}, {1}", level, Application.loadedLevelName));
            LDConsole.Execute("ListObjects -t -c -w");
        }
    }
}
