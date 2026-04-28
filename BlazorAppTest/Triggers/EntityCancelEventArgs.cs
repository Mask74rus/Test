namespace BlazorAppTest.Domain;

public class EntityCancelEventArgs<T>(T entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes)
    : EventArgs
{
    public T Entity { get; } = entity;

    public EntityStateChangeEnum State { get; } = state;

    public IReadOnlyList<PropertyChangeInfo> Changes { get; } = changes;

    public bool Cancel { get; set; }

    public string? ErrorMessage { get; set; }

    // Позволяет остановить выполнение других триггеров в иерархии
    public bool Handled { get; set; }
}
