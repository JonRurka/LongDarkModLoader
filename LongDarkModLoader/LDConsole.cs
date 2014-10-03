#define DEBUG_CONSOLE
#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
 
 
// V.M10.D31.2011.R1
/************************************************************************
* Console.cs
* Copyright 2011 Calvin Rien
* (http://the.darktable.com)
*
* Derived from version 2.0 of Jeremy Hollingsworth's Console
*
* Copyright 2008-2010 By: Jeremy Hollingsworth
* (http://www.ennanzus-interactive.com)
*
* Licensed for commercial, non-commercial, and educational use.
*
* THIS PRODUCT IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND. THE
* LICENSOR MAKES NO WARRANTY REGARDING THE PRODUCT, EXPRESS OR IMPLIED.
* THE LICENSOR EXPRESSLY DISCLAIMS AND THE LICENSEE HEREBY WAIVES ALL
* WARRANTIES, EXPRESS OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, ALL
* IMPLIED WARRANTIES OF MERCHANTABILITY AND ALL IMPLIED WARRANTIES OF
* FITNESS FOR A PARTICULAR PURPOSE.
* ************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.IO;
//using NLua;

namespace LongDarkModLoader {
    /// <summary>
    /// Provides a game-mode, multi-line console with command binding, logging and watch vars.
    ///
    /// ==== Installation ====
    /// Just drop this script into your project. To use from JavaScript(UnityScript), just make sure
    /// you place this script in a folder such as "Plugins" so that it is compiled before your js code.
    ///
    /// See the following Unity docs page for more info on this:
    /// http://unity3d.com/support/documentation/ScriptReference/index.Script_compilation_28Advanced29.html
    ///
    /// ==== Usage (Logging) ====
    ///
    /// To use, you only need to access the desired static Log functions. So, for example, to log a simple
    /// message you would do the following:
    ///
    /// \code
    /// Console.Log("Hello World!");
    /// Console.LogWarning("Careful!");
    /// Console.LogError("Danger!");
    ///
    /// // Now open it
    /// Console.IsOpen = true;
    /// \endcode
    ///
    /// You can log any object that has a functional ToString() method.
    ///
    /// Those static methods will automatically ensure that the console has been set up in your scene for you,
    /// so no need to worry about attaching this script to anything.
    ///
    /// See the comments for the other static functions below for details on their use.
    ///
    /// ==== Usage (DebugCommand Binding) ====
    ///
    /// To use command binding, you create a function to handle the command, then you register that function
    /// along with the string used to invoke it with the console.
    ///
    /// So, for example, if you want to have a command called "ShowFPS", you would first create the handler like
    /// this:
    ///
    /// \code
    /// // JavaScript
    /// function ShowFPSCommand(args)
    /// {
    /// //...
    /// return "value you want printed to console";
    /// }
    ///
    /// // C#
    /// public object ShowFPSCommand(params string[] args)
    /// {
    /// //...
    /// return "value you want printed to console";
    /// }
    /// \endcode
    ///
    /// Then, to register the command with the console to be run when "ShowFPS" is typed, you would do the following:
    ///
    /// \code
    /// Console.RegisterCommand("ShowFPS", ShowFPSCommand);
    /// \endcode
    ///
    /// That's it! Now when the user types "ShowFPS" in the console and hits enter, your function will be run.
    ///
    /// You can also use anonymous functions to register commands
    /// \code
    /// Console.RegisterCommand("echo", args => {if (args.Length < 2) return ""; args[0] = ""; return string.Join(" ", args);});
    /// \endcode
    ///
    /// If you wish to capture input entered after the command text, the args array will contain every space-separated
    /// block of text the user entered after the command. "SetFOV 90" would pass the string "90" to the SetFOV command.
    ///
    /// Note: Typing "/?" followed by enter will show the list of currently-registered commands.
    ///
    /// ==== Usage (Watch Vars) ===
    ///
    /// For the Watch Vars feature, you need to use the provided class, or your own subclass of WatchVarBase, to store
    /// the value of your variable in your project. You then register that WatchVar with the console for tracking.
    ///
    /// Example:
    /// \code
    /// // JavaScript
    /// var myWatchInt = new WatchVar<int>("PowerupCount", 23);
    ///
    /// myWatchInt.Value = 230;
    ///
    /// myWatchInt.UnRegister();
    /// myWatchInt.Register();
    /// \endcode
    ///
    /// As you use that WatchVar<int> to store your value through the project, its live value will be shown in the console.
    ///
    /// You can create a WatchVar<T> for any object that has a functional ToString() method;
    ///
    /// If you subclass WatchVarBase, you can create your own WatchVars to represent more types than are currently built-in.
    /// </summary>
    ///
#if DEBUG_CONSOLE
    public class LDConsole : MonoBehaviour {
        readonly string ENTRYFIELD = "ConsoleEntryField";

        /// <summary>
        /// This is the signature for the DebugCommand delegate if you use the command binding.
        ///
        /// So, if you have a JavaScript function named "SetFOV", that you wanted run when typing a
        /// debug command, it would have to have the following definition:
        ///
        /// \code
        /// function SetFOV(args)
        /// {
        /// //...
        /// return "value you want printed to console";
        /// }
        /// \endcode
        /// </summary>
        /// <param name="args">The text typed in the console after the name of the command.</param>
        public delegate object CommandFunction(params string[] args);

        /// <summary>
        /// How many lines of text this console will display.
        /// </summary>
        public int maxLinesForDisplay = 500;


        public static int logLevel = 0;
        public static bool logOutput = false;
        public static string logName = "console";
        public static string ConsoleTittle = "Long Dark Mod Loader";

        /// <summary>
        /// Default color of the standard display text.
        /// </summary>
        public Color defaultColor = Message.defaultColor;
        public Color warningColor = Message.warningColor;
        public Color errorColor = Message.errorColor;
        public Color systemColor = Message.systemColor;
        public Color inputColor = Message.inputColor;
        public Color outputColor = Message.outputColor;

        /// <summary>
        /// Used to check (or toggle) the open state of the console.
        /// </summary>
        public static bool IsOpen {
            get { return LDConsole.Instance._isOpen; }
            set { LDConsole.Instance._isOpen = value; }
        }

        /// <summary>
        /// Static instance of the console.
        ///
        /// When you want to access the console without a direct
        /// reference (which you do in mose cases), use Console.Instance and the required
        /// GameObject initialization will be done for you.
        /// </summary>
        static LDConsole Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType(typeof(LDConsole)) as LDConsole;

                    if (_instance != null) {
                        return _instance;
                    }

                    GameObject console = new GameObject("__Debug Console__");
                    _instance = console.AddComponent<LDConsole>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Key to press to toggle the visibility of the console.
        /// </summary>
        public static KeyCode toggleKey = KeyCode.BackQuote;
        static LDConsole _instance;
        Dictionary<string, CommandFunction> _cmdTable = new Dictionary<string, CommandFunction>();
        Dictionary<string, CommandDescription> _commandDescriptions = new Dictionary<string, CommandDescription>();
        string _inputString = string.Empty;
        Rect _windowRect;
        Vector2 _logScrollPos = Vector2.zero;
        Vector2 _rawLogScrollPos = Vector2.zero;
        Vector3 _guiScale = Vector3.one;
        Matrix4x4 restoreMatrix = Matrix4x4.identity;
        bool _scaled = false;
        bool _isOpen = true;
        bool isResizingVert = false;
        bool isResizingHorz = false;
        StringBuilder _displayString = new StringBuilder();
        FPSCounter fps;
        bool dirty;
        #region GUI position values

        Rect messageLine = new Rect(4, 0, 264, 20);

        // Keep these private, their values are generated automatically
        Rect innerRect = new Rect(0, 0, 0, 0);
        GUIContent guiContent = new GUIContent();
        GUIStyle labelStyle;
        #endregion

        /// <summary>
        /// This Enum holds the message types used to easily control the formatting and display of a message.
        /// </summary>
        public enum MessageType {
            NORMAL,
            WARNING,
            ERROR,
            SYSTEM,
            INPUT,
            OUTPUT,
            DEBUG,
            INFO
        }

        /// <summary>
        /// Represents a single message, with formatting options.
        /// </summary>
        struct Message {
            string text;
            string formatted;
            MessageType type;
            public bool valid;

            public Color color { get; private set; }

            public static Color defaultColor = Color.white;
            public static Color warningColor = Color.yellow;
            public static Color errorColor = Color.red;
            public static Color systemColor = Color.green;
            public static Color inputColor = Color.green;
            public static Color outputColor = Color.cyan;
            public static Color devColor = Color.blue;

            public Message(object messageObject)
                : this(messageObject, MessageType.NORMAL, Message.defaultColor) {
            }

            public Message(object messageObject, Color displayColor)
                : this(messageObject, MessageType.NORMAL, displayColor) {
            }

            public Message(object messageObject, MessageType messageType)
                : this(messageObject, messageType, Message.defaultColor) {
                if (messageObject == null)
                    valid = false;
                switch (messageType) {
                    case MessageType.ERROR:
                        color = errorColor;
                        break;
                    case MessageType.SYSTEM:
                        color = systemColor;
                        break;
                    case MessageType.WARNING:
                        color = warningColor;
                        break;
                    case MessageType.OUTPUT:
                        color = outputColor;
                        break;
                    case MessageType.INPUT:
                        color = inputColor;
                        break;
                    case MessageType.DEBUG:
                        color = devColor;
                        break;
                }
            }

            void checkDebug(int level) {
                if (level == 99 && !UnityEngine.Debug.isDebugBuild)
                    return;


                string directory = Path.GetDirectoryName(Application.dataPath);
                if (LDConsole.logLevel >= level) {
                    valid = true;
                    if (LDConsole.logOutput)
                        if (File.Exists(global::System.IO.Path.Combine(directory, LDConsole.logName + ".log"))) {
                            using (StreamWriter sw = File.AppendText(global::System.IO.Path.Combine(directory, LDConsole.logName + ".log"))) {
                                sw.WriteLine(this.ToString());
                            }
                        }
                }
                else
                    valid = false;

            }

            public Message(object messageObject, MessageType messageType, Color displayColor)
                : this() {
                this.text = messageObject == null ? "<null>" : messageObject.ToString();

                this.formatted = string.Empty;
                this.type = messageType;
                this.color = Color.red;

                switch (messageType) {
                    case MessageType.ERROR:
                        checkDebug(0);
                        break;
                    case MessageType.INFO:
                        checkDebug(0);
                        break;
                    case MessageType.SYSTEM:
                        checkDebug(0);
                        break;
                    case MessageType.WARNING:
                        checkDebug(0);
                        break;
                    case MessageType.OUTPUT:
                        checkDebug(0);
                        break;
                    case MessageType.INPUT:
                        checkDebug(0);
                        break;
                    case MessageType.DEBUG:
                        checkDebug(0);
                        break;
                    default:
                        checkDebug(0);
                        break;
                }
            }

            public static Message Log(object message) {
                return new Message(message, MessageType.NORMAL, defaultColor);
            }

            public static Message System(object message) {
                return new Message(message, MessageType.SYSTEM, systemColor);
            }

            public static Message Warning(object message) {
                return new Message(message, MessageType.WARNING, warningColor);
            }

            public static Message Error(object message) {
                return new Message(message, MessageType.ERROR, errorColor);
            }

            public static Message Output(object message) {
                return new Message(message, MessageType.OUTPUT, outputColor);
            }

            public static Message Developer(object message) {
                return new Message(message, MessageType.DEBUG, devColor);
            }

            public static Message Input(object message) {
                return new Message(message, MessageType.INPUT, inputColor);
            }

            public override string ToString() {
                switch (type) {
                    case MessageType.ERROR:
                    case MessageType.WARNING:
                    case MessageType.DEBUG:
                    case MessageType.INFO:
                        return string.Format("[{0}] {1}", type, text);
                    default:
                        return ToGUIString();
                }
            }

            public string ToGUIString() {
                if (!string.IsNullOrEmpty(formatted)) {
                    return formatted;
                }

                switch (type) {
                    case MessageType.INPUT:
                        formatted = string.Format(">>> {0}", text);
                        break;
                    case MessageType.OUTPUT:
                        var lines = text.Trim('\n').Split('\n');
                        var output = new StringBuilder();

                        foreach (var line in lines) {
                            output.AppendFormat("= {0}\n", line);
                        }

                        formatted = output.ToString();
                        break;
                    case MessageType.SYSTEM:
                        formatted = string.Format("# {0}", text);
                        break;
                    case MessageType.WARNING:
                        formatted = string.Format("* {0}", text);
                        break;
                    case MessageType.ERROR:
                        formatted = string.Format("** {0}", text);
                        break;
                    case MessageType.DEBUG:
                        formatted = string.Format("% {0}", text);
                        break;
                    default:
                        formatted = text;
                        break;
                }

                return formatted;
            }
        }

        class History {
            List<string> history = new List<string>();
            int index = 0;

            public void Add(string item) {
                history.Add(item);
                index = 0;
            }

            string current;

            public string Fetch(string current, bool next) {
                if (index == 0) {
                    this.current = current;
                }

                if (history.Count == 0) {
                    return current;
                }

                index += next ? -1 : 1;

                if (history.Count + index < 0 || history.Count + index > history.Count - 1) {
                    index = 0;
                    return this.current;
                }

                var result = history[history.Count + index];

                return result;
            }
        }
        public struct CommandDescription {
            public string plugin;
            public string command;
            public string command_args;
            public string description_small;
            public string description_Long;
            public LDConsole.CommandFunction callback;

            public CommandDescription(string _plugin, string _command, string _command_args, string _description_small, string _description_Long, string _callback) {
                plugin = _plugin;
                command = _command.ToLower();
                command_args = _command_args;
                description_small = _description_small;
                description_Long = _description_Long;
                callback = GetCallbackFromStrick(_command, _plugin, _callback);
            }

            public CommandDescription(string _plugin, string _command, string _command_args, string _description_small, string _description_Long, LDConsole.CommandFunction _callback) {
                plugin = _plugin;
                command = _command.ToLower();
                command_args = _command_args;
                description_small = _description_small;
                description_Long = _description_Long;
                callback = _callback;
            }

            public CommandDescription(string _plugin, string _command, string _command_args, string _description_small, string _callback) {
                plugin = _plugin;
                command = _command.ToLower();
                command_args = _command_args;
                description_small = _description_small;
                description_Long = string.Empty;
                callback = GetCallbackFromStrick(_command, _plugin, _callback);
            }

            public CommandDescription(string _plugin, string _command, string _command_args, string _description_small, LDConsole.CommandFunction _callback) {
                plugin = _plugin;
                command = _command.ToLower();
                command_args = _command_args;
                description_small = _description_small;
                description_Long = string.Empty;
                callback = _callback;
            }

            private static LDConsole.CommandFunction GetCallbackFromStrick(string command, string _plugin, string _callback) {
                LDConsole.CommandFunction result = null;
                try {
                    List<string> Path = new List<string>(_callback.Split('.'));
                    string function = Path[Path.Count - 1];
                    Path.RemoveAt(Path.Count - 1);

                    string PathToType = string.Empty;
                    for (int i = 0; i < Path.Count; i++) {
                        PathToType += Path[i];
                        if (i < Path.Count - 1) {
                            PathToType += ".";
                        }
                    }

                    Type assemblyType = Type.GetType(PathToType + "," + Loader.GetAssemblyName(_plugin));
                    MethodInfo FunctionInfo = assemblyType.GetMethod(function);
                    object typeInstance = Loader.GetInstance(_plugin);
                    if (typeInstance != null) {
                        result = (LDConsole.CommandFunction)Delegate.CreateDelegate(typeof(LDConsole.CommandFunction), typeInstance, FunctionInfo);
                    }
                    else if (FunctionInfo.IsStatic) {
                        result = (LDConsole.CommandFunction)Delegate.CreateDelegate(typeof(LDConsole.CommandFunction), FunctionInfo);
                    }
                    else {
                        LDLog.LogError(string.Format("Failed binding command \"{0}\" to function \"{1}\": cannot find object instance.", command, _callback));
                    }
                }
                catch (Exception e) {
                    LogError(string.Format("Failed binding command \"{0}\" to function \"{1}\": {2}.", command, _callback, e.Message));
                    LogError(e);
                }

                return result;
            }
        }

        List<Message> _messages = new List<Message>();
        History _history = new History();

        void Awake() {
            if (_instance != null && _instance != this) {
                DestroyImmediate(this, true);
                return;
            }

            DontDestroyOnLoad(this);
            _instance = this;
        }

        void OnEnable() {
            var scale = Screen.dpi / 160.0f;

            if (scale != 0.0f && scale >= 1.1f) {
                _scaled = true;
                _guiScale.Set(scale, scale, scale);
            }

            fps = new FPSCounter();
            StartCoroutine(fps.Update());


            Message.defaultColor = defaultColor;
            Message.warningColor = warningColor;
            Message.errorColor = errorColor;
            Message.systemColor = systemColor;
            Message.inputColor = inputColor;
            Message.outputColor = outputColor;

            _windowRect = new Rect(30.0f, 30.0f, 300.0f, 420.0f);


            Loader.RegisterCommand(new CommandDescription(Loader.Name, "close", string.Empty, "closes the console.", CMDClose));
            Loader.RegisterCommand(new CommandDescription(Loader.Name, "clear", string.Empty, "Clear the console.", CMDClear));
            Loader.RegisterCommand(new CommandDescription(Loader.Name, "sys", string.Empty, "display system info.", "display detailed info about the system.", CMDSystemInfo));
            Loader.RegisterCommand(new CommandDescription(Loader.Name, "help", "[-p]|[command] [pluginName]", "displays this prompt", "Displays the help prompt. Use the name of a plugin as the third argument if '-p' was used as the second to get a list of every command for that plugin or enter in a command name for the second argument to get a detailed description of the command. notice: you do not need to place a '/' in front of the command name for the second argument", CMDHelp));
            
        }


        void OnGUI() {
            var evt = Event.current;

            if (_scaled) {
                restoreMatrix = GUI.matrix;

                GUI.matrix = GUI.matrix * Matrix4x4.Scale(_guiScale);
            }

            while (_messages.Count > maxLinesForDisplay) {
                _messages.RemoveAt(0);
            }
//#if (!MOBILE && DEBUG) || UNITY_EDITOR
            // Toggle key shows the console in non-iOS dev builds
            if (evt.keyCode == toggleKey && evt.type == EventType.KeyUp) {
                _isOpen = !_isOpen;
            }
//#endif

            if (!_isOpen) {
                return;
            }

            labelStyle = GUI.skin.label;

            innerRect.width = messageLine.width;
            _windowRect = GUI.Window(-1111, _windowRect, LogWindow, string.Format("{0} v{1}\tfps: {2:00.0}", ConsoleTittle, Loader.Version, fps.current));
            GUI.BringWindowToFront(-1111);

            if (GUI.GetNameOfFocusedControl() == ENTRYFIELD) {
                if (_inputString.Trim().Equals("`"))
                    _inputString = string.Empty;
                if (evt.isKey && evt.type == EventType.KeyUp) {
                    if (evt.keyCode == KeyCode.Return) {
                        try {
                            EvalInputString(_inputString);
                        }
                        catch (Exception e) {
                            LDConsole.LogError(string.Format("message: {0}.\nSource {1}.\nTrace: {2}.", e.Message, e.Source, e.StackTrace));
                        }
                        _inputString = string.Empty;
                    }
                    else if (evt.keyCode == KeyCode.UpArrow) {
                        _inputString = _history.Fetch(_inputString, true);
                    }
                    else if (evt.keyCode == KeyCode.DownArrow) {
                        _inputString = _history.Fetch(_inputString, false);
                    }
                }
            }

            if (_scaled) {
                GUI.matrix = restoreMatrix;
            }

            if (dirty && evt.type == EventType.Repaint) {
                _logScrollPos.y = 50000.0f;
                _rawLogScrollPos.y = 50000.0f;

                BuildDisplayString();
                dirty = false;
            }
        }

        void OnDestroy() {
            StopAllCoroutines();
        }
        #region StaticAccessors
        public static Dictionary<string, CommandDescription> CommandDescriptions {
            get { return Instance._commandDescriptions; }
        }

        /// <summary>
        /// Prints a message string to the console.
        /// </summary>
        /// <param name="message">Message to print.</param>
        public static object Log(object message) {
            LDConsole.Instance.LogMessage(Message.Log(message));

            return message;
        }

        public static object LogFormat(string format, params object[] args) {
            return Log(string.Format(format, args));
        }

        /// <summary>
        /// Prints a message string to the console.
        /// </summary>
        /// <param name="message">Message to print.</param>
        /// <param name="messageType">The MessageType of the message. Used to provide
        /// formatting in order to distinguish between message types.</param>
        public static object Log(object message, MessageType messageType) {
            LDConsole.Instance.LogMessage(new Message(message, messageType));

            return message;
        }

        /// <summary>
        /// Prints a message string to the console.
        /// </summary>
        /// <param name="message">Message to print.</param>
        /// <param name="displayColor">The text color to use when displaying the message.</param>
        public static object Log(object message, Color displayColor) {
            LDConsole.Instance.LogMessage(new Message(message, displayColor));

            return message;
        }

        /// <summary>
        /// Prints a message string to the console.
        /// </summary>
        /// <param name="message">Messate to print.</param>
        /// <param name="messageType">The MessageType of the message. Used to provide
        /// formatting in order to distinguish between message types.</param>
        /// <param name="displayColor">The color to use when displaying the message.</param>
        /// <param name="useCustomColor">Flag indicating if the displayColor value should be used or
        /// if the default color for the message type should be used instead.</param>
        public static object Log(object message, MessageType messageType, Color displayColor) {
            LDConsole.Instance.LogMessage(new Message(message, messageType, displayColor));

            return message;
        }

        /// <summary>
        /// Prints a message string to the console using the "Warning" message type formatting.
        /// </summary>
        /// <param name="message">Message to print.</param>
        public static object LogWarning(object message) {
            LDConsole.Instance.LogMessage(Message.Warning(message));

            return message;
        }

        /// <summary>
        /// Prints a message string to the console using the "Error" message type formatting.
        /// </summary>
        /// <param name="message">Message to print.</param>
        public static object LogError(object message) {
            LDConsole.Instance.LogMessage(Message.Error(message));

            return message;
        }
        public static object LogSystem(object message) {
            LDConsole.Instance.LogMessage(Message.System(message));

            return message;
        }
        public static object LogDebug(object message) {
            LDConsole.Instance.LogMessage(Message.Developer(message));

            return message;
        }
        public static object LogInfo(object message) {
            LDConsole.Instance.LogMessage(new Message(message, MessageType.INFO));

            return message;
        }

        /// <summary>
        /// Clears all console output.
        /// </summary>
        public static void Clear() {
            LDConsole.Instance.ClearLog();
        }

        /// <summary>
        /// Execute a console command directly from code.
        /// </summary>
        /// <param name="commandString">The command line you want to execute. For example: "sys"</param>
        public static string Execute(string commandString) {
            string inputString = commandString.Trim();

            if (string.IsNullOrEmpty(inputString)) {
                return Message.Input(string.Empty).ToString();
            }

            var input = new List<string>(inputString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));

            var cmd = input[0].ToLower();

            if (LDConsole.Instance._cmdTable.ContainsKey(cmd)) {
                return LDConsole.Instance._cmdTable[cmd](input.ToArray()).ToString();
            }
            else {
                return Message.Output(string.Format("*** Unknown Command: {0} ***", cmd)).ToString();
            }
        }

        public static string[] Commands() {
            string[] commands = new string[LDConsole.Instance._cmdTable.Keys.Count];
            LDConsole.Instance._cmdTable.Keys.CopyTo(commands, 0);
            return commands;
        }

        /// <summary>
        /// Registers a debug command that is "fired" when the specified command string is entered.
        /// </summary>
        /// <param name="commandString">The string that represents the command. For example: "FOV"</param>
        /// <param name="commandCallback">The method/function to call with the commandString is entered.
        /// For example: "SetFOV"</param>

        public static void RegisterCommand(CommandDescription command) {
            try {
                LDConsole.Instance.RegisterCommandCallback(command, command.callback);
            }
            catch (Exception e) {
                LDLog.LogError(string.Format("Failed to add command \"{0}\": {1}", command.command, e.Message));
            }
        }

        /// <summary>
        /// Removes a previously-registered debug command.
        /// </summary>
        /// <param name="commandString">The string that represents the command.</param>

        public static void UnRegisterCommand(string commandString) {
            LDConsole.Instance.UnRegisterCommandCallback(commandString);
        }
        #endregion
        #region Console commands

        //==== Built-in example DebugCommand handlers ====
        object CMDClose(params string[] args) {
            _isOpen = false;

            return "closed";
        }

        object CMDClear(params string[] args) {
            this.ClearLog();
            return "";
        }

        object CMDHelp(params string[] args) {
            var output = new StringBuilder();
            
            if (args.Length == 1) {
                output.AppendLine(":: Command List ::");
                foreach (string key in _cmdTable.Keys) {
                    output.AppendLine(string.Format("{0} {1} : {2}", key, _commandDescriptions[key].command_args, _commandDescriptions[key].description_small));
                }
            }
            else if (args.Length == 3 && args[1].ToLower().StartsWith("-p")) {
                if (Loader.HasCommands(args[2].ToLower())) {
                    output.AppendLine("");
                    output.AppendLine("Available commands (<> = required, [] = optional): ");
                    CommandDescription[] pluginCmds = Loader.GetCommands(args[2].ToLower());
                    foreach (CommandDescription cmd in pluginCmds) {
                        output.AppendLine(cmd.command + " " + cmd.command_args + ": " + cmd.description_small + ".");
                    }
                    output.AppendLine("");
                }
                else {
                    output.AppendLine("Plugin not found.");
                }
            }
            else if (args.Length == 2 && !args[1].ToLower().StartsWith("-p")) {
                if (_cmdTable.ContainsKey(args[1])) {
                    output.AppendLine("Available commands (<> = required, [] = optional):");
                    output.AppendLine(string.Format("Command: {0} {1}", _commandDescriptions[args[1]].command, _commandDescriptions[args[1]].command_args));
                    if (string.IsNullOrEmpty(_commandDescriptions[args[1]].description_Long)) {
                        output.AppendLine(string.Format("Description: {0}", _commandDescriptions[args[1]].description_small));
                    }
                    else {
                        output.AppendLine(string.Format("Short description: {0}", _commandDescriptions[args[1]].description_small));
                        output.AppendLine(string.Format("Long description: {0}", _commandDescriptions[args[1]].description_Long));
                    }
                }
                else
                    output.AppendLine("Command not found.");
            }
            else if (args.Length == 2 && args[1].ToLower().StartsWith("-p")) {
                output.AppendLine("Please specify a plugin.");
            }
            else if (args.Length > 3) {
                output.AppendLine("To many arguments.");
            }
            else
                output.AppendLine("Unknown error.");

            output.AppendLine(" ");
            
            return output.ToString();
        }

        object CMDSystemInfo(params string[] args) {
            var info = new StringBuilder();

            info.AppendFormat("Unity Ver: {0}\n", Application.unityVersion);
            info.AppendFormat("Platform: {0} Language: {1}\n", Application.platform, Application.systemLanguage);
            info.AppendFormat("Screen:({0},{1}) DPI:{2} Target:{3}fps\n", Screen.width, Screen.height, Screen.dpi, Application.targetFrameRate);
            info.AppendFormat("Level: {0} ({1} of {2})\n", Application.loadedLevelName, Application.loadedLevel, Application.levelCount);
            info.AppendFormat("Quality: {0}\n", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            info.AppendLine();
            info.AppendFormat("Data Path: {0}\n", Application.dataPath);
            info.AppendFormat("Cache Path: {0}\n", Application.temporaryCachePath);
            info.AppendFormat("Persistent Path: {0}\n", Application.persistentDataPath);

#if UNITY_EDITOR
            info.AppendLine();
            info.AppendFormat("editorApp: {0}\n", UnityEditor.EditorApplication.applicationPath);
            info.AppendFormat("editorAppContents: {0}\n", UnityEditor.EditorApplication.applicationContentsPath);
            info.AppendFormat("scene: {0}\n", UnityEditor.EditorApplication.currentScene);
#endif
            info.AppendLine();
            var devices = WebCamTexture.devices;
            if (devices.Length > 0) {
                info.AppendLine("Cameras: ");

                foreach (var device in devices) {
                    info.AppendFormat(" {0} front:{1}\n", device.name, device.isFrontFacing);
                }
            }

            return info.ToString();
        }
        #endregion
        #region GUI Window Methods

        void DrawBottomControls() {
            Rect inputRect = new Rect(5, _windowRect.height - 33, _windowRect.width - 10, 24);
            GUI.SetNextControlName(ENTRYFIELD);
            _inputString = GUI.TextField(inputRect, _inputString);

        }
        void DrawResizeControls(int windowID) {
            Rect verticleResize = new Rect(0, _windowRect.height - 10, _windowRect.width - 10, 10);
            Rect HorizontalResize = new Rect(_windowRect.width - 10, 0, 10, _windowRect.height - 10);
            Rect cornerResize = new Rect(_windowRect.width - 10, _windowRect.height - 10, 10, 10);
            Event e = Event.current;


            // Resizing Controls
            if (verticleResize.Contains(e.mousePosition)) {
                if (e.type == EventType.MouseDown && e.button == 0) {
                    isResizingVert = true;
                }

            }
            if (HorizontalResize.Contains(e.mousePosition)) {
                if (e.type == EventType.MouseDown && e.button == 0) {
                    isResizingHorz = true;
                }

            }
            if (cornerResize.Contains(e.mousePosition)) {
                if (e.type == EventType.MouseDown && e.button == 0) {
                    isResizingVert = true;
                    isResizingHorz = true;
                }

            }
            if (e.type == EventType.mouseUp) {
                isResizingVert = false;
                isResizingHorz = false;
            }
            if (isResizingVert)
                _windowRect.height = _windowRect.y + (e.mousePosition.y - _windowRect.y) + 5;
            if (isResizingHorz)
                _windowRect.width = _windowRect.x + (e.mousePosition.x - _windowRect.x) + 5;


            _windowRect.height = Mathf.Clamp(_windowRect.height, 200, Screen.height - _windowRect.y);
            _windowRect.width = Mathf.Clamp(_windowRect.width, 100, Screen.width - _windowRect.x);


            if (!isResizingVert && !isResizingHorz)
                GUI.DragWindow();




        }

        string GetDisplayString() {
            if (_messages == null) {
                return string.Empty;
            }

            return _displayString.ToString();
        }

        void BuildDisplayString() {
            _displayString.Length = 0;

            foreach (Message m in _messages) {
                _displayString.AppendLine(m.ToString());
            }
        }

        void LogWindow(int windowID) {
            Rect scrollRect = new Rect(5, 20, _windowRect.width - 10, _windowRect.height - 60);

            messageLine.width = _windowRect.width - 30;
            guiContent.text = GetDisplayString();

            var calcHeight = GUI.skin.textArea.CalcHeight(guiContent, messageLine.width);

            innerRect.height = calcHeight < scrollRect.height ? scrollRect.height : calcHeight;

            _rawLogScrollPos = GUI.BeginScrollView(scrollRect, _rawLogScrollPos, innerRect, false, true);

            GUI.TextArea(innerRect, guiContent.text);

            GUI.EndScrollView();

            DrawBottomControls();
            DrawResizeControls(windowID);
        }

        #endregion
        #region InternalFunctionality
        void LogMessage(Message msg) {
            if (msg.valid) {
                if (_messages.Count < 700)
                    _messages.Add(msg);
                else {
                    List<Message> tmpList = new List<Message>();
                    for (int i = 200; i < _messages.Count; i++) {
                        tmpList.Add(_messages[i]);
                    }
                    _messages.Clear();
                    _messages = new List<Message>(tmpList);
                    _messages.Add(msg);
                }
            }
            dirty = true;
        }

        //--- Local version. Use the static version above instead.
        void ClearLog() {
            _messages.Clear();
        }

        //--- Local version. Use the static version above instead.
        void RegisterCommandCallback(CommandDescription command, CommandFunction commandCallback) {
            _cmdTable[command.command.ToLower()] = new CommandFunction(commandCallback);
            _commandDescriptions.Add(command.command, command);
        }

        //--- Local version. Use the static version above instead.
        void UnRegisterCommandCallback(string commandString) {
            _cmdTable.Remove(commandString.ToLower());
            _commandDescriptions.Remove(commandString.ToLower());
        }


        void EvalInputString(string inputString) {
            inputString = inputString.Trim();

            if (string.IsNullOrEmpty(inputString)) {
                LogMessage(Message.Input(string.Empty));
                return;
            }

            _history.Add(inputString);
            LogMessage(Message.Input(inputString));

            var input = new List<string>(inputString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));

            var cmd = input[0].ToLower();

            if (_cmdTable.ContainsKey(cmd)) {
                Log(_cmdTable[cmd](input.ToArray()), MessageType.OUTPUT);
            }
            else {
                LogMessage(Message.Output(string.Format("*** Unknown Command: {0} ***", cmd)));
            }
        }
        #endregion
    }

    public class FPSCounter {
        public float current = 0.0f;
        public float updateInterval = 0.5f;
        // FPS accumulated over the interval
        float accum = 0;
        // Frames drawn over the interval
        int frames = 1;
        // Left time for current interval
        float timeleft;
        float delta;

        public FPSCounter() {
            timeleft = updateInterval;
        }

        public IEnumerator Update() {
            // skip the first frame where everything is initializing.
            yield return null;

            while (true) {
                delta = Time.deltaTime;

                timeleft -= delta;
                accum += Time.timeScale / delta;
                ++frames;

                // Interval ended - update GUI text and start new interval
                if (timeleft <= 0.0f) {
                    current = accum / frames;
                    timeleft = updateInterval;
                    accum = 0.0f;
                    frames = 0;
                }

                yield return null;
            }
        }
    }

    namespace UnityMock {
        public static class Debug {
            // Methods

            public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) {
                UnityEngine.Debug.DrawLine(start, end, color, duration);
            }


            public static void DrawLine(Vector3 start, Vector3 end, Color color) {
                UnityEngine.Debug.DrawLine(start, end, color);
            }


            public static void DrawLine(Vector3 start, Vector3 end) {
                UnityEngine.Debug.DrawLine(start, end);
            }


            public static void DrawRay(Vector3 start, Vector3 dir, Color color) {
                UnityEngine.Debug.DrawRay(start, dir, color);
            }


            public static void DrawRay(Vector3 start, Vector3 dir) {
                UnityEngine.Debug.DrawRay(start, dir);
            }


            public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) {
                UnityEngine.Debug.DrawRay(start, dir, color);
            }


            public static void Break() {
                UnityEngine.Debug.Break();
            }


            public static void DebugBreak() {
                UnityEngine.Debug.DebugBreak();
            }


            public static void Log(object message) {
                UnityEngine.Debug.Log(message);
            }


            public static void Log(object message, UnityEngine.Object context) {
                UnityEngine.Debug.Log(message, context);
            }


            public static void LogError(object message) {
                UnityEngine.Debug.LogError(message);
            }


            public static void LogError(object message, UnityEngine.Object context) {
                UnityEngine.Debug.LogError(message, context);
            }


            public static void LogWarning(object message) {
                UnityEngine.Debug.LogWarning(message);
            }


            public static void LogWarning(object message, UnityEngine.Object context) {
                UnityEngine.Debug.LogWarning(message, context);
            }

            // Properties
            public static bool isDebugBuild {
#if DEBUG
                get { return true; }
#else
			get { return false; }
#endif
            }
        }
    }
#endif
}