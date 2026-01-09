using MediatR;
using SaveState.Core.Common;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;

namespace SaveState.Application.SaveStates.Commands.Handlers;

public sealed class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    private readonly ISaveStateRepository _repository;

    public CreateBranchCommandHandler(ISaveStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var branch = SaveStateBranch.Create(
            request.RootStateId,
            request.BranchName,
            request.Type,
            request.Description);

        await _repository.AddBranchAsync(branch, ct);

        return Result.Success(branch.Id);
    }
}

