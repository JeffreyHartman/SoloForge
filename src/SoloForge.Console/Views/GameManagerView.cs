using System.Collections.ObjectModel;
using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// View for managing campaigns (create, switch, delete).
/// </summary>
public class GameManagerView : View
{
    private readonly CampaignService _campaignService;
    private readonly JournalView _journalView;
    private readonly SoloForgeApp _app;

    private readonly Label _infoLabel;

    public GameManagerView(CampaignService campaignService, JournalView journalView, SoloForgeApp app)
    {
        _campaignService = campaignService;
        _journalView = journalView;
        _app = app;

        // Current campaign info
        var infoFrame = new FrameView
        {
            Title = "Current Campaign",
            X = Pos.Center(),
            Y = 1,
            Width = 50,
            Height = 8
        };

        _infoLabel = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        infoFrame.Add(_infoLabel);

        // Menu buttons
        var menuFrame = new FrameView
        {
            Title = "Options",
            X = Pos.Center(),
            Y = Pos.Bottom(infoFrame) + 1,
            Width = 35,
            Height = 8,
            CanFocus = true
        };

        var newBtn = new Button
        {
            X = 1,
            Y = 0,
            Text = "[N] New Campaign",
            CanFocus = true
        };
        newBtn.Accepting += (s, e) => CreateCampaign();

        var switchBtn = new Button
        {
            X = 1,
            Y = 2,
            Text = "[S] Switch Campaign",
            CanFocus = true
        };
        switchBtn.Accepting += (s, e) => SwitchCampaign();

        var deleteBtn = new Button
        {
            X = 1,
            Y = 4,
            Text = "[D] Delete Campaign",
            CanFocus = true
        };
        deleteBtn.Accepting += (s, e) => DeleteCampaign();

        menuFrame.Add(newBtn, switchBtn, deleteBtn);

        Add(infoFrame, menuFrame);

        RefreshInfo();

        // Keyboard shortcuts
        KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.N:
                    CreateCampaign();
                    e.Handled = true;
                    break;
                case KeyCode.S:
                    SwitchCampaign();
                    e.Handled = true;
                    break;
                case KeyCode.D:
                    DeleteCampaign();
                    e.Handled = true;
                    break;
            }
        };

        newBtn.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
    }

    private void RefreshInfo()
    {
        var current = _campaignService.CurrentCampaign;
        if (current != null)
        {
            _infoLabel.Text =
                $"Name:        {current.Name}\n" +
                $"Created:     {current.CreatedAt:MMM d, yyyy}\n" +
                $"Last Played: {current.LastPlayed:MMM d, yyyy h:mm tt}\n" +
                $"Entries:     {current.History.Count}";
        }
        else
        {
            _infoLabel.Text = "No campaign loaded";
        }
        SetNeedsLayout();
    }

    private void CreateCampaign()
    {
        var dialog = new Dialog
        {
            Title = "New Campaign",
            Width = 50,
            Height = 10
        };

        var nameLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Campaign Name:"
        };

        var nameField = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };

        var okButton = new Button { Text = "Create", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            var name = nameField.Text.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                _campaignService.CreateNew(name);
                _journalView.ReloadForCampaign();
                _app.RefreshSessionInfo();
                RefreshInfo();
            }
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(nameLabel, nameField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        nameField.SetFocus();

        Application.Run(dialog);
    }

    private void SwitchCampaign()
    {
        var campaigns = _campaignService.ListCampaigns().ToList();

        if (campaigns.Count == 0)
        {
            MessageBox.Query("Switch Campaign", "No campaigns found.", "OK");
            return;
        }

        if (campaigns.Count == 1)
        {
            MessageBox.Query("Switch Campaign", "Only one campaign exists. Create another to switch.", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = "Switch Campaign",
            Width = Dim.Percent(70),
            Height = Dim.Percent(60)
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var campaignNames = new ObservableCollection<string>(campaigns.Select(c =>
        {
            var current = _campaignService.CurrentCampaign?.Id == c.Id ? " (current)" : "";
            return $"{c.Name}{current} - {c.LastPlayed:MMM d}";
        }));

        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };
        listView.SetSource(campaignNames);

        CampaignData? selected = null;
        listView.OpenSelectedItem += (s, e) =>
        {
            selected = campaigns[listView.SelectedItem];
            Application.RequestStop();
        };

        var okButton = new Button { Text = "Switch", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            selected = campaigns[listView.SelectedItem];
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(listView);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);

        Application.Run(dialog);

        if (selected != null && selected.Id != _campaignService.CurrentCampaign?.Id)
        {
            _campaignService.Load(selected.Id);
            _journalView.ReloadForCampaign();
            _app.RefreshSessionInfo();
            RefreshInfo();
        }
    }

    private void DeleteCampaign()
    {
        var campaigns = _campaignService.ListCampaigns().ToList();

        if (campaigns.Count == 0)
        {
            MessageBox.Query("Delete Campaign", "No campaigns found.", "OK");
            return;
        }

        if (campaigns.Count == 1)
        {
            MessageBox.Query("Delete Campaign", "Cannot delete the only campaign. Create another first.", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = "Delete Campaign",
            Width = Dim.Percent(70),
            Height = Dim.Percent(60)
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var campaignNames = new ObservableCollection<string>(campaigns.Select(c =>
        {
            var current = _campaignService.CurrentCampaign?.Id == c.Id ? " (current)" : "";
            return $"{c.Name}{current} - {c.LastPlayed:MMM d}";
        }));

        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };
        listView.SetSource(campaignNames);

        CampaignData? selected = null;
        listView.OpenSelectedItem += (s, e) =>
        {
            selected = campaigns[listView.SelectedItem];
            Application.RequestStop();
        };

        var deleteButton = new Button { Text = "Delete" };
        deleteButton.Accepting += (s, e) =>
        {
            selected = campaigns[listView.SelectedItem];
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel", IsDefault = true };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(listView);
        dialog.AddButton(deleteButton);
        dialog.AddButton(cancelButton);

        Application.Run(dialog);

        if (selected != null)
        {
            var confirm = MessageBox.Query("Confirm Delete",
                $"Delete campaign \"{selected.Name}\"?\nThis cannot be undone.", "Delete", "Cancel");

            if (confirm == 0)
            {
                var wasCurrentCampaign = selected.Id == _campaignService.CurrentCampaign?.Id;
                _campaignService.Delete(selected.Id);

                if (wasCurrentCampaign)
                {
                    var remaining = _campaignService.ListCampaigns().FirstOrDefault();
                    if (remaining != null)
                    {
                        _campaignService.Load(remaining.Id);
                    }
                    else
                    {
                        _campaignService.CreateNew("Default Campaign");
                    }
                    _journalView.ReloadForCampaign();
                }

                _app.RefreshSessionInfo();
                RefreshInfo();
            }
        }
    }
}
