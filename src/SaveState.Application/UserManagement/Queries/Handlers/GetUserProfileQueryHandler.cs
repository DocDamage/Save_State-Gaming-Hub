using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.UserManagement.DTOs;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.UserManagement.Queries.Handlers;

/// <summary>
/// Handler for retrieving user profile information.
/// Returns detailed user profile data including personal information and preferences.
/// </summary>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private readonly ITimeProvider _timeProvider;

    public GetUserProfileQueryHandler(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Handles the request to get a user's profile information.
    /// </summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the user profile or an error.</returns>
    public Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var now = _timeProvider.UtcNow;

        // Mock implementation - in a real system, this would query user data from database
        var mockUser = new UserProfileDto
        {
            Id = request.UserId,
            UserName = $"user_{request.UserId.Value}",
            Email = $"user_{request.UserId.Value}@example.com",
            DisplayName = $"User {request.UserId.Value}",
            AvatarUrl = $"/avatars/user_{request.UserId.Value}.png",
            CreatedAt = now.AddDays(-30),
            LastLoginAt = now.AddHours(-2),
            IsActive = true
        };

        return Task.FromResult(Result.Success<UserProfileDto>(mockUser));
    }
}

