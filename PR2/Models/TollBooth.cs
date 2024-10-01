namespace PR2.Models
{
    public class TollBooth
    {
        private SemaphoreSlim _semaphore;  // Управляет количеством шлагбаумов

        public TollBooth(int numberOfGates)
        {
            _semaphore = new SemaphoreSlim(numberOfGates, numberOfGates); // Количество шлагбаумов
        }

        public async Task ProcessCar(Car car, int serviceTime)
        {
            await _semaphore.WaitAsync(); // Ожидаем свободного шлагбаума

            try
            {
                // Моделируем обслуживание автомобиля на шлагбауме
                await Task.Delay(serviceTime);
            }
            finally
            {
                _semaphore.Release(); // Освобождаем шлагбаум после обслуживания
            }
        }

        // Доступ к количеству свободных шлагбаумов
        public int AvailableGates => _semaphore.CurrentCount;
    }
}
