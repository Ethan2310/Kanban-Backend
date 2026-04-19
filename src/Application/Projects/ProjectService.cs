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

    public ProjectService(
        IApplicationDbContext context,
        IAdminAuthorizationService adminAuthorizationService,
        IValidator<CreateProjectRequest> createProjectValidator,
        IValidator<UpdateProjectRequest> updateProjectValidator,
        IValidator<DeleteProjectRequest> deleteProjectValidator,
        IValidator<GetProjectsRequest> getProjectsValidator)
    {
        _context = context;
        _adminAuthorizationService = adminAuthorizationService;
        _createProjectValidator = createProjectValidator;
        _updateProjectValidator = updateProjectValidator;
        _deleteProjectValidator = deleteProjectValidator;
        _getProjectsValidator = getProjectsValidator;
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

    public async Task<UpdateProjectResponse> UpdateProjectAsync(UpdateProjectRequest request, int currentUserId, CancellationToken ct)
    {
        var validation = await _updateProjectValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _adminAuthorizationService.EnsureAdminUserAsync(currentUserId, "update", "projects", ct);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, ct)
            ?? throw new NotFoundException("Project", request.ProjectId);

        project.Name = request.Name;
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

}
