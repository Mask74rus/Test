using BlazorAppTest.Domain;

namespace BlazorAppTest.Interfaces;

// Для правил "ДО" сохранения (валидация)
public interface IBeforeSaveTrigger<T> where T : class
{
    Task HandleAsync(EntityCancelEventArgs<T> args);
}