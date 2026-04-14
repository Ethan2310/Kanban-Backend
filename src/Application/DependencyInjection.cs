using System.Reflection;

using Application.Boards;
using Application.Lists;
using Application.Projects;
using Application.Statuses;
using Application.Tasks;
using Application.Users;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<TaskService>();
        services.AddScoped<BoardService>();
        services.AddScoped<ListService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<StatusService>();
        services.AddScoped<UserService>();
        services.AddScoped<AuthService>();
        return services;
    }
}
