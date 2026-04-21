using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using ValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.Projects;

public class ProjectService
{
    private readonly IApplicationDbContext _context;
    private readonly IAdminAuthorizationService _adminAuthorizationService;

    private readonly IValidator<CreateProjectRequest> _createProjectValidator;
    private readonly IValidator<UpdateProjectRequest> _updateProjectValidator;
    private readonly IValidator<DeleteProjectRequest> _deleteProjectValidator;
    private readonly IValidator<GetProjectsRequest> _getProjectsValidator;
    private readonly IValidator<AddUserToProjectRequest> _addUserToProjectValidator;
    private readonly IValidator<GetUsersInProjectRequest> _getUsersInProjectValidator;
    private readonly IValidator<RemoveUserFromProjectRequest> _removeUserFromProjectValidator;

    public ProjectService(
        IApplicationDbContext context,
        IAdminAuthorizationService adminAuthorizationService,
        IValidator<CreateProjectRequest> createProjectValidator,
        IValidator<UpdateProjectRequest> updateProjectValidator,
        IValidator<DeleteProjectRequest> deleteProjectValidator,
        IValidator<GetProjectsRequest> getProjectsValidator,
        IValidator<AddUserToProjectRequest> addUserToProjectValidator,
        IValidator<GetUsersInProjectRequest> getUsersInProjectValidator,
        IValidator<RemoveUserFromProjectRequest> removeUserFromProjectValidator)
    {
        _context = context;
        _adminAuthorizationService = adminAuthorizationService;
        _createProjectValidator = createProjectValidator;
        _updateProjectValidator = updateProjectValidator;
        _deleteProjectValidator = deleteProjectValidator;
        _getProjectsValidator = getProjectsValidator;
        _addUserToProjectValidator = addUserToProjectValidator;
        _getUsersInProjectValidator = getUsersInProjectValidator;
        _removeUserFromProjectValidator = removeUserFromProjectValidator;
    }

    public async Task<CreateProjectResponse> CreateProjectAsync(CreateProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _createProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "create", "projects", ct);

        var project = new Domain.Entities.Project
        {
            Name = request.Name,
            Description = request.Description,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(ct);

        return new CreateProjectResponse(project.Id, project.Name, project.Description);
    }

    public async Task<UpdateProjectResponse> UpdateProjectAsync(int projectId, UpdateProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "update", "projects", ct);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);

        if (request.Name != null)
            project.Name = request.Name;
        if (request.Description != null)
            project.Description = request.Description;
        project.UpdatedById = currentUserId;
        project.UpdatedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return new UpdateProjectResponse(project.Id, project.Name, project.Description);
    }

    public async Task DeleteProjectAsync(DeleteProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _deleteProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "delete", "projects", ct);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, ct)
            ?? throw new NotFoundException("Project", request.ProjectId);

        project.IsActive = false;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<GetProjectsResponse> GetProjectsAsync(GetProjectsRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _getProjectsValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "view", "projects", ct);

        var projectsQuery = _context.Projects
            .AsNoTracking();

        if (request.BoardId.HasValue)
        {
            // Return projects linked to the provided board via ProjectBoards.
            projectsQuery = projectsQuery.Where(p =>
                p.ProjectBoards.Any(pb => pb.IsActive && pb.BoardId == request.BoardId.Value));
        }

        if (request.UserId.HasValue)
        {
            // Return projects this user has access to via UserProjectAccess.
            projectsQuery = projectsQuery.Where(p =>
                p.UserProjectAccesses.Any(upa => upa.IsActive && upa.UserId == request.UserId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var searchTerm = request.Name.Trim();
            projectsQuery = projectsQuery.Where(p => p.Name.Contains(searchTerm));
        }

        var totalCount = await projectsQuery.CountAsync(ct);

        var projects = await projectsQuery
            .OrderByDescending(p => p.CreatedOn)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProjectSummaryResponse(p.Id, p.Name, p.Description))
            .ToListAsync(ct);

        return new GetProjectsResponse(projects, new PaginationMetadata(request.PageNumber, request.PageSize, totalCount));
    }

    public async Task<AddUserToProjectResponse> AddUserToProjectAsync(AddUserToProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _addUserToProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "create", "user-project-access", ct);

        var project = await _context.Projects
            .Include(p => p.UserProjectAccesses)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive, ct)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId && u.IsActive, ct);
        if (!userExists)
            throw new NotFoundException("User", request.UserId);

        if (project.UserProjectAccesses.Any(upa => upa.IsActive && upa.UserId == request.UserId))
            throw new BadRequestException($"User {request.UserId} already has access to project {request.ProjectId}.");

        var userProjectAccess = new Domain.Entities.UserProjectAccess
        {
            ProjectId = request.ProjectId,
            UserId = request.UserId,
            CreatedById = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        _context.UserProjectAccesses.Add(userProjectAccess);
        await _context.SaveChangesAsync(ct);

        return new AddUserToProjectResponse(request.ProjectId, request.UserId);
    }

    public async Task<GetUsersInProjectResponse> GetUsersInProjectAsync(GetUsersInProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _getUsersInProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "view", "user-project-access", ct);

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId && p.IsActive, ct);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var userAccessQuery = _context.UserProjectAccesses
            .AsNoTracking()
            .Where(upa => upa.IsActive && upa.ProjectId == request.ProjectId && upa.User.IsActive);

        var totalCount = await userAccessQuery.CountAsync(ct);

        var users = await userAccessQuery
            .OrderBy(upa => upa.User.FirstName)
            .ThenBy(upa => upa.User.LastName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(upa => new UserSummaryResponse(upa.User.Id, upa.User.FirstName, upa.User.LastName, upa.User.Email))
            .ToListAsync(ct);

        return new GetUsersInProjectResponse(users, new PaginationMetadata(request.PageNumber, request.PageSize, totalCount));
    }

    public async Task<RemoveUserFromProjectResponse> RemoveUserFromProjectAsync(RemoveUserFromProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _removeUserFromProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "delete", "user-project-access", ct);

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId && p.IsActive, ct);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId && u.IsActive, ct);
        if (!userExists)
            throw new NotFoundException("User", request.UserId);

        var userProjectAccess = await _context.UserProjectAccesses
            .FirstOrDefaultAsync(upa => upa.IsActive && upa.ProjectId == request.ProjectId && upa.UserId == request.UserId, ct)
            ?? throw new NotFoundException("UserProjectAccess", new { request.ProjectId, request.UserId });

        userProjectAccess.IsActive = false;
        await _context.SaveChangesAsync(ct);

        return new RemoveUserFromProjectResponse(true);
    }

}
