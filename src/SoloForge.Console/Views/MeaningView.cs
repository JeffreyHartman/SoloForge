using System.Collections.ObjectModel;
using Terminal.Gui;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// View for the Discovering Meaning submenu with quick rolls, element browser, and fusion rolls.
/// </summary>
public class MeaningView : View
{
    private readonly Session _session;
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly JournalView _journalView;

    private readonly FrameView _menuFrame;
    private readonly FrameView _resultFrame;
    private readonly Label _resultLabel;
    private readonly Label _wordsLabel;

    private MeaningResult? _lastResult;
    private string? _lastTableId1;
    private string? _lastTableId2;
    private string? _lastContext;

    public MeaningView(
        Session session,
        HistoryService historyService,
        CampaignService campaignService,
        JournalView journalView)
    {
        _session = session;
        _historyService = historyService;
        _campaignService = campaignService;
        _journalView = journalView;

        // Menu frame
        _menuFrame = new FrameView
        {
            Title = "Select an Option",
            X = Pos.Center(),
            Y = 1,
            Width = 35,
            Height = 12,
            CanFocus = true
        };

        var y = 0;
        var buttons = new (string label, Action action)[]
        {
            ("[A] Action (Quick Roll)", () => ShowMeaning("Action")),
            ("[D] Description (Quick Roll)", () => ShowMeaning("Description")),
            ("[E] Element Tables", ShowElementBrowser),
            ("[F] Fusion Roll", ShowFusionRoll),
            ("[Q] Quick Sets", ShowQuickSets)
        };

        foreach (var (label, action) in buttons)
        {
            var btn = new Button
            {
                X = 1,
                Y = y,
                Text = label,
                CanFocus = true
            };
            btn.Accepting += (s, e) => action();
            _menuFrame.Add(btn);
            y++;
        }

        // Result frame
        _resultFrame = new FrameView
        {
            Title = "Result",
            X = Pos.Center(),
            Y = Pos.Bottom(_menuFrame) + 1,
            Width = 45,
            Height = 8,
            Visible = false,
            CanFocus = true
        };

        _resultLabel = new Label
        {
            X = Pos.Center(),
            Y = 1,
            Text = ""
        };

        _wordsLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = ""
        };

        var rerollButton = new Button
        {
            X = 1,
            Y = 5,
            Text = "[R] Re-roll",
            CanFocus = true
        };
        rerollButton.Accepting += (s, e) => Reroll();
        rerollButton.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                Reroll();
                e.Handled = true;
            }
        };

        _resultFrame.Add(_resultLabel, _wordsLabel, rerollButton);

        Add(_menuFrame, _resultFrame);

        _menuFrame.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
    }

    private void ShowMeaning(string type)
    {
        // Prompt for context
        var context = PromptForContext($"What are you trying to understand? (optional)");

        var result = type == "Action"
            ? MeaningEngine.GenerateAction()
            : MeaningEngine.GenerateDescription();

        _lastResult = result;
        _lastTableId1 = null;
        _lastTableId2 = null;
        _lastContext = context;

        LogAndDisplay(result, type, context);
    }

    private void ShowElementBrowser()
    {
        var tables = TableService.Instance.ElementTables.ToList();
        if (tables.Count == 0)
        {
            MessageBox.ErrorQuery("Error", "No element tables found in data/elements/", "OK");
            return;
        }

        var context = PromptForContext("What are you looking for? (optional)");

        var dialog = new Dialog
        {
            Title = "Select Element Table",
            Width = Dim.Percent(80),
            Height = Dim.Percent(80)
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var tableNames = new ObservableCollection<string>(
            tables.Select(t => $"{t.Category} > {t.DisplayName}")
        );
        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };
        listView.SetSource(tableNames);
        listView.TabStop = TabBehavior.TabStop;
        listView.CanFocus = true;

        TableInfo? selectedTable = null;
        listView.OpenSelectedItem += (s, e) =>
        {
            selectedTable = tables[listView.SelectedItem];
            Application.RequestStop();
        };
        listView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                selectedTable = tables[listView.SelectedItem];
                Application.RequestStop();
                e.Handled = true;
            }
        };
        var okButton = new Button { Text = "Select", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            selectedTable = tables[listView.SelectedItem];
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(listView);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);

        listView.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
        Application.Run(dialog);

        if (selectedTable != null)
        {
            var result = MeaningEngine.GenerateFromTable(selectedTable.Id, selectedTable.DisplayName);
            _lastResult = result;
            _lastTableId1 = selectedTable.Id;
            _lastTableId2 = null;
            _lastContext = context;

            LogAndDisplay(result, selectedTable.DisplayName, context);
        }
    }

    private void ShowFusionRoll()
    {
        var allTables = TableService.Instance.AvailableTables.ToList();
        var context = PromptForContext("What are you combining meanings for? (optional)");

        // Select first table
        var table1 = SelectTable(allTables, "Select First Table");
        if (table1 == null) return;

        // Select second table
        var table2 = SelectTable(allTables, "Select Second Table");
        if (table2 == null) return;

        var result = MeaningEngine.GenerateFusion(table1.Id, table2.Id);
        _lastResult = result;
        _lastTableId1 = table1.Id;
        _lastTableId2 = table2.Id;
        _lastContext = context;

        LogAndDisplay(result, $"Fusion: {table1.DisplayName} + {table2.DisplayName}", context);
    }

    private TableInfo? SelectTable(List<TableInfo> tables, string title)
    {
        var dialog = new Dialog
        {
            Title = title,
            Width = Dim.Percent(80),
            Height = Dim.Percent(80)
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var tableNames = new ObservableCollection<string>(
            tables.Select(t =>
                t.IsElement ? $"[Element] {t.Category} > {t.DisplayName}" : $"[Core] {t.DisplayName}"
            )
        );

        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };
        listView.SetSource(tableNames);
        listView.TabStop = TabBehavior.TabStop;
        listView.CanFocus = true;

        TableInfo? selectedTable = null;
        listView.OpenSelectedItem += (s, e) =>
        {
            selectedTable = tables[listView.SelectedItem];
            Application.RequestStop();
        };
        listView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                selectedTable = tables[listView.SelectedItem];
                Application.RequestStop();
                e.Handled = true;
            }
        };
        var okButton = new Button { Text = "Select", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            selectedTable = tables[listView.SelectedItem];
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(listView);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);

        listView.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
        Application.Run(dialog);

        return selectedTable;
    }

    private void ShowQuickSets()
    {
        var quickSets = QuickSetService.Instance.QuickSets;
        if (quickSets.Count == 0)
        {
            MessageBox.ErrorQuery("Error", "No quick sets found. Check data/quicksets.json", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = "Select Quick Set",
            Width = Dim.Percent(80),
            Height = Dim.Percent(80)
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var setNames = new ObservableCollection<string>(
            quickSets.Select(q => $"{q.Name} - {q.Description}")
        );

        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };
        listView.SetSource(setNames);
        listView.TabStop = TabBehavior.TabStop;
        listView.CanFocus = true;

        QuickSet? selectedSet = null;
        listView.OpenSelectedItem += (s, e) =>
        {
            selectedSet = quickSets[listView.SelectedItem];
            Application.RequestStop();
        };
        listView.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                selectedSet = quickSets[listView.SelectedItem];
                Application.RequestStop();
                e.Handled = true;
            }
        };
        var okButton = new Button { Text = "Generate", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            selectedSet = quickSets[listView.SelectedItem];
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(listView);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);

        listView.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
        Application.Run(dialog);

        if (selectedSet != null)
        {
            ShowQuickSetResult(selectedSet);
        }
    }

    private void ShowQuickSetResult(QuickSet quickSet)
    {
        try
        {
            var result = QuickSetService.Instance.Generate(quickSet);

            _historyService.AddEntry(
                LogType.Meaning,
                $"{quickSet.Name} Generated",
                null,
                result.ToDisplayDetails()
            );
            _campaignService.Save();

            var entry = _historyService.Entries.LastOrDefault();
            if (entry != null)
            {
                _journalView.AppendEntry(entry);
            }

            // Build result text
            var resultText = string.Join("\n", result.Results.Select(r => $"{r.Label}: {r.Combined}"));

            var resultDialog = new Dialog
            {
                Title = quickSet.Name,
                Width = Dim.Percent(70),
                Height = Dim.Percent(60)
            };

            var label = new Label
            {
                X = 1,
                Y = 1,
                Text = resultText
            };

            var regenButton = new Button { Text = "Regenerate" };
            regenButton.Accepting += (s, e) =>
            {
                Application.RequestStop();
                ShowQuickSetResult(quickSet);
            };

            var closeButton = new Button { Text = "Close", IsDefault = true };
            closeButton.Accepting += (s, e) => Application.RequestStop();

            resultDialog.Add(label);
            resultDialog.AddButton(regenButton);
            resultDialog.AddButton(closeButton);

            Application.Run(resultDialog);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to generate quick set: {ex.Message}", "OK");
        }
    }

    private void Reroll()
    {
        if (_lastResult == null) return;

        MeaningResult result;
        if (_lastResult.IsFusion && _lastTableId1 != null && _lastTableId2 != null)
        {
            result = MeaningEngine.GenerateFusion(_lastTableId1, _lastTableId2);
        }
        else if (_lastTableId1 != null)
        {
            result = MeaningEngine.GenerateFromTable(_lastTableId1, _lastResult.TableName);
        }
        else if (_lastResult.TableName == "Action")
        {
            result = MeaningEngine.GenerateAction();
        }
        else
        {
            result = MeaningEngine.GenerateDescription();
        }

        _lastResult = result;
        LogAndDisplay(result, result.TableName + " (Re-roll)", _lastContext);
    }

    private void LogAndDisplay(MeaningResult result, string tableName, string? context)
    {
        _historyService.AddEntry(
            LogType.Meaning,
            result.Combined,
            context,
            $"Table: {tableName}"
        );
        _campaignService.Save();

        var entry = _historyService.Entries.LastOrDefault();
        if (entry != null)
        {
            _journalView.AppendEntry(entry);
        }

        // Update display
        _resultLabel.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black)
        };
        _resultLabel.Text = result.Combined;

        _wordsLabel.Text = $"Word 1: {result.Word1}\nWord 2: {result.Word2}";

        _resultFrame.Title = tableName;
        _resultFrame.Visible = true;
        SetNeedsLayout();
    }

    private string? PromptForContext(string prompt)
    {
        var dialog = new Dialog
        {
            Title = "Context",
            Width = 60,
            Height = 8
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var label = new Label
        {
            X = 1,
            Y = 1,
            Text = prompt
        };

        var field = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };

        string? result = null;

        var okButton = new Button { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            result = field.Text.ToString();
            if (string.IsNullOrWhiteSpace(result)) result = null;
            Application.RequestStop();
        };

        var skipButton = new Button { Text = "Skip" };
        skipButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(label, field);
        dialog.AddButton(okButton);
        dialog.AddButton(skipButton);
        field.SetFocus();

        Application.Run(dialog);

        return result;
    }
}
