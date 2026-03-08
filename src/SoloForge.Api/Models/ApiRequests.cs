namespace SoloForge.Api.Models;

public sealed record CreateCampaignRequest(string? Name);

public sealed record UpdateSessionRequest(int? Chaos, string? Engine, string? Theme);

public sealed record FateCheckRequest(string? Odds, string? Question, int? Chaos);

public sealed record SceneCheckRequest(string? Context = null, int? Chaos = null);

public sealed record DiceRollRequest(string? Expression);

public sealed record JournalUpdateRequest(string? Content);

public sealed record AddCharacterRequest(string? Name, string? Description);

public sealed record AddThreadRequest(string? Name, string? Description);

public sealed record MeaningRequest(string? Context);

public sealed record MeaningTableRequest(string? TableId, string? Context);

public sealed record MeaningFusionRequest(string? TableId1, string? TableId2, string? Context);

public sealed record GenerateQuickSetRequest(string? Id, string? Context);

// Notes vault requests
public sealed record NoteCreateRequest(string? Path, string? Content);

public sealed record NoteUpdateRequest(string? Path, string? Content);

public sealed record NoteMoveRequest(string? OldPath, string? NewPath);

public sealed record FolderCreateRequest(string? Path);

public sealed record SetSessionLogRequest(string? Path);
