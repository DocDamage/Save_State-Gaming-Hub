using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Account
{
    public enum FriendStatus
    {
        None,
        Pending,        // Request sent
        Incoming,       // Request received
        Accepted,
        Blocked
    }

    public class FriendRelation
    {
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;
        public FriendStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public string? Nickname { get; set; }
    }

    public class FriendInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarPath { get; set; }
        public int Level { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? CurrentGame { get; set; }
        public FriendStatus Status { get; set; }
    }

    public class FriendsService
    {
        private static FriendsService? _instance;
        private readonly string _dataPath;
        private readonly AuthService _authService;
        private readonly ProfileService _profileService;
        private readonly Dictionary<string, List<FriendRelation>> _relations = new();

        public event EventHandler<FriendRelation>? FriendRequestReceived;
        public event EventHandler<FriendRelation>? FriendAccepted;

        public static FriendsService Instance => _instance ??= new FriendsService();

        private FriendsService()
        {
            _authService = AuthService.Instance;
            _profileService = ProfileService.Instance;
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "friends");
            if (!Directory.Exists(_dataPath)) Directory.CreateDirectory(_dataPath);
            LoadRelations();
        }

        private string? CurrentUserId => _authService.CurrentUser?.UserId;

        public async Task<bool> SendFriendRequestAsync(string targetUserId)
        {
            if (CurrentUserId == null || CurrentUserId == targetUserId) return false;

            var existing = GetRelation(CurrentUserId, targetUserId);
            if (existing != null) return false; // Already have a relation

            // Check target user exists
            var targetProfile = _profileService.GetProfile(targetUserId);
            if (targetProfile == null) return false;

            // Create outgoing request
            var relation = new FriendRelation
            {
                UserId = CurrentUserId,
                FriendId = targetUserId,
                Status = FriendStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            AddRelation(relation);

            // Create incoming request for target
            var incoming = new FriendRelation
            {
                UserId = targetUserId,
                FriendId = CurrentUserId,
                Status = FriendStatus.Incoming,
                CreatedAt = DateTime.UtcNow
            };
            AddRelation(incoming);

            SaveRelations();
            FriendRequestReceived?.Invoke(this, incoming);

            await Task.Yield();
            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(string fromUserId)
        {
            if (CurrentUserId == null) return false;

            var myRelation = GetRelation(CurrentUserId, fromUserId);
            var theirRelation = GetRelation(fromUserId, CurrentUserId);

            if (myRelation?.Status != FriendStatus.Incoming) return false;

            myRelation.Status = FriendStatus.Accepted;
            myRelation.AcceptedAt = DateTime.UtcNow;

            if (theirRelation != null)
            {
                theirRelation.Status = FriendStatus.Accepted;
                theirRelation.AcceptedAt = DateTime.UtcNow;
            }

            SaveRelations();
            FriendAccepted?.Invoke(this, myRelation);

            await Task.Yield();
            return true;
        }

        public async Task<bool> DeclineFriendRequestAsync(string fromUserId)
        {
            if (CurrentUserId == null) return false;

            RemoveRelation(CurrentUserId, fromUserId);
            RemoveRelation(fromUserId, CurrentUserId);
            SaveRelations();

            await Task.Yield();
            return true;
        }

        public async Task<bool> RemoveFriendAsync(string friendId)
        {
            if (CurrentUserId == null) return false;

            RemoveRelation(CurrentUserId, friendId);
            RemoveRelation(friendId, CurrentUserId);
            SaveRelations();

            await Task.Yield();
            return true;
        }

        public async Task<bool> BlockUserAsync(string userId)
        {
            if (CurrentUserId == null || CurrentUserId == userId) return false;

            // Remove existing relation if any
            RemoveRelation(CurrentUserId, userId);

            // Create block relation
            var relation = new FriendRelation
            {
                UserId = CurrentUserId,
                FriendId = userId,
                Status = FriendStatus.Blocked,
                CreatedAt = DateTime.UtcNow
            };
            AddRelation(relation);
            SaveRelations();

            await Task.Yield();
            return true;
        }

        public async Task<bool> UnblockUserAsync(string userId)
        {
            if (CurrentUserId == null) return false;

            RemoveRelation(CurrentUserId, userId);
            SaveRelations();

            await Task.Yield();
            return true;
        }

        public async Task<bool> SetNicknameAsync(string friendId, string nickname)
        {
            if (CurrentUserId == null) return false;

            var relation = GetRelation(CurrentUserId, friendId);
            if (relation == null || relation.Status != FriendStatus.Accepted) return false;

            relation.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname;
            SaveRelations();

            await Task.Yield();
            return true;
        }

        public List<FriendInfo> GetFriends()
        {
            if (CurrentUserId == null) return new();

            return GetUserRelations(CurrentUserId)
                .Where(r => r.Status == FriendStatus.Accepted)
                .Select(r => CreateFriendInfo(r))
                .Where(f => f != null)
                .Cast<FriendInfo>()
                .ToList();
        }

        public List<FriendInfo> GetPendingRequests()
        {
            if (CurrentUserId == null) return new();

            return GetUserRelations(CurrentUserId)
                .Where(r => r.Status == FriendStatus.Incoming)
                .Select(r => CreateFriendInfo(r))
                .Where(f => f != null)
                .Cast<FriendInfo>()
                .ToList();
        }

        public List<FriendInfo> GetSentRequests()
        {
            if (CurrentUserId == null) return new();

            return GetUserRelations(CurrentUserId)
                .Where(r => r.Status == FriendStatus.Pending)
                .Select(r => CreateFriendInfo(r))
                .Where(f => f != null)
                .Cast<FriendInfo>()
                .ToList();
        }

        public List<FriendInfo> GetBlockedUsers()
        {
            if (CurrentUserId == null) return new();

            return GetUserRelations(CurrentUserId)
                .Where(r => r.Status == FriendStatus.Blocked)
                .Select(r => CreateFriendInfo(r))
                .Where(f => f != null)
                .Cast<FriendInfo>()
                .ToList();
        }

        public FriendStatus GetFriendStatus(string userId)
        {
            if (CurrentUserId == null) return FriendStatus.None;
            return GetRelation(CurrentUserId, userId)?.Status ?? FriendStatus.None;
        }

        public int GetFriendCount()
        {
            if (CurrentUserId == null) return 0;
            return GetUserRelations(CurrentUserId).Count(r => r.Status == FriendStatus.Accepted);
        }

        private FriendInfo? CreateFriendInfo(FriendRelation relation)
        {
            var profile = _profileService.GetProfile(relation.FriendId);
            if (profile == null) return null;

            return new FriendInfo
            {
                UserId = profile.UserId,
                Username = profile.Username,
                DisplayName = relation.Nickname ?? profile.DisplayName,
                AvatarPath = profile.AvatarPath,
                Level = profile.Level,
                LastSeen = profile.LastActive,
                Status = relation.Status
            };
        }

        private FriendRelation? GetRelation(string userId, string friendId)
        {
            if (!_relations.TryGetValue(userId, out var list)) return null;
            return list.FirstOrDefault(r => r.FriendId == friendId);
        }

        private List<FriendRelation> GetUserRelations(string userId)
        {
            return _relations.GetValueOrDefault(userId) ?? new();
        }

        private void AddRelation(FriendRelation relation)
        {
            if (!_relations.ContainsKey(relation.UserId))
                _relations[relation.UserId] = new();
            
            _relations[relation.UserId].RemoveAll(r => r.FriendId == relation.FriendId);
            _relations[relation.UserId].Add(relation);
        }

        private void RemoveRelation(string userId, string friendId)
        {
            if (_relations.TryGetValue(userId, out var list))
            {
                list.RemoveAll(r => r.FriendId == friendId);
            }
        }

        private void LoadRelations()
        {
            var path = Path.Combine(_dataPath, "relations.json");
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var allRelations = JsonSerializer.Deserialize<List<FriendRelation>>(json);
                    if (allRelations != null)
                    {
                        foreach (var relation in allRelations)
                        {
                            AddRelation(relation);
                        }
                    }
                }
                catch { }
            }
        }

        private void SaveRelations()
        {
            var path = Path.Combine(_dataPath, "relations.json");
            var allRelations = _relations.Values.SelectMany(r => r).ToList();
            var json = JsonSerializer.Serialize(allRelations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
