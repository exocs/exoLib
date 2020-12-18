using System;
using System.Text;
using UnityEngine;

namespace exoLib.Diagnostics.Console
{
	/// <summary>
	/// Console handler for game (standalone) builds.
	/// 
	/// Uses immediate GUI to draw the console with all controls.
	/// </summary>
	sealed class GameConsole : DebugConsole
	{
		/// <summary>
		/// The key that will be used to toggle console visibility.
		/// </summary>
		public KeyCode OpenCloseKey = KeyCode.BackQuote;

		/// <summary>
		/// The key that will be used to submit requests.
		/// </summary>
		public KeyCode SubmitKey = KeyCode.Return;

		/// <summary>
		/// The key that will be used to clear our input.
		/// </summary>
		public KeyCode ClearKey = KeyCode.Escape;

		/// <summary>
		/// Current console input written by the user.
		/// </summary>
		private string _currentInput = string.Empty;

		/// <summary>
		/// Current console output
		/// </summary>
		private readonly StringBuilder _currentOutput = new StringBuilder();

		/// <summary>
		/// Are we toggling the console?
		/// </summary>
		private bool _shouldToggle;

		/// <summary>
		/// Are we submitting our input?
		/// </summary>
		private bool _shouldSubmit;

		/// <summary>
		/// Are we clearing our input?
		/// </summary>
		private bool _shouldClear;

		/// <summary>
		/// Scroll amount in the output field.
		/// </summary>
		private Vector2 _scrollAmount;

		/// <summary>
		/// Handle input for GUI
		/// </summary>
		protected override void Update()
		{
			// In addition when console is closed, we will want to open it outside of the GUI
			if (Input.GetKeyDown(OpenCloseKey))
				_shouldToggle = true;

			// We will handle these changes in update,
			// because GUI may be called multiple times a frame
			// and it will yield in broken layouts
			if (_shouldToggle)
			{
				IsOpen = !IsOpen;
				_shouldToggle = false;
			}

			if (_shouldSubmit)
			{
				Submit();
				_shouldSubmit = false;
			}

			if (_shouldClear)
			{
				_currentInput = string.Empty;
				_shouldClear = false;
			}


			// Parent update
			base.Update();
		}

		/// <summary>
		/// Draws the console GUI.
		/// </summary>
		private void OnGUI()
		{
			// Console is closed, we will not draw anything
			if (!IsOpen)
				return;

			// If console is open, we may want to handle inputs here,
			// but process them only when update comes
			var currentEvent = Event.current;
			if (currentEvent.type == EventType.KeyDown)
			{
				if (currentEvent.keyCode == OpenCloseKey)
					_shouldToggle = true;

				if (currentEvent.keyCode == SubmitKey)
					_shouldSubmit = true;

				if (currentEvent.keyCode == ClearKey)
					_shouldClear = true;
			}

			var consoleRect = GetConsoleRect();
			GUILayout.BeginArea(consoleRect);
			{
				// Prepare GUI skins
				var windowStyle = new GUIStyle(GUI.skin.window);
				GUI.contentColor = Color.white;

				// Create console background, draw it with the background color
				GUI.color = ConsoleColors.BackgroundColor;
				GUI.Box(consoleRect, GUIContent.none, windowStyle);

				// Use the foreground color from now on
				GUI.color = ConsoleColors.ContentColor;

				// Draw console title
				GUILayout.Label(Application.productName + " Developer Console");

				// Scroll view for content
				_scrollAmount = GUILayout.BeginScrollView(_scrollAmount);
				{
					GUILayout.Label(_currentOutput.ToString());
				}
				GUILayout.EndScrollView();

				// Set name of the input field element
				const string inputFieldName = "InputField";
				GUI.SetNextControlName(inputFieldName);

				// Input field with submit button
				GUILayout.BeginHorizontal();
				{
					var newInput = _currentInput;
					newInput = GUILayout.TextField(newInput);
					if (GUILayout.Button("Submit", GUILayout.MaxWidth(64)))
					{
						_shouldSubmit = true;
					}

					// And focus the input field
					GUI.FocusControl(inputFieldName);

					// This will prevent from writing garbage into input
					if (!_shouldSubmit && !_shouldToggle)
						_currentInput = newInput;
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		/// <summary>
		/// When a message is logged, it is appended to the output.
		/// </summary>
		protected override void OnLogMessageReceived(string condition, string stackTrace, LogType type)
		{
			// We can skip adding empty strings..
			if (string.IsNullOrEmpty(condition))
				return;

			// Begin markup
			_currentOutput.Append("<color=#");

			// Choose color per type
			switch (type)
			{
				case LogType.Log:
					_currentOutput.Append(ColorUtility.ToHtmlStringRGB(ConsoleColors.LogColor));
					break;
				case LogType.Warning:
					_currentOutput.Append(ColorUtility.ToHtmlStringRGB(ConsoleColors.WarningColor));
					break;
				case LogType.Error:
				case LogType.Assert:
				case LogType.Exception:
					_currentOutput.Append(ColorUtility.ToHtmlStringRGB(ConsoleColors.ErrorColor));
					break;
			}

			// Close the markup
			_currentOutput.Append(">");

			// Write the message
			_currentOutput.Append(condition);

			// Finish the color
			_currentOutput.Append("</color>");

			// Append newline if none
			if (!condition.EndsWith(Environment.NewLine))
				_currentOutput.Append(Environment.NewLine);

			// Scroll the window
			_scrollAmount += new Vector2(0, 1024.0f);
		}

		/// <summary>
		/// Clears the output content.
		/// </summary>
		protected override void ClearConsole()
		{
			_currentOutput.Clear();
			base.ClearConsole();
		}

		/// <summary>
		/// Submits current input to the console.
		/// </summary>
		private void Submit()
		{
			// Echo user input
			if (EchoInput)
				Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(ConsoleColors.EchoColor)}>> {_currentInput}</color>");

			// Execute current command
			Execute(_currentInput);

			// Clear input
			_currentInput = string.Empty;
		}

		/// <summary>
		/// Returns the rect for this console.
		/// </summary>
		/// <returns></returns>
		private Rect GetConsoleRect()
		{
			// Calculate relative or absolute scale based on settings
			float width = (IsRelative) ? Mathf.Clamp(Width, 0.0f, 1.0f) * Screen.width : Width;
			float height = (IsRelative) ? Mathf.Clamp(Height, 0.0f, 1.0f) * Screen.height : Height;

			// Calculate relative or absolute position based on settings
			float xPosition = (IsRelative) ? Mathf.Clamp(Position.x, 0.0f, 1.0f) * Screen.width : Position.x;
			float yPosition = (IsRelative) ? Mathf.Clamp(Position.y, 0.0f, 1.0f) * Screen.height : Position.y;

			// Return the rect
			return new Rect(xPosition, yPosition, width, height);
		}
	}
}