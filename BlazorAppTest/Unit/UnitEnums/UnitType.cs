namespace BlazorAppTest.Unit;

public enum UnitType
{
    // --- Структурные подразделения (Organizational) ---
    Workshop,       // Цех
    Section,        // Участок
    Line,           // Линия / Конвейер
    Workstation,    // Рабочее место / Пост

    // --- Складская логистика (Storage & Logistics) ---
    Warehouse,      // Здание склада
    Zone,           // Зона хранения (например, зона приемки или зона А)
    Rack,           // Стеллаж
    Shelf,          // Полка
    Cell,           // Ячейка (конечный адрес хранения)

    // --- Оборудование и техника (Equipment) ---
    Crane,          // Кран / Подъемное устройство
    MachineTool,    // Станок
    Table,          // Стол / Верстак
    Vehicle,        // Транспортное средство (погрузчик, тягач)
    Conveyor,       // Автономный транспортер

    // --- Прочее ---
    Other           // Прочее
}