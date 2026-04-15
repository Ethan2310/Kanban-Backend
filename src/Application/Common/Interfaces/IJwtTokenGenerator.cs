namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(int userId, string email, string role);
}
