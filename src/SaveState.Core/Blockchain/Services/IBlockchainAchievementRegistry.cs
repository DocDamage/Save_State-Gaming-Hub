using SaveState.Core.Blockchain.Models;
using SaveState.Core.Common;

namespace SaveState.Core.Blockchain.Services;

/// <summary>
/// Service for registering and managing gaming achievements as NFTs on blockchain.
/// </summary>
public interface IBlockchainAchievementRegistry
{
    /// <summary>
    /// Initializes the blockchain achievement registry.
    /// </summary>
    /// <param name="configuration">Blockchain configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeAsync(BlockchainConfiguration configuration, CancellationToken ct = default);

    /// <summary>
    /// Connects a wallet for blockchain operations.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="walletType">Type of wallet to connect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing wallet connection details.</returns>
    Task<Result<WalletConnection>> ConnectWalletAsync(string userId, WalletType walletType, CancellationToken ct = default);

    /// <summary>
    /// Disconnects a wallet.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DisconnectWalletAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the connected wallet for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing wallet connection.</returns>
    Task<Result<WalletConnection>> GetConnectedWalletAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Registers a new achievement type on the blockchain.
    /// </summary>
    /// <param name="achievement">Achievement details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the registered achievement.</returns>
    Task<Result<BlockchainAchievement>> RegisterAchievementAsync(BlockchainAchievement achievement, CancellationToken ct = default);

    /// <summary>
    /// Gets an achievement by ID.
    /// </summary>
    /// <param name="achievementId">Achievement identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the achievement.</returns>
    Task<Result<BlockchainAchievement>> GetAchievementAsync(string achievementId, CancellationToken ct = default);

    /// <summary>
    /// Lists all achievements for a game.
    /// </summary>
    /// <param name="gameId">Game identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing achievements.</returns>
    Task<Result<IReadOnlyList<BlockchainAchievement>>> GetGameAchievementsAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Mints an achievement NFT for a user.
    /// </summary>
    /// <param name="request">Mint request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the minted achievement details.</returns>
    Task<Result<MintedAchievement>> MintAchievementAsync(MintAchievementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets all minted achievements for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing minted achievements.</returns>
    Task<Result<IReadOnlyList<MintedAchievement>>> GetUserAchievementsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Verifies that a minted achievement exists on the blockchain.
    /// </summary>
    /// <param name="mintedAchievementId">Minted achievement identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating verification status.</returns>
    Task<Result<bool>> VerifyAchievementAsync(string mintedAchievementId, CancellationToken ct = default);

    /// <summary>
    /// Gets achievement statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing blockchain statistics.</returns>
    Task<Result<BlockchainStats>> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the status of a transaction.
    /// </summary>
    /// <param name="transactionHash">Transaction hash.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing transaction details.</returns>
    Task<Result<BlockchainTransaction>> GetTransactionStatusAsync(string transactionHash, BlockchainNetwork network, CancellationToken ct = default);

    /// <summary>
    /// Estimates gas fees for minting an achievement.
    /// </summary>
    /// <param name="achievementId">Achievement identifier.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing estimated gas fees in native currency.</returns>
    Task<Result<decimal>> EstimateMintingFeeAsync(string achievementId, BlockchainNetwork network, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the blockchain registry.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
