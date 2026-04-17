using Domain.Enumerations;

namespace Application.Users;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    int? AddedById);

public record RegisterResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);

public record LoginRequest(
    string Email,
    string Password);

public record AuthResponse(
    string Token,
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);

public record DeleteUserRequest(int AdminID, int UserID);

public record DeleteUserResponse(bool Success);
