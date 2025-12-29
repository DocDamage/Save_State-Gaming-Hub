namespace SaveState.Application.GameLibrary.Validators;

using FluentValidation;
using SaveState.Application.GameLibrary.Commands;

public class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Game title is required")
            .Length(1, 200).WithMessage("Game title must be between 1 and 200 characters");

        RuleFor(x => x.CoverImagePath)
            .Must(path => path == null || Uri.IsWellFormedUriString(path, UriKind.Absolute) || path.StartsWith("/"))
            .WithMessage("Cover image path must be a valid URL or relative path");
    }
}
