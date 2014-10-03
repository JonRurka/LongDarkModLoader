using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using IniParser;
using IniParser.Parser;
using IniParser.Model;
using UnityEngine;

namespace LongDarkModLoader {
    public class Loader {
        static Loader instance;
        static string modFolder = string.Empty;
        static ICollection<IPlugin> plugins;
        static Dictionary<string, string> assemblyQualifiedNames;
        static Dictionary<string, string> modDirectories;
        static Dictionary<string, IPlugin> name_plugin;
        static Dictionary<string, List<LDConsole.CommandDescription>> plugin_Commands;

        private bool AutoLoad = true;
        private bool LoadCommands = true;

        /// <summary>
        /// Returns the loader instance.
        /// </summary>
        public static Loader Instance {
            get {
                if (instance == null) {
                    LDLog.LogError("loader is null.");
                }
                return instance;
            }
        }

        /// <summary>
        /// Returns a list of all plugins.
        /// </summary>
        public static ICollection<IPlugin> Plugins {
            get { return plugins; }
        }

        /// <summary>
        /// Returns the name of the loader.
        /// </summary>
        public static string Name {
            get { return "LongDarkModLoader"; }
        }

        /// <summary>
        /// Returns the current version of the loader.
        /// </summary>
        public static string Version {
            get;
            private set;
        }

        /// <summary>
        /// The folder config files, models and plugins are put into.
        /// </summary>
        public static string ModFolder
        {
            get { return modFolder; }
        }

        public Loader() {
            try {
                instance = this;
                modFolder = Environment.CurrentDirectory + @"\tld_Data\mods\";
                RefreshDataContainers();
                LoadConfig();
                LDLog.Log("Started.");
                if (AutoLoad) 
                    StartPlugins();
                else
                    LDLog.Log("Automatic plugin loading disabled.");
                if (LoadCommands) 
                    LoadCommandsFromPlugins();
                else
                    LDLog.Log("Plugin commands disabled.");
                new GameObject("_LDmonitor").AddComponent<LDMonitor>();
                
            }
            catch (Exception e) {
                LDLog.LogError("Failure initializing Loader.");
                LDLog.LogError(e);
            }
        }

        public void LoadConfig() {
            IniData config = ConfigParser.GetConfig(Name);
            Version        =              ConfigParser.GetValue(config, "General", "Version");
            AutoLoad       =  bool.Parse( ConfigParser.GetValue(config, "Debug"  , "AutoLoad"));
            LoadCommands   =  bool.Parse( ConfigParser.GetValue(config, "Debug"  , "LoadCommands"));
        }

        public void RefreshDataContainers() {
            name_plugin = new Dictionary<string, IPlugin>();
            assemblyQualifiedNames = new Dictionary<string, string>();
            modDirectories = new Dictionary<string, string>();
            plugin_Commands = new Dictionary<string, List<LDConsole.CommandDescription>>();
            plugins = new List<IPlugin>();
            modDirectories.Add(Name, modFolder + Name + @"\");
            assemblyQualifiedNames.Add(Name, Assembly.GetCallingAssembly().FullName.ToString());
            LoadCommandsFromPlugin(Name);
        }

        public void StartPlugins() {
            try {
                // search for dll files
                LDLog.Log("searching for dll files");
                List<string> dllFileNames = new List<string>();
                List<string> modsDirectories = new List<string>();
                if (Directory.Exists(modFolder)) {
                    modsDirectories.AddRange(Directory.GetDirectories(modFolder));
                    foreach (string modDir in modsDirectories) {
                        string[] dllModFiles = Directory.GetFiles(modDir, "*.dll");
                        if (dllModFiles.Length > 0) {
                            foreach (string dllModFile in dllModFiles) {
                                dllFileNames.Add(dllModFile);
                                modDirectories.Add(new DirectoryInfo(dllModFile).Name, modDir + @"\");
                            }
                        }
                    }
                    LoadPluginsFromFiles(dllFileNames);
                }
                else {
                    LDLog.LogError("failed to locate mods folder.\nPath: " + modFolder);
                    return;
                }
            }
            catch(Exception e){
                LDLog.LogError(e);
                LDLog.LogError("Failed loading plugins.");
            }
        }
        
        public void LoadCommandsFromPlugins() {
            foreach (IPlugin plugin in plugins) {
                if (plugin != null) {
                    try {
                        LoadCommandsFromPlugin(plugin.Name);
                    }
                    catch (Exception e) {
                        LDLog.LogError("Failure loading commands for " + plugin.Name);
                        LDLog.LogError(e);
                    }
                }
            }
        }

        public void LoadCommandsFromPlugin(string plugin) {
            LDConsole.CommandDescription[] commands = ConfigParser.GetCommands(plugin);
            foreach (LDConsole.CommandDescription cmd in commands) {
                RegisterCommand(cmd);
            }
        }

        public void UnloadPluginCommands(string plugin) {
            if (plugin_Commands.ContainsKey(plugin)) {
                LDConsole.CommandDescription[] Commands = plugin_Commands[plugin].ToArray();
                foreach (LDConsole.CommandDescription cmd in Commands) {
                    LDConsole.UnRegisterCommand(cmd.command);
                }
                plugin_Commands[plugin].Clear();
                plugin_Commands.Remove(plugin);
            }
        }

        public object LoadCmd(params string[] args) {
            string result = string.Empty;
            if (args.Length > 1) {
                for (int i = 1; i < args.Length; i++) {
                    LoadPlugin(args[i]);
                    result += args[1];
                    if (i < args.Length - 1) {
                        result += ",";
                    }
                }
            }
            return "loaded " + result;
        }

        public object UnloadCmd(params string[] args) {
            string result = string.Empty;
            if (args.Length > 1) {
                for (int i = 1; i < args.Length; i++) {
                    UnloadPlugin(args[i]);
                    result += args[i];
                    if (i < args.Length - 1) {
                        result += ", ";
                    }
                }
            }
            return "Loaded " + result;
        }

        public object ReloadCmd(params string[] args) {
            string result = string.Empty;
            if (args.Length > 1) {
                for (int i = 1; i < args.Length; i++) {
                    UnloadPlugin(args[i]);
                    LoadPlugin(args[i]);
                    result = args[i];
                    if (i < args.Length - 1) {
                        result += ", ";
                    }
                }
            }
            else if (args.Length == 1) {
                foreach (string plugin in name_plugin.Keys) {
                    UnloadPlugin(plugin);
                    LoadPlugin(plugin);
                }
                result = "all plugins";
            }

            return "Reloaded " + result;
        }

        public object PluginsCmd(params string[] args) {
            var output = new StringBuilder();
            if (args.Length == 1) {
                output.AppendLine("::Plugins::");
                foreach (string plugin in name_plugin.Keys) {
                    output.AppendLine("-" + plugin);
                }
            }
            output.AppendLine("");
            return output.ToString();
        }

        public object ListObjectsCmd(params string[] args) {
            bool listChildren = false;
            bool writeToFile = false;
            bool listComponents = false;
            for (int i = 0; i < args.Length; i++) {
                switch (args[i].ToLower()) {
                    case "-t":
                        listChildren = true;
                        break;

                    case "-w":
                        writeToFile = true;
                        break;

                    case "-c":
                        listComponents = true;
                        break;
                }
            }
            StringBuilder output = ListObjects(listChildren, listComponents);
            if (writeToFile) {
                string file = Application.dataPath + @"/gameObjects.txt";
                StreamWriter writer = File.CreateText(file);
                writer.Write(output.ToString());
                writer.Close();
                output.AppendLine("results written to gameobjects.txt.");
            }

            return output.ToString();
        }

        public object ObjectInfoCmd(params string[] args) {
            StringBuilder builder = new StringBuilder();
            if (args.Length > 1) {
                Transform obj = GameObject.Find(args[1]).transform;
                bool listComponents = false;
                if (obj != null) {
                    builder.AppendLine("- " + obj.name);
                    if (args.Length == 3 && args[2].ToLower() == "-c")
                        listComponents = true;
                    if (listComponents) {
                        ListComponents("", ref builder, obj);
                    }
                    GetChildren(1, ref builder, obj, listComponents);
                    builder.AppendLine("");
                }
                else {
                    builder.AppendLine("Object not found.");
                }
            }
            else {
                builder.AppendLine("Please specify an object.");
            }
            return builder.ToString();
        }

        public static void Init() {
            if (instance == null) {
                new Loader();
            }
            else {
                LDLog.LogError("Only one Loader instance allowed.");
            }
        }

        public static void RegisterCommand(LDConsole.CommandDescription cmd) {
            if (cmd.callback != null) {
                LDConsole.RegisterCommand(cmd);
                if (plugin_Commands.ContainsKey(cmd.plugin.ToLower())) {
                    plugin_Commands[cmd.plugin.ToLower()].Add(cmd);
                }
                else {
                    plugin_Commands.Add(cmd.plugin.ToLower(), new List<LDConsole.CommandDescription>());
                    plugin_Commands[cmd.plugin.ToLower()].Add(cmd);
                }
            }
            else {
                LDLog.LogError(string.Format("Command callback is null for \"{0}\".", cmd.command));
            }
        }

        public static IPlugin GetPlugin(string modName) {
            if (name_plugin.ContainsKey(modName)) {
                return name_plugin[modName];
            }
            return null;
        }

        public static string GetAssemblyName(string modName) {
            if (assemblyQualifiedNames.ContainsKey(modName)) {
                return assemblyQualifiedNames[modName];
            }
            return string.Empty;
        }

        public static string GetModFolder(string modName) {
            if (modDirectories.ContainsKey(modName)) {
                return modDirectories[modName];
            }
            return string.Empty;
        }

        public static bool PluginExists(string modName) {
            return name_plugin.ContainsKey(modName);
        }

        public static bool HasCommands(string modName) {
            return plugin_Commands.ContainsKey(modName);
        }

        public static LDConsole.CommandDescription[] GetCommands(string modName) {
            if (HasCommands(modName)) {
                return plugin_Commands[modName].ToArray();
            }
            return null;
        }

        public static object GetInstance(string modName) {
            if (modName.Equals(Name)) {
                return instance;
            }
            else if (name_plugin.ContainsKey(modName)) {
                return (object)name_plugin[modName];
            }
            else {
                return null; 
            }
        }

        private StringBuilder ListObjects(bool ListChildren, bool listComponents) {
            StringBuilder builder = new StringBuilder();
            Transform[] objects = GameObject.FindObjectsOfType<Transform>();
            builder.AppendLine("::GameObjects::");
            foreach (Transform obj in objects) {
                if (obj.transform.parent == null) {
                    builder.AppendLine("- " + obj.name);
                    if (listComponents) {
                        ListComponents("", ref builder, obj);
                    }
                    if (ListChildren) {
                        GetChildren(1, ref builder, obj, listComponents);
                    }

                    if (listComponents || ListChildren) {
                        builder.AppendLine("");
                    }
                }
            }
            return builder;
        }

        private void GetChildren(int depth, ref StringBuilder builder, Transform obj, bool listComponents) { 
            int childCount = obj.childCount;
            string tab = string.Empty;
            for (int t = 0; t < depth; t++) {
                tab += '\t';
            }

            for (int i = 0; i < childCount; i++) {
                builder.AppendLine(tab + "- " + obj.name);
                if (listComponents)
                    ListComponents(tab, ref builder, obj);
                GetChildren(depth + 1, ref builder, obj.GetChild(i), listComponents);
            }
        } // recursive function.

        private void ListComponents(string tab, ref StringBuilder builder, Transform obj) {
            Component[] components = obj.GetComponents(typeof(Component));
            foreach (Component comp in components) {
                if (comp is Transform)
                    continue;
                builder.AppendLine(string.Format("{0}  [{1}]", tab, comp.GetType().Name));
            }
        }

        private void LoadPlugin(string name) {
            string modDir = modFolder + name + @"\";
            if (!modDirectories.ContainsKey(name)) {
                modDirectories.Add(name, modDir);
            }

            string[] modDllFile = Directory.GetFiles(modDir, "*.dll");
            LoadPluginsFromFiles(new List<string>(modDllFile));
            if (LoadCommands) {
                LoadCommandsFromPlugins();
            }
        }

        private void UnloadPlugin(string name) {
            if (name_plugin.ContainsKey(name)) {
                IPlugin _instance = name_plugin[name];
                _instance.Stop();
                UnloadPluginCommands(name);
                assemblyQualifiedNames.Remove(name);
                modDirectories.Remove(name);
                name_plugin.Remove(name);
                plugins.Remove(_instance);
                _instance = null;
            }
        }

        private void LoadPluginsFromFiles(List<string> dllFileNames) {
            // load assemblies
            LDLog.Log("Loading assemblies.");
            ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Count);
            foreach (string dllFile in dllFileNames) {
                AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                Assembly assembly = Assembly.Load(an);
                assemblies.Add(assembly);
            }

            if (assemblies.Count > 0) {
                // search for types that implement IPlugin
                LDLog.Log("searching for types that implement IPlugin.");
                Type pluginType = typeof(IPlugin);
                ICollection<Type> pluginTypes = new List<Type>();
                foreach (Assembly assembly in assemblies) {
                    if (assembly != null) {
                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types) {
                            if (type.IsInterface || type.IsAbstract) {
                                continue;
                            }
                            else {
                                if (type.GetInterface(pluginType.FullName) != null) {
                                    pluginTypes.Add(type);
                                }
                            }
                        }
                    }
                }

                // create instances from types.
                LDLog.Log("creating instances from types.");
                plugins = new List<IPlugin>(pluginTypes.Count);
                foreach (Type type in pluginTypes) {
                    IPlugin plugin = (IPlugin)System.Activator.CreateInstance(type);
                    if (plugin.Name != string.Empty || modDirectories.ContainsKey(plugin.Name)) {
                        plugin.Init(this);
                        plugins.Add(plugin);
                        name_plugin.Add(plugin.Name, plugin);
                        assemblyQualifiedNames.Add(plugin.Name, plugin.GetType().AssemblyQualifiedName);
                    }
                    else if (plugin.Name == string.Empty) {
                        LDLog.LogError("Failed loading assembly \"" + plugin.GetType().AssemblyQualifiedName + "\": Plugin name is empty.");
                    }
                    else {
                        LDLog.LogError("Failed loading assembly \"" + plugin.GetType().AssemblyQualifiedName + "\": Plugins name does not match folder name.");
                    }
                }

                LDLog.Log("Loaded " + plugins.Count + " plugins.");

            }
            else {
                LDLog.Log("No combatible assemblies found.");
            }
        }
    }
}
