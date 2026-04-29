namespace BlazorAppTest.Audit;

// Вспомогательный класс для десериализации 
public class AuditPropertyEntry
{
    public string PropertyName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}