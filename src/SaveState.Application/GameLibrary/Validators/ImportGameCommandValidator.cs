using FluentValidation;
using SaveState.Application.GameLibrary.Commands;

namespace SaveState.Application.GameLibrary.Validators;

public class ImportGameCommandValidator : AbstractValidator<ImportGameCommand>
{
    public ImportGameCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Game title is required")
            .Length(1, 200).WithMessage("Game title must be 1-200 characters");

        RuleFor(x => x.PlatformName)
            .NotEmpty().WithMessage("Platform name is required")
            .Length(1, 50).WithMessage("Platform name must be 1-50 characters");

        RuleFor(x => x.InstallPath)
            .Must(BeValidPath).When(x => !string.IsNullOrEmpty(x.InstallPath))
            .WithMessage("Install path must be a valid absolute path");

        RuleFor(x => x.Source)
            .Length(0, 50).WithMessage("Source must be 50 characters or less");

        RuleFor(x => x.SourceId)
            .Length(0, 100).WithMessage("Source ID must be 100 characters or less");

        RuleForEach(x => x.Tags)
            .Length(1, 30).WithMessage("Each tag must be 1-30 characters");
    }

    private bool BeValidPath(string? path)
    {
        return !string.IsNullOrEmpty(path) && Path.IsPathRooted(path);
    }
}
