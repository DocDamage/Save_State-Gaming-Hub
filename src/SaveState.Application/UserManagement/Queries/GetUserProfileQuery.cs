using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.UserManagement.DTOs;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.UserManagement.Queries;

public record GetUserProfileQuery : IRequest<Result<UserProfileDto>>
{
    public required UserId UserId { get; init; }
}
