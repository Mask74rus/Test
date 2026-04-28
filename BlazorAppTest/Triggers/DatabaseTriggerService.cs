
using System.Collections.Concurrent;

namespace BlazorAppTest.Domain;

public class DatabaseTriggerService(IServiceScopeFactory scopeFactory)
{
    // Словари для разделения логики
    private readonly Dictionary<Type, List<Func<EntityCancelEventArgs<object>, Task>>> _beforeSubscribers = new();

    // Теперь здесь тоже EntityChangedEventArgs для object
    private readonly Dictionary<Type, List<Func<EntityChangedEventArgs<object>, Task>>> _afterSubscribers = new();

    // Добавьте это свойство, чтобы оно было доступно в методах расширения
    public IServiceScopeFactory ScopeFactory => scopeFactory;

    // Кэш типов
    private readonly ConcurrentDictionary<Type, List<Type>> _hierarchyCache = new();
    
    // Подписка на проверку (До сохранения)
    public void BeforeSave<T>(Func<EntityCancelEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_beforeSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityCancelEventArgs<T>((T)args.Entity, args.State, (List<PropertyChangeInfo>)args.Changes);
            await action(genericArgs); 
            args.Cancel = genericArgs.Cancel;
            args.ErrorMessage = genericArgs.ErrorMessage;
            args.Handled = genericArgs.Handled;
        });
    }

    // Проверка перед сохранением
    public async Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes)
    {
        IEnumerable<Type> typesToCheck = GetTypesHierarchy(entity.GetType());
        var args = new EntityCancelEventArgs<object>(entity, state, changes);

        foreach (Type type in typesToCheck)
        {
            if (_beforeSubscribers.TryGetValue(type, out var actions))
            {
                foreach (var action in actions)
                {
                    await action(args); // Ждем завершения валидации
                    if (args.Cancel) throw new OperationCanceledException(args.ErrorMessage ?? "Действие отменено триггером.");
                    if (args.Handled) return;
                }
            }
        }
    }

    // Метод Notify с поддержкой Handled
    public async Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes)
    {
        Type entityType = entity.GetType();
        IEnumerable<Type> typesToNotify = GetTypesHierarchy(entityType);

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

    // Вспомогательный метод (универсальный для обоих типов словарей)
    private void AddSubscriber<TAction>(Dictionary<Type, List<TAction>> dict, Type type, TAction action)
    {
        if (!dict.TryGetValue(type, out var list))
        {
            list = [];
            dict[type] = list;
        }
        list.Add(action);
    }

    // Подписка на уведомление (После сохранения) 
    public void Subscribe<T>(Func<EntityChangedEventArgs<T>, Task> action) where T : class
    {
        AddSubscriber(_afterSubscribers, typeof(T), async args =>
        {
            var genericArgs = new EntityChangedEventArgs<T>((T)args.Entity, args.State, (List<PropertyChangeInfo>)args.Changes);
            await action(genericArgs);
            args.Handled = genericArgs.Handled;
        });
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