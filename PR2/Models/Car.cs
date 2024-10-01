namespace PR2.Models
{
    public class Car
    {
        public int Id { get; set; } // Уникальный идентификатор автомобиля
        public string? LicensePlate { get; set; } // Номерной знак (можно использовать для визуализации)
        public double? PreviousLeftPosition { get; set; } = null; // Хранит предыдущую позицию машины
    }
}
