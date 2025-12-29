using FluentValidation;
using SaveState.Application.Common.Options;

namespace SaveState.Application.Common.Validation;

public class LaunchOptionsValidator : AbstractValidator<LaunchOptions>
{
    public LaunchOptionsValidator()
    {
        RuleFor(x => x.Arguments)
            .Length(0, 1000).WithMessage("Launch arguments must be 1000 characters or less");

        RuleFor(x => x.WorkingDirectory)
            .Must(BeValidPath).When(x => !string.IsNullOrEmpty(x.WorkingDirectory))
            .WithMessage("Working directory must be a valid absolute path");
    }

    private bool BeValidPath(string? path)
    {
        return !string.IsNullOrEmpty(path) && Path.IsPathRooted(path);
    }
}
