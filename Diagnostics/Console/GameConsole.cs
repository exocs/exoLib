using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;

using exoLib.Collections.Generic;

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
		/// Internal GUI input field element name.
		/// </summary>
		private const string GUI_INPUT_FIELD = "inputField";
		/// <summary>
		/// Invalid (none) index.
		/// </summary>
		private const int INVALID_INDEX = -1;
		/// <summary>
		/// Maximum number of items kept in the command history.
		/// </summary>
		private const int COMMAND_HISTORY_MAX = 8;
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
		/// Input from last time, so we can see if it changed.
		/// </summary>
		private string _lastInput;
		/// <summary>
		/// Current suggestion (if any).
		/// </summary>
		private string _currentSuggestion = string.Empty;
		/// <summary>
		/// Current suggestion index (if any) or -1 (if none).
		/// </summary>
		private int _currentSuggestionIndex = -1;
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
		/// Do we want to apply suggestion?
		/// </summary>
		private bool _applySuggestion = false;
		/// <summary>
		/// Scroll amount in the output field.
		/// </summary>
		private Vector2 _scrollAmount;
		/// <summary>
		/// Suggestions for autocompletion.
		/// </summary>
		private readonly List<string> _suggestions = new List<string>();
		/// <summary>
		/// Window title GUI style.
		/// </summary>
		private GUIStyle _titleStyle;
		/// <summary>
		/// Main window GUI style.
		/// </summary>
		private GUIStyle _windowStyle;
		/// <summary>
		/// History of executed commands.
		/// </summary>
		private CircularBuffer<string> _commandHistory = new CircularBuffer<string>(COMMAND_HISTORY_MAX);
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

			// Submit command
			if (_shouldSubmit)
			{
				// If we have a suggestion, apply and clear what we have
				if (!string.IsNullOrWhiteSpace(_currentSuggestion))
				{
					_currentInput = _currentSuggestion;
					_currentSuggestion = string.Empty;
					_currentSuggestionIndex = INVALID_INDEX;
				}

				// If string is empty or spaces only, clear it
				// If not, submit it!
				if (string.IsNullOrWhiteSpace(_currentInput))
					_currentInput = string.Empty;
				else
				{
					// Store current command to history
					_commandHistory.Add(_currentInput);

					// Submit the item
					Submit();
				}

				_shouldSubmit = false;
			}

			// Clear input field
			if (_shouldClear)
			{
				_currentInput = string.Empty;
				_shouldClear = false;
			}

			// Clear suggestions if no input
			if (string.IsNullOrWhiteSpace(_currentInput))
			{
				_suggestions.Clear();

				// Use the history instead :)
				if (!_commandHistory.IsEmpty)
					_suggestions.AddRange(_commandHistory.Peek(_commandHistory.Count));
			}

			// Parent update
			base.Update();
		}
		/// <summary>
		/// Draws the console GUI.
		/// </summary>
		private void OnGUI()
		{
			// Initialize styles.
			// Has to be done from inside of GUI.
			{
				if (_windowStyle == null)
				{
					_windowStyle = new GUIStyle(GUI.skin.window);
				}
				if (_titleStyle == null)
				{
					_titleStyle = new GUIStyle(GUI.skin.label)
					{
						fontSize = 12,
						clipping = TextClipping.Overflow
					};
				}
			}

			// Console is closed, we will not draw anything
			if (!IsOpen)
				return;

			// Should we move the caret?
			bool moveCaret = false;
			// Moves caret to end of current input field that keyboard has focus in
			void moveCaretToEnd(string text)
			{
				var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (textEditor != null)
				{
					textEditor.text = text;
					textEditor.cursorIndex = text.Length;
					textEditor.SelectNone();
				}
			}

			// If we had a suggestion and we backspaced, undo that change
			bool undoBackspace = false;

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

				if (currentEvent.keyCode == KeyCode.Space)
				{
					_applySuggestion = true;

					// If we don't have a suggestion picked, yet
					// there could be options, try picking the first one
					if (_currentSuggestionIndex == INVALID_INDEX && _currentInput.Length > 0 && _suggestions.Count > 0)
					{
						_currentSuggestionIndex = 0;
						_currentSuggestion = _suggestions[0];
						_shouldClear = false;
						moveCaret = true;
					}
				}

				// browse suggestion (down)
				if (currentEvent.keyCode == KeyCode.DownArrow)
				{
					// Select next suggestion
					_currentSuggestionIndex = (int)Mathf.Repeat(_currentSuggestionIndex + 1, _suggestions.Count);
					moveCaret = true;
				}

				// browse suggestion (up)
				if (currentEvent.keyCode == KeyCode.UpArrow)
				{
					// Select previous suggestion
					_currentSuggestionIndex = (int)Mathf.Repeat(_currentSuggestionIndex - 1, _suggestions.Count);
					moveCaret = true;
				}

				// Clear suggestions
				if (currentEvent.keyCode == KeyCode.Backspace || currentEvent.keyCode == KeyCode.Escape)
				{
					// If we're removing a suggestion, revert changes done in current input
					if (!string.IsNullOrWhiteSpace(_currentSuggestion))
					{
						// Revert backspace
						undoBackspace = true;

						// And clear the suggestions
						_currentSuggestionIndex = INVALID_INDEX;
						_currentSuggestion = string.Empty;

						// We will not clear the input field,
						// we will only remove the suggestion
						_shouldClear = false;

						// Move the caret to end too
						moveCaret = true;
					}
				}
			}

			// Get rect for console
			var consoleRect = GetConsoleRect();

			// Draw background
			// Prepare GUI skins
			GUI.contentColor = Color.white;

			// Create console background, draw it with the background color
			GUI.color = ConsoleColors.BackgroundColor;
			GUI.Box(consoleRect, GUIContent.none, _windowStyle);

			// Start drawing the console window
			GUILayout.BeginArea(consoleRect);
			{
				// Use the foreground color from now on
				GUI.color = ConsoleColors.ContentColor;
				// Write the console title
				GUILayout.Label(Application.productName + " Development Console", _titleStyle, GUILayout.Height(14));

				// Scroll view for content (console output)
				_scrollAmount = GUILayout.BeginScrollView(_scrollAmount);
				{
					GUILayout.Label(_currentOutput.ToString());
				}
				GUILayout.EndScrollView();

				// Set name of the input field element
				GUI.SetNextControlName(GUI_INPUT_FIELD);

				// Input field with submit button
				GUILayout.BeginHorizontal();
				{
					// Fetch new input
					var newInput = _currentInput;

					// Select suggestion, if there are available ones
					int suggestionsCount = _suggestions.Count;
					if (_currentSuggestionIndex > -1 && suggestionsCount > 0 && _currentSuggestionIndex < suggestionsCount)
						_currentSuggestion = _suggestions[_currentSuggestionIndex].ToLowerInvariant();
					else
						_currentSuggestion = string.Empty;

					// Write suggestion if any, otherwise update text
					if (!string.IsNullOrWhiteSpace(_currentSuggestion))
					{
						GUILayout.TextArea(_currentSuggestion);
						moveCaret = true;

						if (_applySuggestion)
						{
							newInput = _currentSuggestion;
							_currentSuggestionIndex = -1;
							_currentSuggestion = string.Empty;
							_applySuggestion = false;
						}
					}
					else
						newInput = GUILayout.TextField(newInput);


					// Submit on enter
					if (GUILayout.Button("Submit", GUILayout.MaxWidth(64)))
						_shouldSubmit = true;

					// And focus the input field
					GUI.FocusControl(GUI_INPUT_FIELD);

					// If we were undoing backSpace, use previous input
					if (undoBackspace)
					{
						newInput = _lastInput;
						moveCaret = true;
					}

					// This will prevent from writing garbage into input
					if (!_shouldSubmit && !_shouldToggle)
						_currentInput = newInput;

					// Move to end if we are supposed to
					if (moveCaret)
					{
						if (!string.IsNullOrWhiteSpace(_currentSuggestion))
							moveCaretToEnd(_currentSuggestion);
						else
							moveCaretToEnd(_currentInput);
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();

			// if not empty, draw autocompletion
			if (_currentInput.Length > 0)
				DrawSuggestions(consoleRect, _currentInput);

			// Store last input
			_lastInput = _currentInput;
		}
		/// <summary>
		/// Draws and handles input for auto completion.
		/// </summary>
		/// <param name="consoleRect">Rect of the console to append to.</param>
		/// <param name="input">Input text to find suggestions for</param>
		private void DrawSuggestions(Rect consoleRect, string input)
		{
			int count = _suggestions.Count;
			if (_lastInput != input)
			{
				// Pick suggestions
				// TODO: Does not have to be picked all the time
				_suggestions.Clear();
				count = GetAutocompletionSuggestions(input, _suggestions);
			}

			// Dont draw anything
			if (count == 0)
				return;

			const int elementHeight = 24;
			// Draw suggestions
			var autoCompletionRect = new Rect(consoleRect.min.x, consoleRect.max.y + 1, consoleRect.width, count * elementHeight);
			// Draw background
			GUI.Box(autoCompletionRect, GUIContent.none);

			// Draw the content
			GUI.color = ConsoleColors.ContentColor;
			// Add some margin
			const int margin = 2;
			autoCompletionRect.x += margin;
			autoCompletionRect.width -= margin;

			// Draw the layout
			GUILayout.BeginArea(autoCompletionRect);
			{
				GUILayout.BeginVertical();
				{
					// Draw individual suggestions
					for (int i = 0; i < count; i++)
					{
						var suggestion = _suggestions[i].ToLowerInvariant();
						GUILayout.Label(suggestion, GUILayout.MaxHeight(elementHeight));
					}
				}
				GUILayout.EndVertical();
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