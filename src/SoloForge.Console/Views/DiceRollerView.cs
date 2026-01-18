using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// View for rolling arbitrary dice expressions.
/// </summary>
public class DiceRollerView : View
{
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly JournalView _journalView;

    private readonly TextField _expressionField;
    private readonly FrameView _resultFrame;
    private readonly Label _resultLabel;
    private readonly Label _breakdownLabel;
    private readonly Label _historyLabel;

    private readonly List<string> _rollHistory = [];

    public DiceRollerView(
        HistoryService historyService,
        CampaignService campaignService,
        JournalView journalView)
    {
        _historyService = historyService;
        _campaignService = campaignService;
        _journalView = journalView;

        // Expression input
        var inputLabel = new Label
        {
            X = 1,
            Y = 0,
            Text = "Enter dice expression (e.g., 2d6+1 or 1d%):"
        };

        _expressionField = new TextField
        {
            X = 1,
            Y = 1,
            Width = 30,
            Height = 1,
            CanFocus = true
        };
        _expressionField.KeyDown += OnExpressionKeyDown;

        var rollButton = new Button
        {
            X = 32,
            Y = 1,
            Text = "Roll",
            IsDefault = true,
            CanFocus = true
        };
        rollButton.Accepting += (s, e) => RollDice();

        // Common dice buttons
        var commonFrame = new FrameView
        {
            Title = "Common Dice",
            X = 1,
            Y = 3,
            Width = 45,
            Height = 4
        };

        var commonDice = new[] { "d4", "d6", "d8", "d10", "d12", "d20", "d100" };
        var x = 0;
        foreach (var die in commonDice)
        {
            var btn = new Button
            {
                X = x,
                Y = 0,
                Text = die,
                CanFocus = true
            };
            var dieExpr = die;
            btn.Accepting += (s, e) =>
            {
                _expressionField.Text = "1" + dieExpr;
                RollDice();
            };
            commonFrame.Add(btn);
            x += die.Length + 4;
        }

        // Result frame
        _resultFrame = new FrameView
        {
            Title = "Result",
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Height = 6,
            Visible = false
        };

        _resultLabel = new Label
        {
            X = Pos.Center(),
            Y = 0,
            Text = ""
        };

        _breakdownLabel = new Label
        {
            X = 1,
            Y = 2,
            Text = ""
        };

        _resultFrame.Add(_resultLabel, _breakdownLabel);

        // History frame
        var historyFrame = new FrameView
        {
            Title = "Recent Rolls",
            X = 1,
            Y = 15,
            Width = Dim.Fill(2),
            Height = Dim.Fill()
        };

        _historyLabel = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = ""
        };

        historyFrame.Add(_historyLabel);

        Add(inputLabel, _expressionField, rollButton, commonFrame, _resultFrame, historyFrame);

        _expressionField.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
    }

    private void OnExpressionKeyDown(object? sender, Key e)
    {
        if (e == Key.Enter)
        {
            RollDice();
            e.Handled = true;
        }
    }

    private void RollDice()
    {
        var input = _expressionField.Text.ToString();
        if (string.IsNullOrWhiteSpace(input)) return;

        if (!DiceExpression.TryParse(input, out var expression, out var error))
        {
            MessageBox.ErrorQuery("Invalid Expression", error ?? "Could not parse dice expression", "OK");
            return;
        }

        var result = DiceRoller.Instance.Roll(expression!);
        var summary = result.Summary;
        var breakdown = result.BuildBreakdown();

        // Log result
        _historyService.AddEntry(LogType.DiceRoll, result.Total.ToString(), expression!.ToDisplayString(), breakdown);
        _campaignService.Save();

        // Update journal
        var entry = _historyService.Entries.LastOrDefault();
        if (entry != null)
        {
            _journalView.AppendEntry(entry);
        }

        // Display result
        _resultLabel.ColorScheme = UiThemes.Instance.ActiveAccent;
        _resultLabel.Text = $">>> {summary} <<<";

        _breakdownLabel.Text = string.IsNullOrWhiteSpace(breakdown) ? "" : breakdown;

        _resultFrame.Visible = true;

        // Add to history
        _rollHistory.Insert(0, $"{expression.ToDisplayString()} = {result.Total}");
        if (_rollHistory.Count > 10)
        {
            _rollHistory.RemoveAt(_rollHistory.Count - 1);
        }
        _historyLabel.Text = string.Join("\n", _rollHistory);

        // Clear input for next roll
        _expressionField.Text = "";
        _expressionField.SetFocus();

        SetNeedsLayout();
    }
}
