using BookManager.Domain.Entities;

namespace BookManager.Application.Abstractions.Security
{
    public interface IJwtTokenGenerator
    {
        string Generate(User user);
    }
}
