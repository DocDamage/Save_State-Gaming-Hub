using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Blockchain.Models;

/// <summary>
/// Represents a blockchain network type.
/// </summary>
public enum BlockchainNetwork
{
    Ethereum,
    Polygon,
    Solana,
    Avalanche,
    BinanceSmartChain,
    Arbitrum,
    Optimism,
    Custom
}

/// <summary>
/// Represents a blockchain-based achievement token.
/// </summary>
public record BlockchainAchievement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string AchievementId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? TokenUri { get; init; }
    public BlockchainNetwork Network { get; init; }
    public string? ContractAddress { get; init; }
    public int? TokenId { get; init; }
    public int TotalMinted { get; init; }
    public int MaxSupply { get; init; }
    public bool IsTransferable { get; init; } = false;
    public DateTime CreatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public AchievementRarity Rarity { get; init; } = AchievementRarity.Common;
}

/// <summary>
/// Achievement rarity levels.
/// </summary>
public enum AchievementRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

/// <summary>
/// Represents a minted achievement NFT.
/// </summary>
public record MintedAchievement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string AchievementId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string WalletAddress { get; init; } = string.Empty;
    public string TransactionHash { get; init; } = string.Empty;
    public BlockchainNetwork Network { get; init; }
    public string ContractAddress { get; init; } = string.Empty;
    public int TokenId { get; init; }
    public DateTime MintedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public string? BlockNumber { get; init; }
    public bool Verified { get; init; } = false;
}

/// <summary>
/// Represents a decentralized save state on blockchain.
/// </summary>
public record DecentralizedSaveState
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string SaveStateId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string WalletAddress { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IpfsHash { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? EncryptionKeyHash { get; init; }
    public string TransactionHash { get; init; } = string.Empty;
    public BlockchainNetwork Network { get; init; }
    public string ContractAddress { get; init; } = string.Empty;
    public int TokenId { get; init; }
    public SaveStateVisibility Visibility { get; init; } = SaveStateVisibility.Private;
    public DateTime UploadedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime? LastAccessedAt { get; init; }
    public int AccessCount { get; init; } = 0;
}

/// <summary>
/// Save state visibility levels.
/// </summary>
public enum SaveStateVisibility
{
    Private,
    Unlisted,
    Public,
    Shared
}

/// <summary>
/// Represents a wallet connection.
/// </summary>
public record WalletConnection
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public string WalletAddress { get; init; } = string.Empty;
    public WalletType WalletType { get; init; }
    public BlockchainNetwork PreferredNetwork { get; init; }
    public bool IsConnected { get; init; }
    public DateTime ConnectedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime? LastUsedAt { get; init; }
}

/// <summary>
/// Wallet types.
/// </summary>
public enum WalletType
{
    MetaMask,
    WalletConnect,
    CoinbaseWallet,
    Phantom,
    Ledger,
    Trezor,
    TrustWallet,
    Rainbow,
    Other
}

/// <summary>
/// Represents a blockchain transaction.
/// </summary>
public record BlockchainTransaction
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string TransactionHash { get; init; } = string.Empty;
    public BlockchainNetwork Network { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string? Data { get; init; }
    public TransactionStatus Status { get; init; } = TransactionStatus.Pending;
    public int? Confirmations { get; init; }
    public string? BlockNumber { get; init; }
    public decimal? GasPrice { get; init; }
    public decimal? GasUsed { get; init; }
    public DateTime CreatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime? ConfirmedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Transaction status.
/// </summary>
public enum TransactionStatus
{
    Pending,
    Confirmed,
    Failed,
    Dropped,
    Replaced
}

/// <summary>
/// Configuration for blockchain features.
/// </summary>
public record BlockchainConfiguration
{
    public bool AchievementsEnabled { get; init; } = true;
    public bool SaveStateNetworkEnabled { get; init; } = false;
    public BlockchainNetwork DefaultNetwork { get; init; } = BlockchainNetwork.Polygon;
    public IReadOnlyDictionary<BlockchainNetwork, NetworkConfiguration> Networks { get; init; } = new Dictionary<BlockchainNetwork, NetworkConfiguration>();
    public string? IpfsGatewayUrl { get; init; }
    public string? PinataApiKey { get; init; }
    public string? PinataSecretKey { get; init; }
}

/// <summary>
/// Network-specific configuration.
/// </summary>
public record NetworkConfiguration
{
    public string RpcUrl { get; init; } = string.Empty;
    public int ChainId { get; init; }
    public string? ContractAddress { get; init; }
    public string CurrencySymbol { get; init; } = "ETH";
    public int BlockConfirmationThreshold { get; init; } = 12;
    public decimal? MaxGasPrice { get; init; }
}

/// <summary>
/// Request to mint an achievement.
/// </summary>
public record MintAchievementRequest
{
    public string AchievementId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string WalletAddress { get; init; } = string.Empty;
    public BlockchainNetwork Network { get; init; }
}

/// <summary>
/// Request to upload a save state.
/// </summary>
public record UploadSaveStateRequest
{
    public string SaveStateId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string WalletAddress { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public SaveStateVisibility Visibility { get; init; } = SaveStateVisibility.Private;
    public BlockchainNetwork Network { get; init; }
}

/// <summary>
/// Represents a shared save state access grant.
/// </summary>
public record SaveStateAccessGrant
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string SaveStateId { get; init; } = string.Empty;
    public string OwnerUserId { get; init; } = string.Empty;
    public string GrantedToUserId { get; init; } = string.Empty;
    public string GrantedToWalletAddress { get; init; } = string.Empty;
    public AccessPermission Permission { get; init; } = AccessPermission.Read;
    public DateTime GrantedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Access permission levels.
/// </summary>
public enum AccessPermission
{
    Read,
    Copy,
    Modify
}

/// <summary>
/// Statistics for blockchain features.
/// </summary>
public record BlockchainStats
{
    public int TotalMintedAchievements { get; init; }
    public int TotalDecentralizedSaveStates { get; init; }
    public long TotalStorageUsed { get; init; }
    public IReadOnlyDictionary<BlockchainNetwork, int> AchievementsByNetwork { get; init; } = new Dictionary<BlockchainNetwork, int>();
    public IReadOnlyDictionary<string, int> TopGames { get; init; } = new Dictionary<string, int>();
}
