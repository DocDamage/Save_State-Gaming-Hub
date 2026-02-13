namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Network packet for transport.
/// </summary>
public class NetworkPacket
{
    public string PacketId { get; set; } = default!;
    public PacketType Type { get; set; }
    public string SourceAccountId { get; set; } = default!;
    public PlatformType SourcePlatform { get; set; }
    public byte[] Payload { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public int SequenceNumber { get; set; }
    public bool RequiresAck { get; set; }
}

/// <summary>
/// Types of network packets.
/// </summary>
public enum PacketType
{
    SyncRequest,
    SyncResponse,
    StateUpdate,
    ConflictNotification,
    Acknowledgment,
    Heartbeat,
    Disconnect
}

/// <summary>
/// Transport configuration.
/// </summary>
public class TransportConfig
{
    public string Endpoint { get; set; } = default!;
    public int Port { get; set; }
    public bool UseEncryption { get; set; } = true;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxRetries { get; set; } = 3;
    public int MaxConcurrentConnections { get; set; } = 10;
}

/// <summary>
/// Connection state information.
/// </summary>
public class ConnectionState
{
    public string ConnectionId { get; set; } = default!;
    public PlatformType Platform { get; set; }
    public bool IsConnected { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public long BytesTransferred { get; set; }
    public int PacketsSent { get; set; }
    public int PacketsReceived { get; set; }
}

/// <summary>
/// Network transport result.
/// </summary>
public class TransportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int BytesTransferred { get; set; }
    public TimeSpan Latency { get; set; }
}

/// <summary>
/// Peer information for direct connections.
/// </summary>
public class PeerInfo
{
    public string PeerId { get; set; } = default!;
    public string Endpoint { get; set; } = default!;
    public PlatformType Platform { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public bool IsReachable { get; set; }
}
