using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;

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
    private readonly IValidator<GetUsersRequest> _getUsersValidator;
    private readonly IValidator<DeleteUserRequest> _deleteUserValidator;

    public AuthService(
        IApplicationDbContext context,
        IJwtTokenGenerator jwt,
        IValidator<DeleteUserRequest> deleteUserValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<GetUsersRequest> getUsersValidator)
    {
        _context = context;
        _jwt = jwt;
        _deleteUserValidator = deleteUserValidator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _getUsersValidator = getUsersValidator;
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
            throw new ConflictException("A user with this email address already exists.");

        int? createdById = null;

        if (request.AddedById.HasValue)
        {
            var addedByUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.AddedById.Value, ct);

            if (addedByUser is null || addedByUser.Role != Domain.Enumerations.UserRole.Admin)
                throw new UnauthorizedException("You do not have permission to register users.");

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
            throw new UnauthorizedException("Invalid email or password.");

        var token = _jwt.Generate(user.Id, user.Email, user.Role.ToString());

        return new AuthResponse(token, user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());
    }

    public async Task<DeleteUserResponse> DeleteUserAsync(int currentUserId, int userId, CancellationToken ct)
    {
        var validation = await _deleteUserValidator.ValidateAsync(new DeleteUserRequest(userId), ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (adminUser is null || adminUser.Role != UserRole.Admin)
            throw new UnauthorizedException("You do not have permission to delete users.");

        var userToDelete = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException(userId.ToString(), userId);
        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync(ct);

        return new DeleteUserResponse(true);
    }

    public async Task<GetUsersResponse> GetUsersAsync(GetUsersRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _getUsersValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct);

        if (currentUser is null || currentUser.Role != UserRole.Admin)
            throw new UnauthorizedException("You do not have permission to view users.");

        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.FirstName))
            query = query.Where(u => u.FirstName.Contains(request.FirstName));

        if (!string.IsNullOrEmpty(request.LastName))
            query = query.Where(u => u.LastName.Contains(request.LastName));

        if (!string.IsNullOrEmpty(request.Email))
            query = query.Where(u => u.Email.Contains(request.Email));

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role.ToString(),
                u.IsVerified))
            .ToListAsync(ct);

        return new GetUsersResponse(
            users,
            new PaginationMetadata(request.PageNumber, request.PageSize, totalCount));
    }

}
