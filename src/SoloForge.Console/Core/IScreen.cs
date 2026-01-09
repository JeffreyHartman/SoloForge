namespace SoloForge.Console.Core;

/// <summary>
/// Defines the contract for a screen in the application.
/// Screens are responsible for rendering UI and handling user input.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Runs the screen, handling all UI rendering and user interaction.
    /// </summary>
    /// <returns>
    /// The next screen to navigate to, or null to exit/return to previous context.
    /// </returns>
    IScreen? Run();
}
