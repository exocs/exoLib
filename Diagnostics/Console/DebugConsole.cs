﻿using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace exoLib.Diagnostics.Console
{
	/// <summary>
	/// Base console implementation.
	/// 
	/// There should only be a single instance of this or derived class at any given time.
	/// </summary>
	public abstract class DebugConsole : MonoBehaviour
	{
		[Serializable]
		public struct Colors
		{
			/// <summary>
			/// Primary console color.
			/// </summary>
			public Color ContentColor;

			/// <summary>
			/// Background console color.
			/// </summary>
			public Color BackgroundColor;

			/// <summary>
			/// Default log console color.
			/// </summary>
			public Color LogColor;

			/// <summary>
			/// Warning log console color.
			/// </summary>
			public Color WarningColor;

			/// <summary>
			/// Error log console color.
			/// </summary>
			public Color ErrorColor;

			/// <summary>
			/// Echoed input color.
			/// </summary>
			public Color EchoColor;

			/// <summary>
			/// Returns default console settings.
			/// </summary>
			public static Colors Default
			{
				get
				{
					//var contentColor = new Color(1.0f, 0.7f, 0.3f);
					var contentColor = Color.white;
					var colors = new Colors
					{
						ContentColor = contentColor,
						LogColor = contentColor,
						BackgroundColor = Color.black,
						WarningColor = Color.Lerp(Color.red, Color.yellow, 0.5f), // orange
						ErrorColor = Color.red,
						EchoColor = Color.yellow
					};
					return colors;
				}
			}
		}

		/// <summary>
		/// Provides the user with a in-game debug console.
		/// Allows registering of various commands with parameters, see example for reference.
		/// </summary>
		private static DebugConsole _instance;
		public static DebugConsole Instance
		{
			get
			{
				if (_instance == null)
				{
#if UNITY_SERVER
					// Headless mode, see BuildOptions.EnableHeadlessMode.
                   _instance = new GameObject("Server Console").AddComponent<ServerConsole>();
#else
					// Standard client
					_instance = new GameObject("Game Console").AddComponent<GameConsole>();
#endif

					// Disable destroying of current instance
					DontDestroyOnLoad(_instance);

				}
				return _instance;
			}
		}

		/// <summary>
		/// Is the console open?
		/// </summary>
		public virtual bool IsOpen { get; set; }

		/// <summary>
		/// Should the user input be echoed to the console?
		/// </summary>
		public virtual bool EchoInput { get; set; } = true;

		/// <summary>
		/// Is the console Width and Height specified as relative (0,1) or in pixels (0, pixels) ?
		/// </summary>
		public virtual bool IsRelative { get; set; } = true;
		/// <summary>
		/// Console width. 
		/// Relative if <see cref="IsRelative"/> is set to true, absolute in pixels otherwise.
		/// </summary>
		public virtual float Width { get; set; } = 1.0f;
		/// <summary>
		/// Console height.
		/// Relative if <see cref="IsRelative"/> is set to true, absolute in pixels otherwise.
		/// </summary>
		public virtual float Height { get; set; } = 0.5f;
		/// <summary>
		/// Console position.
		/// Relative if <see cref="IsRelative"/> is set to true, absolute in pixels otherwise.
		/// </summary>
		public virtual Vector2 Position { get; set; } = Vector2.zero;
		/// <summary>
		/// Console colors.
		/// </summary>
		public Colors ConsoleColors = Colors.Default;

		/// <summary>
		/// To not reallocate the comamnds dictionary intially, we will begin with fixed capacity.
		/// </summary>
		private const int INITIAL_CAPACITY = 32;
		/// <summary>
		/// Dictionary of registered console commands.
		/// </summary>
		private readonly Dictionary<string, ConsoleCommand> _commands = new Dictionary<string, ConsoleCommand>(INITIAL_CAPACITY);

		/// <summary>
		/// Initialize the console.
		/// </summary>
		protected virtual void Awake()
		{
			// Hook events
			Application.logMessageReceived += OnLogMessageReceived;

			// Disable stack trace for logs, leave only for warnings, errors...
			Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

			// Register help command
			RegisterCommand("help", (args) =>
			{
				// Create the help message
				// in the following format:
				//
				//		commandName0: description
				//		commandName1: description
				//		...
				//		commandNameN: description
				//
				var stringBuilder = new StringBuilder();
				foreach (var keyPairValue in _commands)
				{
					stringBuilder.Append(keyPairValue.Key.ToLowerInvariant());
					stringBuilder.Append(": ");
					stringBuilder.Append(keyPairValue.Value.Description);
					stringBuilder.Append(Environment.NewLine);
				}

				// Log the help message
				Debug.Log(stringBuilder.ToString());

			}, 0, "Prints available commands information.");

			// Register echo command
			RegisterCommand("echo", (args) =>
			{
				var builder = new StringBuilder();
				// Append all args except last one with a space (as intended)
				for (int i = 0; i < args.Length - 1; i++)
				{
					builder.Append(args[i]);
					builder.Append(" ");
				}
				// Append last one too
				builder.Append(args[args.Length - 1]);

				// Log the message
				Debug.Log(builder.ToString());

			}, 1, "Prints message to the console output.");

			// Register clear command
			RegisterCommand("clear", (args) =>
			{
				ClearConsole();
			}, 0, "Clears the console window.");
		}

		/// <summary>
		/// Called on frame basis.
		/// </summary>
		protected virtual void Update()
		{

		}

		/// <summary>
		/// Called after each frame.
		/// </summary>
		protected virtual void LateUpdate()
		{

		}

		/// <summary>
		/// Handles disposing of console.
		/// </summary>
		protected virtual void OnDestroy()
		{

		}

		/// <summary>
		/// Clear the console.
		/// </summary>
		protected virtual void ClearConsole()
		{

		}

		/// <summary>
		/// Attached to application log event.
		/// </summary>
		protected virtual void OnLogMessageReceived(string condition, string stackTrace, LogType type)
		{

		}

		/// <summary>
		/// Tries to parse command and execute it from the provided line of text.
		/// </summary>
		/// <param name="line">line of text to parse and execute</param>
		/// <returns>Returns true in case of success, false otherwise.</returns>
		protected bool Execute(string line)
		{
			if (string.IsNullOrEmpty(line))
				return false;

			// Command needs to have at least one symbol
			var symbols = line.Split(' ');
			if (symbols != null && symbols.Length < 1)
				return false;

			// Our command name is the first symbol, the rest is arguments.
			var commandName = symbols[0];
			if (string.IsNullOrEmpty(commandName))
				return false;

			// Get argumetns
			var arguments = new string[symbols.Length - 1];
			for (int i = 1; i <= arguments.Length; i++)
				arguments[i - 1] = symbols[i];

			return ExecuteCommand(commandName, arguments);
		}

		/// <summary>
		/// Executes command of provided name (if it exists).
		/// </summary>
		/// <param name="commandName">Name of command</param>
		/// <param name="arguments">Arguments to be passed into command</param>
		/// <returns>Returns true in case of success, false otherwise.</returns>
		protected bool ExecuteCommand(string commandName, params string[] arguments)
		{
			var command = FindCommand(commandName);
			if (command == null)
				return false;

			var argumentsCount = (arguments != null) ? arguments.Length : 0;
			if (argumentsCount < command.MinimumArgumentsCount)
				return false;

			// In case somebody does some nasty stuff in the method itself,
			// this will slow down the execution, but the console will always be safe
			// from whatever code may be invoked..
			try
			{
				command.Function.Invoke(arguments);
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Tries to find command by its name.
		/// </summary>
		/// <param name="commandName">Name of command to find.</param>
		/// <returns>Command or null if none.</returns>
		private ConsoleCommand FindCommand(string commandName)
		{
			var key = commandName.ToUpperInvariant();
			if (_commands.ContainsKey(key))
				return _commands[key];

			return null;
		}

		/// <summary>
		/// Registers command into the console.
		/// </summary>
		/// <param name="commandName">The name (key) of this command.</param>
		/// <param name="consoleCommand">Command information.</param>
		/// <returns>True in case of success, false in case of failure.</returns>
		private bool RegisterCommand(string commandName, ConsoleCommand consoleCommand)
		{
			var key = commandName.ToUpperInvariant();
			if (_commands.ContainsKey(key))
				return false;

			_commands.Add(key, consoleCommand);
			return true;
		}

		/// <summary>
		/// Registers command into the console.
		/// </summary>
		/// <param name="commandName">The name (key) of this command.</param>
		/// <param name="commandFunction">The function (callback) this command will invoke.</param>
		/// <param name="minimumArgumentsCount">Minimum amount of arguments for this command (or 0 if none)</param>
		/// <param name="description">Description of this command.</param>
		/// <returns></returns>
		public bool RegisterCommand(string commandName, ConsoleFunction commandFunction, uint minimumArgumentsCount = 0, string description = "No description provided")
		{
			var command = new ConsoleCommand(commandFunction, minimumArgumentsCount, description);
			return RegisterCommand(commandName, command);
		}
	}
}