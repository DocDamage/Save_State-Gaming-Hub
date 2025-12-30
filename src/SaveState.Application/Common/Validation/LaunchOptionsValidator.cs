using FluentValidation;
using SaveState.Application.Common.Options;
using SaveState.Core.Common.Validation;

namespace SaveState.Application.Common.Validation;

public class LaunchOptionsValidator : AbstractValidator<LaunchOptions>
{
    public LaunchOptionsValidator()
    {
        RuleFor(x => x.Arguments)
            .Length(0, 1000).WithMessage("Launch arguments must be 1000 characters or less")
            .Must(InputSanitizer.IsSafeCommandLine).When(x => !string.IsNullOrEmpty(x.Arguments))
            .WithMessage("Launch arguments contain potentially dangerous characters");

        RuleFor(x => x.WorkingDirectory)
            .Must(BeValidPath).When(x => !string.IsNullOrEmpty(x.WorkingDirectory))
            .WithMessage("Working directory must be a valid absolute path");
    }

    private bool BeValidPath(string? path)
    {
        return !string.IsNullOrEmpty(path) && Path.IsPathRooted(path) && InputSanitizer.IsSafePath(path);
    }
}
