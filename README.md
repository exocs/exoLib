# exoLib
https://github.com/exocs/exoLib

## What is exoLib:
ExoLib is a library that provides various utilities and readymade solutions for usage within the Unity Engine. See https://unity.com/ for more.

## Current features
### TaskManager
Allows executing code from anywhere during Unity update events via simple API.
##### Example usage:
```
TaskManager.Run(() =>
{
Debug.Log("I will be performed during LateUpdate");
}, TaskPerformWhen.LateUpdate);
```

### DebugConsole
Allows the usage of a in-game development console via simple API. The console is automagically managed and does not require any prefabs or additional scripts.
The console is supported both in standalone, editor and headless builds.
In editor and standalone the console can be opened via pressing the backquote '`' key. In headless mode the console is always present, as long as the instance is created.

##### Example usage:
```
// This is the only line of code that has exist for the console to work
DebugConsole console = DebugConsole.Instance;

// Registering commands is easy
console.RegisterCommand("printMessage", (arguments) =>
{
  Debug.Log(arguments[0]);
}, 1, "Prints the provided message. Requires at least 1 argument.");
```
