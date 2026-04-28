using BlazorAppTest.DomainObject.Interface;
using BlazorAppTest.Interfaces;
using System.Reflection;

namespace BlazorAppTest.Domain;

public static class DbTriggersConfiguration
{
    public static void RegisterDomainTriggers(this IServiceProvider services)
    {
        var triggerService = services.GetRequiredService<DatabaseTriggerService>();

        // Используем сборку, где лежат интерфейсы триггеров (ваш основной проект)
        Assembly assembly = typeof(IBeforeSaveTrigger<>).Assembly;

        RegisterTriggers(assembly, triggerService, "BeforeSave", typeof(IBeforeSaveTrigger<>));
        RegisterTriggers(assembly, triggerService, "AfterSave", typeof(IAfterSaveTrigger<>));
    }

    private static void RegisterTriggers(Assembly assembly, DatabaseTriggerService service, string methodName, Type interfaceType)
    {
        List<Type> types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface &&
                        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType))
            .ToList();

        foreach (Type type in types)
        {
            IEnumerable<Type> interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);

            foreach (Type @interface in interfaces)
            {
                Type entityType = @interface.GetGenericArguments()[0];
                MethodInfo targetMethod = typeof(DatabaseTriggerService).GetMethod(methodName)!.MakeGenericMethod(entityType);

                // Создаем обертку. Важно: args приходит как object (из сервиса)
                var action = new Func<object, Task>(async (args) =>
                {
                    using IServiceScope scope = service.ScopeFactory.CreateScope();
                    object handler = ActivatorUtilities.CreateInstance(scope.ServiceProvider, type);

                    // Используем dynamic для вызова HandleAsync. 
                    // Это автоматически найдет нужный метод HandleAsync(EntityCancelEventArgs<T>)
                    // несмотря на то, что args передан как object.
                    dynamic dynamicHandler = handler;
                    dynamic dynamicArgs = args;

                    await dynamicHandler.HandleAsync(dynamicArgs);
                });

                targetMethod.Invoke(service, [action]);
            }
        }
    }
}