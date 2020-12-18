using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace exoLib.WinApi
{
	/// <summary>
	/// A wrapper for Windows console.
	/// Will only work in Windows builds.
	/// </summary>
	public class WinConsole
	{
		/// <summary>
		/// User input text color.
		/// </summary>
		public ConsoleColor EchoColor { get; set; } = ConsoleColor.Yellow;
		/// <summary>
		/// Output text color.
		/// </summary>
		public ConsoleColor LogColor { get; set; } = ConsoleColor.White;
		/// <summary>
		/// Error text color.
		/// </summary>
		public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
		/// <summary>
		/// Warning text color.
		/// </summary>
		public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
		/// <summary>
		/// Should the user input be echoed?
		/// </summary>
		public bool EchoInput { get; set; } = true;

		/// <summary>
		/// Delegate for string based events.
		/// </summary>
		public delegate void TextEvent(string text);

		/// <summary>
		/// Called when user submits input.
		/// </summary>
		public event TextEvent OnInput;

		/// <summary>
		/// Previous output that we can trestore
		/// </summary>
		private TextWriter _previousOutput;

		/// <summary>
		/// Current user input.
		/// </summary>
		private string _currentInput = string.Empty;

		/// <summary>
		/// When log message is received, next update shall redraw the input
		/// </summary>
		private bool _redrawInput = false;

		/// <summary>
		/// Initialize this console window.
		/// </summary>
		public void Initialize()
		{
			// Try attaching to existing console, in case of failure create new one
			if (!AttachConsole(0x0ffffffff))
				AllocConsole();

			// Store previous out so we can restore it when we're destroyed
			_previousOutput = Console.Out;

			try
			{
				// Get console fhandle
				IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
				Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true);

				// Open stream
				FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
				System.Text.Encoding encoding = System.Text.Encoding.ASCII;

				// Prepare writer and redirect the console output
				StreamWriter standardOutput = new StreamWriter(fileStream, encoding)
				{
					AutoFlush = true
				};
				Console.SetOut(standardOutput);

				// Set default color
				Console.ForegroundColor = LogColor;
			}
			catch (Exception e)
			{
				Debug.Log("Output could not be redirected: " + e.Message);
			}
		}

		/// <summary>
		/// When input is received we will clear our line and rewrite it
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Follows Unity.LogCallback signature.")]
		public void OnLogMessageReceived(string condition, string stackTrace, LogType type)
		{
			// Choose color per type
			switch (type)
			{
				case LogType.Log:
					Console.ForegroundColor = LogColor;
					break;
				case LogType.Warning:
					Console.ForegroundColor = WarningColor;
					break;
				case LogType.Error:
				case LogType.Assert:
				case LogType.Exception:
					Console.ForegroundColor = ErrorColor;
					break;
			}

			ClearLine();
			_redrawInput = true;
		}

		/// <summary>
		/// Update this console window.
		/// </summary>
		public void Update()
		{
			// We want to redraw input due to received log message
			if (_redrawInput)
			{
				RewriteInputLine();
				_redrawInput = false;
			}

			// No key, we do not worry about input
			if (!Console.KeyAvailable)
				return;

			var key = Console.ReadKey();
			OnKey(key);
		}

		/// <summary>
		/// Occurs when the user presses a key
		/// </summary>
		private void OnKey(ConsoleKeyInfo key)
		{
			switch (key.Key)
			{
				case ConsoleKey.Backspace:
					EraseInputLine();
					return;

				case ConsoleKey.Escape:
					ClearInputLine();
					return;

				case ConsoleKey.Enter:
					SubmitInputLine();
					return;

				default:
					break;
			}

			// if enter -> submit
			// if backspace -> delet
			// if escape -> escap
			// if not newline -> add
			// Character is not null
			if (key.KeyChar != '\u0000')
			{
				_currentInput += key.KeyChar;
				RewriteInputLine();
				return;
			}
		}

		/// <summary>
		/// Clears a line in the console.
		/// </summary>
		private void ClearLine()
		{
			Console.CursorLeft = 0;
			// Replace whole line width with empty characters
			Console.Write(new string(' ', Console.BufferWidth));
			Console.CursorTop--;
			Console.CursorLeft = 0;
		}

		/// <summary>
		/// Update the input line
		/// </summary>
		private void RewriteInputLine()
		{
			// We don't draw anything
			if (_currentInput.Length == 0)
				return;

			// We'll have to clear what we have
			if (Console.CursorLeft > 0)
				ClearLine();

			// Set color
			Console.ForegroundColor = EchoColor;
			// Write the text
			Console.Write(_currentInput);
			// Restore color
			Console.ForegroundColor = LogColor;
		}

		/// <summary>
		/// Erases one character from the input line
		/// </summary>
		private void EraseInputLine()
		{
			// We're empty already
			if (_currentInput.Length < 1)
				return;

			_currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
			RewriteInputLine();
		}

		/// <summary>
		/// Erases the whole content of input line.
		/// </summary>
		private void ClearInputLine()
		{
			ClearLine();
			_currentInput = string.Empty;
		}

		/// <summary>
		/// Submits current input line
		/// </summary>
		private void SubmitInputLine()
		{
			// Store current input
			var input = _currentInput;

			// Clear our input
			_currentInput = string.Empty;

			// Clear and echo what we submitted
			ClearLine();

			if (EchoInput)
			{
				Console.ForegroundColor = EchoColor;
				Console.WriteLine("> " + input);
				Console.ForegroundColor = LogColor;
			}

			OnInput?.Invoke(input);
		}

		/// <summary>
		/// Clears the console window.
		/// </summary>
		public void Clear()
		{
			Console.Clear();
		}

		/// <summary>
		/// Disposes of this console window.
		/// </summary>
		public void Dispose()
		{
			Console.SetOut(_previousOutput);
			FreeConsole();
		}

		/// <summary>
		/// Set the console title
		/// </summary>
		public void SetTitle(string title)
		{
			SetConsoleTitle(title);
		}

		// See https://docs.microsoft.com/en-us/windows/console/getstdhandle
		private const int STD_OUTPUT_HANDLE = -11;

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleTitle(string lpConsoleTitle);
	}
}
