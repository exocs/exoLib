using UnityEngine;

using exoLib.WinApi;

namespace exoLib.Diagnostics.Console
{
	/// <summary>
	/// Console handler for dedicated (headless) server builds.
	/// 
	/// Attempts to hook onto existing console and read the input to be
	/// later parsed and executed as commands.
	/// </summary>
	sealed class ServerConsole : DebugConsole
	{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		private readonly WinConsole _consoleWindow = new WinConsole();

		/// <summary>
		/// By assumption in headless build the console is always open.
		/// </summary>
		public override bool IsOpen => true;
		/// <summary>
		/// Initialize server console.
		/// </summary>
		protected override void Awake()
		{
			// Initialize the console
			_consoleWindow.Initialize();
			// Sets the title
			_consoleWindow.SetTitle(Application.productName + " Console");

			// Hooks user input to execute the command
			_consoleWindow.OnInput += (s) =>
			{
				Execute(s);
			};

			// Parent awake
			base.Awake();
		}
		/// <summary>
		/// Update the console state.
		/// </summary>
		protected override void LateUpdate()
		{
			// Update the console
			_consoleWindow.Update();

			// Update whether input should be echoed
			_consoleWindow.EchoInput = EchoInput;

			// Parent update
			base.LateUpdate();
		}
		/// <summary>
		/// Dispose of the console.
		/// </summary>
		protected override void OnDestroy()
		{
			// Dispose of the window
			_consoleWindow.Dispose();

			// Parent destroy
			base.OnDestroy();
		}
		/// <summary>
		/// Clear the console window.
		/// </summary>
		protected override void ClearConsole()
		{
			_consoleWindow.Clear();
			base.ClearConsole();
		}
		/// <summary>
		/// Called when a log mesage is received. Passed into console window.
		/// </summary>
		protected override void OnLogMessageReceived(string condition, string stackTrace, LogType type)
		{
			_consoleWindow.OnLogMessageReceived(condition, stackTrace, type);
			base.OnLogMessageReceived(condition, stackTrace, type);
		}
	}
#endif
}