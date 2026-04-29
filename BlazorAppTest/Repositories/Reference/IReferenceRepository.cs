using BlazorAppTest.Domain;

namespace BlazorAppTest.Repositories;

public interface IReferenceRepository<T> : IBaseRepository<T, Guid> where T : ReferenceBase
{
    Task<T?> GetByCodeAsync(string code);

    Task<List<T>> SearchByNameAsync(string namePart);
}