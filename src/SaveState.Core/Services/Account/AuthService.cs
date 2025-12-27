using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Account
{
    public enum AuthProvider
    {
        Local,
        Google,
        Discord,
        Twitch,
        Steam
    }

    public class AuthToken
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public AuthProvider Provider { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }

    public class UserCredentials
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public AuthProvider Provider { get; set; } = AuthProvider.Local;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public AuthToken? Token { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthService
    {
        private static AuthService? _instance;
        private readonly ILogger _logger = Log.ForContext<AuthService>();
        private readonly HttpClient _httpClient;
        private readonly string _credentialsPath;
        private readonly Dictionary<string, UserCredentials> _users = new();
        private UserCredentials? _currentUser;
        private AuthToken? _currentToken;

        public static AuthService Instance => _instance ??= new AuthService();
        public bool IsLoggedIn => _currentUser != null;
        public UserCredentials? CurrentUser => _currentUser;

        public event EventHandler<UserCredentials?>? UserChanged;

        private AuthService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _credentialsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "auth");
            if (!Directory.Exists(_credentialsPath)) Directory.CreateDirectory(_credentialsPath);
            LoadUsers();
            TryAutoLogin();
        }

        // Local authentication
        public async Task<AuthResult> RegisterAsync(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return new AuthResult { Success = false, ErrorMessage = "Username must be at least 3 characters" };

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return new AuthResult { Success = false, ErrorMessage = "Invalid email address" };

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return new AuthResult { Success = false, ErrorMessage = "Password must be at least 6 characters" };

            if (_users.Values.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return new AuthResult { Success = false, ErrorMessage = "Username already taken" };

            if (_users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                return new AuthResult { Success = false, ErrorMessage = "Email already registered" };

            var user = new UserCredentials
            {
                UserId = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Provider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow
            };

            _users[user.UserId] = user;
            SaveUsers();

            // Auto-login after registration
            return await LoginAsync(username, password);
        }

        public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password)
        {
            await Task.Yield(); // Simulate async

            var user = _users.Values.FirstOrDefault(u =>
                u.Username.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return new AuthResult { Success = false, ErrorMessage = "User not found" };

            if (!VerifyPassword(password, user.PasswordHash))
                return new AuthResult { Success = false, ErrorMessage = "Invalid password" };

            user.LastLogin = DateTime.UtcNow;
            _currentUser = user;
            _currentToken = GenerateToken(user);
            
            SaveUsers();
            SaveSession();
            UserChanged?.Invoke(this, user);

            return new AuthResult
            {
                Success = true,
                UserId = user.UserId,
                Username = user.Username,
                Token = _currentToken
            };
        }

        public void Logout()
        {
            _currentUser = null;
            _currentToken = null;
            ClearSession();
            UserChanged?.Invoke(this, null);
        }

        // OAuth login stubs (would integrate with actual OAuth flows)
        public async Task<AuthResult> LoginWithGoogleAsync(string authCode)
        {
            // In production: Exchange auth code for tokens via Google OAuth
            await Task.Delay(100);
            return new AuthResult { Success = false, ErrorMessage = "Google OAuth not configured" };
        }

        public async Task<AuthResult> LoginWithDiscordAsync(string authCode)
        {
            await Task.Delay(100);
            return new AuthResult { Success = false, ErrorMessage = "Discord OAuth not configured" };
        }

        public async Task<AuthResult> LoginWithTwitchAsync(string authCode)
        {
            await Task.Delay(100);
            return new AuthResult { Success = false, ErrorMessage = "Twitch OAuth not configured" };
        }

        public async Task<AuthResult> LoginWithSteamAsync()
        {
            await Task.Delay(100);
            return new AuthResult { Success = false, ErrorMessage = "Steam OAuth not configured" };
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            if (_currentUser == null) return false;

            if (!VerifyPassword(currentPassword, _currentUser.PasswordHash))
                return false;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return false;

            _currentUser.PasswordHash = HashPassword(newPassword);
            SaveUsers();
            
            await Task.Yield();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            var user = _users.Values.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            
            if (user == null) return false;

            // In production: Send password reset email
            _logger.Debug("Password reset requested for: {Email}", email);
            await Task.Yield();
            return true;
        }

        public bool ValidateToken(string? token)
        {
            if (string.IsNullOrEmpty(token) || _currentToken == null)
                return false;

            return _currentToken.AccessToken == token && !_currentToken.IsExpired;
        }

        private AuthToken GenerateToken(UserCredentials user)
        {
            return new AuthToken
            {
                AccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                RefreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Provider = user.Provider
            };
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = "SaveState2024!"; // In production: Use per-user salt
            var bytes = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private void TryAutoLogin()
        {
            var sessionPath = Path.Combine(_credentialsPath, "session.json");
            if (!File.Exists(sessionPath)) return;

            try
            {
                var json = File.ReadAllText(sessionPath);
                var session = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                
                if (session != null && 
                    session.TryGetValue("userId", out var userId) &&
                    _users.TryGetValue(userId, out var user))
                {
                    _currentUser = user;
                    _currentToken = GenerateToken(user);
                    UserChanged?.Invoke(this, user);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Auto-login failed");
            }
        }

        private void SaveSession()
        {
            if (_currentUser == null) return;

            var sessionPath = Path.Combine(_credentialsPath, "session.json");
            var session = new Dictionary<string, string>
            {
                { "userId", _currentUser.UserId },
                { "savedAt", DateTime.UtcNow.ToString("O") }
            };
            File.WriteAllText(sessionPath, JsonSerializer.Serialize(session));
        }

        private void ClearSession()
        {
            var sessionPath = Path.Combine(_credentialsPath, "session.json");
            if (File.Exists(sessionPath)) File.Delete(sessionPath);
        }

        private void LoadUsers()
        {
            var usersPath = Path.Combine(_credentialsPath, "users.json");
            if (File.Exists(usersPath))
            {
                try
                {
                    var json = File.ReadAllText(usersPath);
                    var users = JsonSerializer.Deserialize<List<UserCredentials>>(json);
                    if (users != null)
                    {
                        foreach (var user in users)
                        {
                            _users[user.UserId] = user;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to load users");
                }
            }
        }

        private void SaveUsers()
        {
            var usersPath = Path.Combine(_credentialsPath, "users.json");
            var json = JsonSerializer.Serialize(_users.Values.ToList(), 
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(usersPath, json);
        }
    }
}
