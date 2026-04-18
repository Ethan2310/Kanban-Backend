using Application.Common.Models;

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

public record DeleteUserRequest(int UserID);

public record DeleteUserResponse(bool Success);

public record GetUsersRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    int PageNumber = PaginationRequestDefaults.PageNumber,
    int PageSize = PaginationRequestDefaults.PageSize);

public record UserSummaryResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsVerified);

public record GetUsersResponse(
    IReadOnlyList<UserSummaryResponse> Users,
    PaginationMetadata Pagination);
