
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace BlazorAppTest.Domain;

public class DatabaseTriggerService(IServiceScopeFactory scopeFactory)
{
    private readonly Dictionary<Type, List<Func<EntityCancelEventArgs<object>, Task>>> _beforeSubscribers = new();
    private readonly Dictionary<Type, List<Func<EntityChangedEventArgs<object>, Task>>> _afterSubscribers = new();
    private readonly ConcurrentDictionary<Type, List<Type>> _hierarchyCache = new();

    public IServiceScopeFactory ScopeFactory => scopeFactory;

    public void BeforeSave<T>(Func<EntityCancelEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_beforeSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityCancelEventArgs<T>((T)args.Entity, args.State, args.Changes, args.Context);
            await action(genericArgs);
            args.Cancel = genericArgs.Cancel;
            args.ErrorMessage = genericArgs.ErrorMessage;
            args.Handled = genericArgs.Handled;
        });
    }

    public void AfterSave<T>(Func<EntityChangedEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_afterSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityChangedEventArgs<T>((T)args.Entity, args.State, args.Changes, args.ChangedBy, args.ChangedAt);
            await action(genericArgs);
            args.Handled = genericArgs.Handled;
        });
    }

    public async Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context)
    {
        foreach (var type in GetTypesHierarchy(entity.GetType()))
        {
            if (_beforeSubscribers.TryGetValue(type, out var actions))
            {
                var args = new EntityCancelEventArgs<object>(entity, state, changes, context);
                foreach (var action in actions)
                {
                    await action(args);
                    if (args.Cancel) throw new OperationCanceledException(args.ErrorMessage ?? "Action blocked.");
                    if (args.Handled) return;
                }
            }
        }
    }

    public async Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? user, DateTime at)
    {
        foreach (var type in GetTypesHierarchy(entity.GetType()))
        {
            if (_afterSubscribers.TryGetValue(type, out var actions))
            {
                var args = new EntityChangedEventArgs<object>(entity, state, changes, user, at);
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
        if (!dict.TryGetValue(type, out var list)) { list = new(); dict[type] = list; }
        list.Add(action);
    }

    private IEnumerable<Type> GetTypesHierarchy(Type type) =>
        _hierarchyCache.GetOrAdd(type, t => {
            var types = new List<Type>();
            for (var c = t; c != null && c != typeof(object); c = c.BaseType) types.Add(c);
            return types.Concat(t.GetInterfaces()).Distinct().ToList();
        });
}
/*
//Реализация
triggerService.Subscribe<ProjectStatus>(async args => {
       await _emailService.SendAsync("Admin", $"Status {args.Entity.Name} changed");
   });
*/