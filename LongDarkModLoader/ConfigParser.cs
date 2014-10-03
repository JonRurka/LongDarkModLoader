// -----------------------------------------------------------------------
// <copyright file="ConfigParser.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace LongDarkModLoader {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using IniParser;
    using IniParser.Parser;
    using IniParser.Model;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class ConfigParser {
        public static string Extension = ".ini";

        public static LDConsole.CommandDescription[] GetCommands(string plugin) {
            List<LDConsole.CommandDescription> commands = new List<LDConsole.CommandDescription>();
            FileIniDataParser parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            IniData config = parser.ReadFile(Loader.GetModFolder(plugin) + plugin + "_commands" + Extension);
            foreach (SectionData section in config.Sections) {
                string command = section.SectionName;
                string command_args = string.Empty;
                string description_small = string.Empty;
                string description_Long = string.Empty;
                string callback = string.Empty;

                if (config[command].ContainsKey("command_args")) {
                    command_args = config[command]["command_args"];
                }

                if (config[command].ContainsKey("description_small")) {
                    description_small = config[command]["description_small"];
                }
                else {
                    LDLog.LogError("Failed to parse Command \"" + command + "\": Short description not specified.");
                }

                if (config[command].ContainsKey("description_Long")) {
                    description_Long = config[command]["description_Long"];
                }

                if (config[command].ContainsKey("function")) {
                    callback = config[command]["function"];
                }
                else{
                    LDLog.LogError("Failed to parse Command \"" + command + "\": Function not specified.");
                }

                commands.Add(new LDConsole.CommandDescription(plugin, command, command_args, description_small, description_Long, callback));
            }
            return commands.ToArray();
        }

        public static IniData GetConfig(string plugin) {
            FileIniDataParser parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            return parser.ReadFile(Loader.GetModFolder(plugin) + plugin + Extension);
        }

        public static string GetValue(string plugin, string section, string key) {
            IniData config = GetConfig(plugin);
            if (config.Sections.ContainsSection(section) && config[section].ContainsKey(key)) {
                return config[section][key];
            }
            else return string.Empty;
        }

        public static string GetValue(IniData config, string section, string key) {
            if (config.Sections.ContainsSection(section) && config[section].ContainsKey(key)) {
                return config[section][key];
            }
            else return string.Empty;
        }
    }
}
