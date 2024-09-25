namespace PR2.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public int CurrentPosition { get; set; } = 0;
    }
}