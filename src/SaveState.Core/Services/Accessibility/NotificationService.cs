using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Serilog;

namespace SaveState.Core.Services.Accessibility
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Achievement,
        Challenge,
        Friend,
        System
    }

    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Icon { get; set; } = "ℹ️";
        public DateTime CreatedAt { get; set; }
        public int DurationMs { get; set; } = 5000;
        public bool IsRead { get; set; }
        public bool IsDismissed { get; set; }
        public string? ActionLabel { get; set; }
        public Action? Action { get; set; }
    }

    public class NotificationService
    {
        private static NotificationService? _instance;
        private readonly ILogger _logger = Log.ForContext<NotificationService>();
        private readonly ConcurrentQueue<Notification> _queue = new();
        private readonly List<Notification> _history = new();
        private readonly AccessibilityService _accessibilityService;
        private const int MaxHistory = 100;

        public event EventHandler<Notification>? NotificationReceived;
        public event EventHandler<Notification>? NotificationDismissed;

        public static NotificationService Instance => _instance ??= new NotificationService();

        public IReadOnlyList<Notification> History => _history;
        public int UnreadCount => _history.FindAll(n => !n.IsRead).Count;

        private NotificationService()
        {
            _accessibilityService = AccessibilityService.Instance;
        }

        public void Show(string title, string message, NotificationType type = NotificationType.Info)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Icon = GetIconForType(type),
                CreatedAt = DateTime.Now,
                DurationMs = _accessibilityService.Settings.ToastDuration
            };

            _queue.Enqueue(notification);
            AddToHistory(notification);
            
            NotificationReceived?.Invoke(this, notification);
            _accessibilityService.AnnounceNotification(title, message);
            
            _logger.Information("[{Type}] {Title}: {Message}", type, title, message);
        }

        public void ShowInfo(string title, string message) => Show(title, message, NotificationType.Info);
        public void ShowSuccess(string title, string message) => Show(title, message, NotificationType.Success);
        public void ShowWarning(string title, string message) => Show(title, message, NotificationType.Warning);
        public void ShowError(string title, string message) => Show(title, message, NotificationType.Error);

        public void ShowAchievement(string achievementName, int xpReward = 0)
        {
            var message = xpReward > 0 ? $"+{xpReward} XP" : "Achievement unlocked!";
            var notification = new Notification
            {
                Title = $"🏆 {achievementName}",
                Message = message,
                Type = NotificationType.Achievement,
                Icon = "🏆",
                CreatedAt = DateTime.Now,
                DurationMs = 7000 // Show achievements longer
            };

            _queue.Enqueue(notification);
            AddToHistory(notification);
            NotificationReceived?.Invoke(this, notification);
        }

        public void ShowChallengeComplete(string challengeName, int xpReward)
        {
            Show($"🎯 Challenge Complete!", $"{challengeName} - +{xpReward} XP", NotificationType.Challenge);
        }

        public void ShowFriendActivity(string friendName, string activity)
        {
            Show($"👤 {friendName}", activity, NotificationType.Friend);
        }

        public void ShowWithAction(string title, string message, string actionLabel, Action action)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Info,
                Icon = "ℹ️",
                CreatedAt = DateTime.Now,
                ActionLabel = actionLabel,
                Action = action
            };

            _queue.Enqueue(notification);
            AddToHistory(notification);
            NotificationReceived?.Invoke(this, notification);
        }

        public void Dismiss(string notificationId)
        {
            var notification = _history.Find(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsDismissed = true;
                NotificationDismissed?.Invoke(this, notification);
            }
        }

        public void DismissAll()
        {
            foreach (var notification in _history)
            {
                notification.IsDismissed = true;
            }
        }

        public void MarkAsRead(string notificationId)
        {
            var notification = _history.Find(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
        }

        public void MarkAllAsRead()
        {
            foreach (var notification in _history)
            {
                notification.IsRead = true;
            }
        }

        public List<Notification> GetUnread()
        {
            return _history.FindAll(n => !n.IsRead);
        }

        public Notification? GetNext()
        {
            return _queue.TryDequeue(out var notification) ? notification : null;
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        private void AddToHistory(Notification notification)
        {
            _history.Insert(0, notification);
            while (_history.Count > MaxHistory)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        private string GetIconForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => "ℹ️",
                NotificationType.Success => "✅",
                NotificationType.Warning => "⚠️",
                NotificationType.Error => "❌",
                NotificationType.Achievement => "🏆",
                NotificationType.Challenge => "🎯",
                NotificationType.Friend => "👤",
                NotificationType.System => "🔔",
                _ => "ℹ️"
            };
        }
    }
}
