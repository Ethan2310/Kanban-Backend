using Application.Common.Interfaces;

using Domain.Entities;
using Domain.Enumerations;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using ValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.Users;

public class AuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IApplicationDbContext context,
        IJwtTokenGenerator jwt,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _context = context;
        _jwt = jwt;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var emailTaken = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email, ct);

        if (emailTaken)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), "Email is already in use.")
            ]);

        int? createdById = null;

        if (request.AddedById.HasValue)
        {
            var addedByUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.AddedById.Value, ct);

            if (addedByUser is null || addedByUser.Role != Domain.Enumerations.UserRole.Admin)
                throw new ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(request.AddedById), "AddedById must reference an existing admin user.")
                ]);

            createdById = request.AddedById.Value;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsVerified = false,
            CreatedById = createdById
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        return new RegisterResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("credentials", "Invalid email or password.")
            ]);

        var token = _jwt.Generate(user.Id, user.Email, user.Role.ToString());

        return new AuthResponse(token, user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());
    }
}
