namespace BlazorAppTest.DomainObject.Interface;

public interface IDomainObjectHasKey<TKey> : IDomainObject
{
    TKey Id { get; set; }
}