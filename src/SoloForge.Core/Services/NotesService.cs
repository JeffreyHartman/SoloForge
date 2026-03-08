using Serilog;
using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

/// <summary>
/// Manages a directory-based notes vault per campaign.
/// Each campaign gets a folder at saves/{campaignId}/ containing markdown files and subfolders.
/// </summary>
public sealed class NotesService
{
    private readonly Func<Guid, string> _vaultPathResolver;
    private readonly ILogger _log = AppLogger.ForContext<NotesService>();

    public NotesService(Func<Guid, string> vaultPathResolver)
    {
        _vaultPathResolver = vaultPathResolver ?? throw new ArgumentNullException(nameof(vaultPathResolver));
    }

    public string GetVaultPath(Guid campaignId) => _vaultPathResolver(campaignId);

    /// <summary>
    /// Returns the recursive tree of notes and folders in a campaign vault.
    /// </summary>
    public List<NoteNode> ListTree(Guid campaignId)
    {
        var vault = GetVaultPath(campaignId);
        if (!Directory.Exists(vault)) return [];
        return BuildTree(vault, vault);
    }

    /// <summary>
    /// Returns all note paths (relative, forward-slash) in the vault.
    /// </summary>
    public List<string> ListAllNotePaths(Guid campaignId)
    {
        var vault = GetVaultPath(campaignId);
        if (!Directory.Exists(vault)) return [];

        return Directory.GetFiles(vault, "*.md", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(vault, f).Replace('\\', '/'))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Reads a note's content. Returns null if the note doesn't exist.
    /// </summary>
    public string? ReadNote(Guid campaignId, string relativePath)
    {
        var fullPath = ResolveSafePath(campaignId, relativePath);
        if (!File.Exists(fullPath)) return null;

        try
        {
            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to read note: {Path}", fullPath);
            return null;
        }
    }

    /// <summary>
    /// Writes content to a note, creating parent directories as needed.
    /// </summary>
    public bool WriteNote(Guid campaignId, string relativePath, string content)
    {
        var fullPath = ResolveSafePath(campaignId, relativePath);

        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content ?? string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to write note: {Path}", fullPath);
            return false;
        }
    }

    /// <summary>
    /// Creates a new note. Returns false if it already exists.
    /// </summary>
    public bool CreateNote(Guid campaignId, string relativePath, string? content = null)
    {
        var fullPath = ResolveSafePath(campaignId, relativePath);
        if (File.Exists(fullPath)) return false;

        return WriteNote(campaignId, relativePath, content ?? string.Empty);
    }

    /// <summary>
    /// Deletes a note file.
    /// </summary>
    public bool DeleteNote(Guid campaignId, string relativePath)
    {
        var fullPath = ResolveSafePath(campaignId, relativePath);
        if (!File.Exists(fullPath)) return false;

        try
        {
            File.Delete(fullPath);
            CleanEmptyParents(fullPath, GetVaultPath(campaignId));
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to delete note: {Path}", fullPath);
            return false;
        }
    }

    /// <summary>
    /// Creates a folder in the vault.
    /// </summary>
    public bool CreateFolder(Guid campaignId, string relativePath)
    {
        var fullPath = ResolveSafePath(campaignId, relativePath);
        if (Directory.Exists(fullPath)) return false;

        try
        {
            Directory.CreateDirectory(fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to create folder: {Path}", fullPath);
            return false;
        }
    }

    /// <summary>
    /// Deletes a folder and all its contents.
    /// </summary>
    public bool DeleteFolder(Guid campaignId, string relativePath)
    {
        var fullPath = ResolveSafePath(campaignId, relativePath);
        if (!Directory.Exists(fullPath)) return false;

        try
        {
            Directory.Delete(fullPath, recursive: true);
            CleanEmptyParents(fullPath, GetVaultPath(campaignId));
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to delete folder: {Path}", fullPath);
            return false;
        }
    }

    /// <summary>
    /// Moves or renames a note or folder.
    /// </summary>
    public bool Move(Guid campaignId, string oldRelativePath, string newRelativePath)
    {
        var oldFull = ResolveSafePath(campaignId, oldRelativePath);
        var newFull = ResolveSafePath(campaignId, newRelativePath);

        if (oldFull == newFull) return false;

        try
        {
            var newDir = Path.GetDirectoryName(newFull);
            if (!string.IsNullOrEmpty(newDir))
                Directory.CreateDirectory(newDir);

            if (File.Exists(oldFull))
            {
                if (File.Exists(newFull)) return false;
                File.Move(oldFull, newFull);
            }
            else if (Directory.Exists(oldFull))
            {
                if (Directory.Exists(newFull)) return false;
                Directory.Move(oldFull, newFull);
            }
            else
            {
                return false;
            }

            CleanEmptyParents(oldFull, GetVaultPath(campaignId));
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to move {Old} -> {New}", oldFull, newFull);
            return false;
        }
    }

    /// <summary>
    /// Ensures the vault directory exists for a campaign.
    /// </summary>
    public void EnsureVault(Guid campaignId)
    {
        var vault = GetVaultPath(campaignId);
        Directory.CreateDirectory(vault);
    }

    /// <summary>
    /// Migrates a legacy single-file journal to the vault structure.
    /// Moves saves/{campaignId}.md -> saves/{campaignId}/Session Log.md
    /// </summary>
    public bool MigrateIfNeeded(Guid campaignId, string legacyJournalPath, string sessionLogPath)
    {
        var vault = GetVaultPath(campaignId);

        if (!File.Exists(legacyJournalPath))
            return false;

        // If vault already has files, don't migrate (vault takes precedence)
        if (Directory.Exists(vault) && Directory.GetFiles(vault, "*.md", SearchOption.AllDirectories).Length > 0)
        {
            _log.Debug("Vault already has notes, skipping migration for {CampaignId}", campaignId);
            return false;
        }

        try
        {
            Directory.CreateDirectory(vault);
            // Validate sessionLogPath stays within vault (path traversal protection)
            var targetPath = ResolveSafePath(campaignId, sessionLogPath);
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Move(legacyJournalPath, targetPath);
            _log.Information("Migrated journal {Legacy} -> {Target}", legacyJournalPath, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to migrate journal for campaign {CampaignId}", campaignId);
            return false;
        }
    }

    /// <summary>
    /// Deletes the entire vault directory for a campaign.
    /// </summary>
    public bool DeleteVault(Guid campaignId)
    {
        var vault = GetVaultPath(campaignId);
        if (!Directory.Exists(vault)) return false;

        try
        {
            Directory.Delete(vault, recursive: true);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to delete vault for campaign {CampaignId}", campaignId);
            return false;
        }
    }

    /// <summary>
    /// Validates and resolves a relative path to an absolute path within the vault.
    /// Throws if the path would escape the vault directory.
    /// </summary>
    private string ResolveSafePath(Guid campaignId, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Path cannot be empty", nameof(relativePath));

        // Reject obvious traversal attempts
        if (relativePath.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Path traversal not allowed");

        var vault = GetVaultPath(campaignId);
        var fullPath = Path.GetFullPath(Path.Combine(vault, relativePath));

        // Ensure resolved path is still within the vault
        var normalizedVault = Path.GetFullPath(vault);
        if (!fullPath.StartsWith(normalizedVault + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, normalizedVault, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal not allowed");
        }

        return fullPath;
    }

    private static List<NoteNode> BuildTree(string directory, string vaultRoot)
    {
        var nodes = new List<NoteNode>();

        // Folders first, sorted
        foreach (var subDir in Directory.GetDirectories(directory).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(subDir);
            var relativePath = Path.GetRelativePath(vaultRoot, subDir).Replace('\\', '/');
            nodes.Add(new NoteNode
            {
                Name = name,
                Path = relativePath,
                IsFolder = true,
                Children = BuildTree(subDir, vaultRoot)
            });
        }

        // Note files, sorted
        foreach (var file in Directory.GetFiles(directory, "*.md").OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var relativePath = Path.GetRelativePath(vaultRoot, file).Replace('\\', '/');
            nodes.Add(new NoteNode
            {
                Name = name,
                Path = relativePath,
                IsFolder = false
            });
        }

        return nodes;
    }

    /// <summary>
    /// Removes empty parent directories up to the vault root after a file/folder deletion.
    /// </summary>
    private static void CleanEmptyParents(string deletedPath, string vaultRoot)
    {
        var normalizedRoot = Path.GetFullPath(vaultRoot);
        var parent = Path.GetDirectoryName(deletedPath);

        while (!string.IsNullOrEmpty(parent) &&
               !string.Equals(Path.GetFullPath(parent), normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any())
            {
                Directory.Delete(parent);
                parent = Path.GetDirectoryName(parent);
            }
            else
            {
                break;
            }
        }
    }
}
