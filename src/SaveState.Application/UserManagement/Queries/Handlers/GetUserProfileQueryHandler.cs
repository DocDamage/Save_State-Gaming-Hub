using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.UserManagement.DTOs;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.UserManagement.Queries.Handlers;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    public Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        // Mock implementation - in a real system, this would query user data from database
        var mockUser = new UserProfileDto
        {
            Id = request.UserId,
            UserName = $"user_{request.UserId.Value}",
            Email = $"user_{request.UserId.Value}@example.com",
            DisplayName = $"User {request.UserId.Value}",
            AvatarUrl = $"/avatars/user_{request.UserId.Value}.png",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastLoginAt = DateTime.UtcNow.AddHours(-2),
            IsActive = true
        };

        return Task.FromResult(Result<UserProfileDto>.Success(mockUser));
    }
}
