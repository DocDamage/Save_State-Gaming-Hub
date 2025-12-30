using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.UserManagement.Commands;
using SaveState.Core.Common;
using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.UserManagement.Commands.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        try
        {
            // Validate passwords match
            if (request.Password != request.ConfirmPassword)
            {
                return Result<RegisterUserResponse>.Failure("Passwords do not match");
            }

            // Validate password strength
            var passwordValidation = _passwordHasher.ValidatePasswordStrength(request.Password);
            if (!passwordValidation.IsValid)
            {
                return Result<RegisterUserResponse>.Failure(
                    $"Password does not meet requirements: {string.Join(", ", passwordValidation.Errors)}");
            }

            // Check if username exists
            if (await _userRepository.UsernameExistsAsync(request.Username, ct))
            {
                return Result<RegisterUserResponse>.Failure("Username is already taken");
            }

            // Check if email exists
            if (await _userRepository.EmailExistsAsync(request.Email, ct))
            {
                return Result<RegisterUserResponse>.Failure("Email is already registered");
            }

            // Hash password
            var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.Password);

            // Create user
            var user = User.Create(request.Username, request.Email, passwordHash, passwordSalt);

            // Assign default role (User role)
            var userRole = await _roleRepository.GetByNameAsync("User", ct);
            if (userRole != null)
            {
                user.AddRole(userRole);
            }

            // Save user
            await _userRepository.AddAsync(user, ct);

            _logger.LogInformation("User registered successfully: {Username} ({UserId})",
                user.Username, user.Id);

            var response = new RegisterUserResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                RequiresEmailVerification = true // Could be configurable
            };

            return Result<RegisterUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for: {Username}", request.Username);
            return Result<RegisterUserResponse>.Failure("An error occurred during registration");
        }
    }
}
