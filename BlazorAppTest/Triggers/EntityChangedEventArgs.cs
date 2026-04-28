namespace BlazorAppTest.Domain;

public class EntityChangedEventArgs<T>(T entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes)
    : EventArgs
{
    public T Entity { get; } = entity;

    public EntityStateChangeEnum State { get; } = state;

    public IReadOnlyList<PropertyChangeInfo> Changes { get; } = changes;

    public bool Handled { get; set; } 
}