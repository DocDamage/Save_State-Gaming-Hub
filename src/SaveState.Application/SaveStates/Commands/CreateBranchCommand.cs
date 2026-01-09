using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;

namespace SaveState.Application.SaveStates.Commands;

public sealed record CreateBranchCommand(
    Guid RootStateId,
    string BranchName,
    string Description,
    BranchType Type = BranchType.StoryBranch) : IRequest<Result<Guid>>;
