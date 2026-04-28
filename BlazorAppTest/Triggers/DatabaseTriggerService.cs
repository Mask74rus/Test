
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace BlazorAppTest.Domain;

public class DatabaseTriggerService(IServiceScopeFactory scopeFactory)
{
    // Словари для хранения подписчиков
    private readonly Dictionary<Type, List<Func<EntityCancelEventArgs<object>, Task>>> _beforeSubscribers = new();
    private readonly Dictionary<Type, List<Func<EntityChangedEventArgs<object>, Task>>> _afterSubscribers = new();

    // Кэш иерархии типов для быстрого поиска триггеров
    private readonly ConcurrentDictionary<Type, List<Type>> _hierarchyCache = new();

    public IServiceScopeFactory ScopeFactory => scopeFactory;

    /// <summary>
    /// Регистрация триггера, выполняемого ДО сохранения (валидация).
    /// </summary>
    public void BeforeSave<T>(Func<EntityCancelEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_beforeSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityCancelEventArgs<T>(
                (T)args.Entity,
                args.State,
                args.Changes,
                args.Context);

            await action(genericArgs);

            args.Cancel = genericArgs.Cancel;
            args.ErrorMessage = genericArgs.ErrorMessage;
            args.Handled = genericArgs.Handled;
        });
    }

    /// <summary>
    /// Регистрация триггера, выполняемого ПОСЛЕ успешного сохранения (уведомления).
    /// </summary>
    public void AfterSave<T>(Func<EntityChangedEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_afterSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityChangedEventArgs<T>(
                (T)args.Entity,
                args.State,
                args.Changes);

            await action(genericArgs);
            args.Handled = genericArgs.Handled;
        });
    }

    /// <summary>
    /// Вызов валидаторов (используется в Interceptor.SavingChangesAsync)
    /// </summary>
    public async Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context)
    {
        IEnumerable<Type> typesToCheck = GetTypesHierarchy(entity.GetType());
        var args = new EntityCancelEventArgs<object>(entity, state, changes, context);

        foreach (Type type in typesToCheck)
        {
            if (_beforeSubscribers.TryGetValue(type, out var actions))
            {
                foreach (var action in actions)
                {
                    await action(args);
                    if (args.Cancel)
                        throw new OperationCanceledException(args.ErrorMessage ?? "Действие заблокировано триггером.");

                    if (args.Handled) return;
                }
            }
        }
    }

    /// <summary>
    /// Вызов уведомлений (используется в Interceptor.SavedChangesAsync)
    /// </summary>
    public async Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes)
    {
        IEnumerable<Type> typesToNotify = GetTypesHierarchy(entity.GetType());
        var args = new EntityChangedEventArgs<object>(entity, state, changes);

        foreach (Type type in typesToNotify)
        {
            if (_afterSubscribers.TryGetValue(type, out var actions))
            {
                foreach (var action in actions)
                {
                    await action(args);
                    if (args.Handled) return;
                }
            }
        }
    }

    private void AddSubscriber<TAction>(Dictionary<Type, List<TAction>> dict, Type type, TAction action)
    {
        if (!dict.TryGetValue(type, out var list))
        {
            list = new List<TAction>();
            dict[type] = list;
        }
        list.Add(action);
    }

    private IEnumerable<Type> GetTypesHierarchy(Type type)
    {
        return _hierarchyCache.GetOrAdd(type, t =>
        {
            var baseTypes = new List<Type>();
            Type? current = t;
            while (current != null && current != typeof(object))
            {
                baseTypes.Add(current);
                current = current.BaseType;
            }
            return baseTypes.Concat(t.GetInterfaces()).Distinct().ToList();
        });
    }
}
/*
//Реализация
triggerService.Subscribe<ProjectStatus>(async args => {
       await _emailService.SendAsync("Admin", $"Status {args.Entity.Name} changed");
   });
*/