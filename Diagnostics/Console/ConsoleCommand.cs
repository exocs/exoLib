namespace exoLib.Diagnostics.Console
{
	/// <summary>
	/// Functions that can be hooked into the console should always match this delegate.
	/// </summary>
	/// <param name="arguments">List of arguments</param>
	/// <returns></returns>
	public delegate void ConsoleFunction(params string[] arguments);

	/// <summary>
	/// Wrapper for individual console commands.
	/// </summary>
	public sealed class ConsoleCommand
	{
		/// <summary>
		/// Function we will invoke when command is executed.
		/// </summary>
		public readonly ConsoleFunction Function;
		/// <summary>
		/// Short summary of this function.
		/// </summary>
		public readonly string Summary;
		/// <summary>
		/// Description of this function.
		/// </summary>
		public readonly string Description;
		/// <summary>
		/// The number of arguments required by this command.
		/// </summary>
		public readonly uint MinimumArgumentsCount;
		/// <summary>
		/// Create the wrapper from provided information
		/// </summary>
		public ConsoleCommand(ConsoleFunction function, uint minimumArgumentsCount = 0, string summary = "No description provided.", string description = null)
		{
			Function = function;
			MinimumArgumentsCount = minimumArgumentsCount;
			Summary = summary;
			Description = description;
		}
		/// <summary>
		/// Hiding the default constructor.
		/// </summary>
		private ConsoleCommand()
		{
		}
	}
}