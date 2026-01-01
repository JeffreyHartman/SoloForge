using Spectre.Console;

while (true)
{
    AnsiConsole.Clear();

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What do you want to do?")
            .AddChoices(new[] { "Fate Check", "Random Event", "NPC Generator", "Dice Roller", "Settings", "Exit" })   
    );

    if (choice == "Exit")
    {
        AnsiConsole.WriteLine("Goodbye!");
        break;
    }
    else
    {
        AnsiConsole.WriteLine("Not implemented yet...");
        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}
