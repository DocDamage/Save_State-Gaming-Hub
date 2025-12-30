using FluentValidation;
using SaveState.Application.UserManagement.Commands;

namespace SaveState.Application.UserManagement.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("Username or email is required")
            .Length(3, 254).WithMessage("Username or email must be between 3 and 254 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
