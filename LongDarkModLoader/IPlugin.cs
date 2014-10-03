// -----------------------------------------------------------------------
// <copyright file="IPlugin.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace LongDarkModLoader {
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IPlugin {
        string Name { get; }

        string Version { get; }

        void Init(Loader core);

        void Stop();
    }
}
