using BlazorAppTest.Interfaces;

namespace BlazorAppTest.Domain;

public class UnitArchiveNotificationTrigger : IAfterSaveTrigger<ISoftDeletable>
{
    public async Task HandleAsync(EntityChangedEventArgs<ISoftDeletable> args)
    {
        if (args.State == EntityStateChangeEnum.SoftDeleted)
        {
            // Здесь будет: await _bus.Publish(new UnitArchivedEvent(args.Entity.Id));
            await Task.CompletedTask;
        }
    }
}