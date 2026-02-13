using SaveState.Core.Blockchain.Models;
using SaveState.Core.Common;

namespace SaveState.Core.Blockchain.Services;

/// <summary>
/// Service for managing decentralized save state storage on blockchain and IPFS.
/// </summary>
public interface IDecentralizedSaveStateNetwork
{
    /// <summary>
    /// Initializes the decentralized save state network.
    /// </summary>
    /// <param name="configuration">Blockchain configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(BlockchainConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Uploads a save state to decentralized storage.
    /// </summary>
    /// <param name="request">Upload request.</param>
    /// <param name="progressCallback">Optional progress callback.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the uploaded save state details.</returns>
    Task<Result<DecentralizedSaveState>> UploadSaveStateAsync(UploadSaveStateRequest request, IProgress<double>? progressCallback = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads a save state from decentralized storage.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="userId">User identifier requesting download.</param>
    /// <param name="progressCallback">Optional progress callback.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the save state data.</returns>
    Task<Result<byte[]>> DownloadSaveStateAsync(string saveStateId, string userId, IProgress<double>? progressCallback = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a decentralized save state metadata.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing save state metadata.</returns>
    Task<Result<DecentralizedSaveState>> GetSaveStateAsync(string saveStateId, CancellationToken ct = default);

    /// <summary>
    /// Lists all decentralized save states for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing save states.</returns>
    Task<Result<IReadOnlyList<DecentralizedSaveState>>> GetUserSaveStatesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Lists all decentralized save states for a game.
    /// </summary>
    /// <param name="gameId">Game identifier.</param>
    /// <param name="visibility">Filter by visibility.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing save states.</returns>
    Task<Result<IReadOnlyList<DecentralizedSaveState>>> GetGameSaveStatesAsync(string gameId, SaveStateVisibility? visibility = null, CancellationToken ct = default);

    /// <summary>
    /// Updates save state visibility.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="newVisibility">New visibility level.</param>
    /// <param name="userId">User making the change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing updated save state.</returns>
    Task<Result<DecentralizedSaveState>> UpdateVisibilityAsync(string saveStateId, SaveStateVisibility newVisibility, string userId, CancellationToken ct = default);

    /// <summary>
    /// Grants access to a save state to another user.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="ownerUserId">Owner user identifier.</param>
    /// <param name="grantToUserId">User to grant access to.</param>
    /// <param name="grantToWalletAddress">Wallet address to grant access to.</param>
    /// <param name="permission">Access permission level.</param>
    /// <param name="expiresAt">Optional expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the access grant.</returns>
    Task<Result<SaveStateAccessGrant>> GrantAccessAsync(string saveStateId, string ownerUserId, string grantToUserId, string grantToWalletAddress, AccessPermission permission, DateTime? expiresAt = null, CancellationToken ct = default);

    /// <summary>
    /// Revokes access to a save state.
    /// </summary>
    /// <param name="grantId">Access grant identifier.</param>
    /// <param name="ownerUserId">Owner user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RevokeAccessAsync(string grantId, string ownerUserId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has access to a save state.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing access permission if granted.</returns>
    Task<Result<AccessPermission?>> CheckAccessAsync(string saveStateId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a decentralized save state.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="userId">User requesting deletion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteSaveStateAsync(string saveStateId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Verifies that a save state exists on the blockchain.
    /// </summary>
    /// <param name="saveStateId">Save state identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating verification status.</returns>
    Task<Result<bool>> VerifySaveStateAsync(string saveStateId, CancellationToken ct = default);

    /// <summary>
    /// Estimates storage costs for uploading a save state.
    /// </summary>
    /// <param name="fileSize">File size in bytes.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing estimated cost in native currency.</returns>
    Task<Result<decimal>> EstimateStorageCostAsync(long fileSize, BlockchainNetwork network, CancellationToken ct = default);

    /// <summary>
    /// Gets storage usage statistics for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing storage statistics.</returns>
    Task<Result<UserStorageStats>> GetUserStorageStatsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the status of an upload or download transaction.
    /// </summary>
    /// <param name="transactionHash">Transaction hash.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing transaction status.</returns>
    Task<Result<BlockchainTransaction>> GetTransactionStatusAsync(string transactionHash, BlockchainNetwork network, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the decentralized save state network.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}

/// <summary>
/// User storage statistics.
/// </summary>
public record UserStorageStats
{
    public string UserId { get; init; } = string.Empty;
    public int TotalSaveStates { get; init; }
    public long TotalStorageUsed { get; init; }
    public long StorageLimit { get; init; }
    public IReadOnlyDictionary<BlockchainNetwork, long> StorageByNetwork { get; init; } = new Dictionary<BlockchainNetwork, long>();
    public IReadOnlyDictionary<string, int> SaveStatesByGame { get; init; } = new Dictionary<string, int>();
}
