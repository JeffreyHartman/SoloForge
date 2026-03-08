using System.Text.Json;
using Serilog;
using SoloForge.Console.Core;
using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

/// <summary>
/// Orchestrates campaign persistence across Session, AdventureStateManager, and HistoryService.
/// </summary>
public sealed class CampaignService
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly HistoryService _historyService;
    private readonly ILogger _log = AppLogger.ForContext<CampaignService>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// The currently loaded campaign.
    /// </summary>
    public CampaignData? CurrentCampaign { get; private set; }

    /// <summary>
    /// Directory where campaign saves are stored.
    /// </summary>
    public string SavesDirectory { get; }

    /// <summary>
    /// Path to the global settings file.
    /// </summary>
    public string SettingsPath => Path.Combine(SavesDirectory, "settings.json");

    public CampaignService(Session session, AdventureStateManager stateManager, HistoryService historyService)
        : this(session, stateManager, historyService, null)
    {
    }

    public CampaignService(Session session, AdventureStateManager stateManager, HistoryService historyService, string? savesDirectory)
    {
        _session = session;
        _stateManager = stateManager;
        _historyService = historyService;

        // Find saves directory relative to app
        SavesDirectory = string.IsNullOrWhiteSpace(savesDirectory)
            ? FindOrCreateSavesDirectory()
            : savesDirectory;

        Directory.CreateDirectory(SavesDirectory);
        _log.Debug("CampaignService initialized with saves directory: {Path}", SavesDirectory);
    }

    /// <summary>
    /// Initializes the campaign system on startup.
    /// Loads the last played campaign or creates a default one.
    /// </summary>
    public void Initialize()
    {
        _log.Information("Initializing campaign system");
        var settings = LoadGlobalSettings();

        if (settings.LastPlayedCampaignId.HasValue)
        {
            _log.Debug("Found last played campaign ID: {CampaignId}", settings.LastPlayedCampaignId.Value);
            var campaignPath = GetCampaignPath(settings.LastPlayedCampaignId.Value);
            if (File.Exists(campaignPath))
            {
                try
                {
                    Load(settings.LastPlayedCampaignId.Value);
                    return;
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Corrupt save file, backing up: {Path}", campaignPath);
                    BackupCorruptFile(campaignPath);
                }
            }
        }

        _log.Information("No valid campaign found, creating default");
        CreateNew("Default Campaign");
    }

    /// <summary>
    /// Saves the current campaign state to disk.
    /// </summary>
    public void Save()
    {
        if (CurrentCampaign == null)
        {
            _log.Warning("Save called with no campaign loaded");
            return;
        }

        _log.Debug("Saving campaign: {Name} ({Id})", CurrentCampaign.Name, CurrentCampaign.Id);

        // Gather current state
        var data = GatherState();

        // Serialize and write
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var path = GetCampaignPath(data.Id);
        File.WriteAllText(path, json);

        // Update global settings
        var settings = new GlobalSettings { LastPlayedCampaignId = data.Id };
        SaveGlobalSettings(settings);

        _log.Debug("Campaign saved to {Path}", path);
    }

    /// <summary>
    /// Loads a campaign by ID.
    /// </summary>
    public void Load(Guid campaignId)
    {
        _log.Information("Loading campaign: {CampaignId}", campaignId);
        var path = GetCampaignPath(campaignId);
        if (!File.Exists(path))
        {
            _log.Error("Campaign file not found: {Path}", path);
            throw new FileNotFoundException($"Campaign not found: {campaignId}");
        }

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<CampaignData>(json, JsonOptions)
            ?? throw new InvalidDataException("Failed to deserialize campaign");

        HydrateServices(data);
        CurrentCampaign = data;

        // Update global settings
        var settings = new GlobalSettings { LastPlayedCampaignId = campaignId };
        SaveGlobalSettings(settings);

        _log.Information("Loaded campaign: {Name} with {HistoryCount} history entries", data.Name, data.History.Count);
    }

    /// <summary>
    /// Creates a new campaign with the given name.
    /// </summary>
    public void CreateNew(string name)
    {
        _log.Information("Creating new campaign: {Name}", name);

        // Reset all services
        _session.Chaos = 5;
        _session.Engine = "Mythic 2e";
        _session.Theme = "Fantasy";
        _stateManager.Reset();
        _historyService.Clear();

        // Create new campaign data
        CurrentCampaign = new CampaignData
        {
            Name = name
        };

        // Save immediately
        Save();
        _log.Information("Created campaign: {Name} ({Id})", name, CurrentCampaign.Id);
    }

    /// <summary>
    /// Deletes a campaign by ID.
    /// </summary>
    public bool Delete(Guid campaignId)
    {
        var path = GetCampaignPath(campaignId);
        if (!File.Exists(path)) return false;

        File.Delete(path);

        // Clean up legacy journal file
        var legacyJournal = GetJournalPath(campaignId);
        if (File.Exists(legacyJournal))
            File.Delete(legacyJournal);

        // Clean up vault directory
        var vaultPath = GetVaultPath(campaignId);
        if (Directory.Exists(vaultPath))
            Directory.Delete(vaultPath, recursive: true);

        // If this was the current campaign, clear it
        if (CurrentCampaign?.Id == campaignId)
        {
            CurrentCampaign = null;
        }

        return true;
    }

    /// <summary>
    /// Lists all available campaigns.
    /// </summary>
    public IEnumerable<CampaignData> ListCampaigns()
    {
        if (!Directory.Exists(SavesDirectory))
            yield break;

        foreach (var file in Directory.GetFiles(SavesDirectory, "*.json"))
        {
            if (Path.GetFileName(file) == "settings.json")
                continue;

            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!Guid.TryParse(fileName, out var fileId))
            {
                _log.Warning("Skipping campaign file with non-guid name: {Path}", file);
                continue;
            }

            CampaignData? data = null;
            try
            {
                var json = File.ReadAllText(file);
                data = JsonSerializer.Deserialize<CampaignData>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Skipping corrupt campaign file: {Path}", file);
            }

            if (data == null)
                continue;

            if (data.Id != fileId)
            {
                // If the JSON payload has an ID mismatch vs the filename, prefer the filename.
                // Otherwise campaign switching will try to load a file that does not exist.
                _log.Warning("Campaign ID mismatch. Using filename ID. File={Path} JsonId={JsonId} FileId={FileId}", file, data.Id, fileId);
                data = data with { Id = fileId };
            }

            yield return data;
        }
    }

    /// <summary>
    /// Gets the file path for a campaign.
    /// </summary>
    public string GetCampaignPath(Guid id) => Path.Combine(SavesDirectory, $"{id}.json");

    /// <summary>
    /// Gets the file path for a campaign journal (legacy single-file format).
    /// </summary>
    public string GetJournalPath(Guid id) => Path.Combine(SavesDirectory, $"{id}.md");

    /// <summary>
    /// Gets the vault directory path for a campaign's notes.
    /// </summary>
    public string GetVaultPath(Guid id) => Path.Combine(SavesDirectory, id.ToString());

    private void HydrateServices(CampaignData data)
    {
        // Hydrate session
        _session.Chaos = data.Chaos;
        _session.Engine = data.Engine;
        _session.Theme = data.Theme;

        // Hydrate adventure state
        var state = new AdventureState
        {
            Characters = data.Characters,
            ActiveThreads = data.ActiveThreads,
            ClosedThreads = data.ClosedThreads
        };
        _stateManager.LoadState(state);

        // Hydrate history
        _historyService.LoadHistory(data.History);
    }

    private CampaignData GatherState()
    {
        if (CurrentCampaign == null)
            throw new InvalidOperationException("No campaign loaded");

        return CurrentCampaign with
        {
            LastPlayed = DateTime.Now,
            Chaos = _session.Chaos,
            Engine = _session.Engine,
            Theme = _session.Theme,
            Characters = [.. _stateManager.State.Characters],
            ActiveThreads = [.. _stateManager.State.ActiveThreads],
            ClosedThreads = [.. _stateManager.State.ClosedThreads],
            History = _historyService.GetAllEntries()
        };
    }

    private GlobalSettings LoadGlobalSettings()
    {
        if (!File.Exists(SettingsPath))
            return new GlobalSettings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<GlobalSettings>(json, JsonOptions)
                ?? new GlobalSettings();
        }
        catch
        {
            return new GlobalSettings();
        }
    }

    private void SaveGlobalSettings(GlobalSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static string FindOrCreateSavesDirectory()
    {
        // Try relative to executable first
        var baseDir = AppContext.BaseDirectory;
        var savesDir = Path.Combine(baseDir, "saves");

        // Walk up looking for existing saves directory or src folder
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "saves");
            if (Directory.Exists(candidate))
                return candidate;

            // If we find src/SoloForge.Console, create saves there
            var srcCandidate = Path.Combine(current.FullName, "src", "SoloForge.Console", "saves");
            if (Directory.Exists(Path.GetDirectoryName(srcCandidate)))
            {
                Directory.CreateDirectory(srcCandidate);
                return srcCandidate;
            }

            current = current.Parent;
        }

        // Default: create next to executable
        Directory.CreateDirectory(savesDir);
        return savesDir;
    }

    private static void BackupCorruptFile(string path)
    {
        if (!File.Exists(path)) return;

        var backupPath = path + ".bak";
        var counter = 1;
        while (File.Exists(backupPath))
        {
            backupPath = $"{path}.{counter}.bak";
            counter++;
        }
        File.Move(path, backupPath);
    }
}
