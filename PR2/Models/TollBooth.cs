namespace PR2.Models
{
    public class TollBooth
    {
        private SemaphoreSlim _semaphore; 

        public TollBooth(int numberOfGates)
        {
            _semaphore = new SemaphoreSlim(numberOfGates, numberOfGates);
        }
        public async Task ProcessCar(Car car, int serviceTime)
        {
            await _semaphore.WaitAsync();

            try
            {
                await Task.Delay(serviceTime);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        public int AvailableGates => _semaphore.CurrentCount;
    }
}