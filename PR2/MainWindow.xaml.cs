using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PR2.Models;

namespace PR2
{
    public partial class MainWindow : Window
    {
        private Random _random = new Random();
        private List<FrameworkElement> _cars = new List<FrameworkElement>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartSimulation(object sender, RoutedEventArgs e)
        {
            int dT1 = int.Parse(txtDT1.Text);
            int gates1 = int.Parse(txtGates1.Text);
            int dT2 = int.Parse(txtDT2.Text);
            int exitProbability = int.Parse(txtExitProbability.Text);
            int dT3 = int.Parse(txtDT3.Text);
            int gates2 = int.Parse(txtGates2.Text);
            int dT4 = int.Parse(txtDT4.Text);
            Application.Current.Dispatcher.Invoke(() => CreateEntryBarriers(gates1));
            Application.Current.Dispatcher.Invoke(() => CreateExitBarriers(gates2));

            TollBooth tollBooth1 = new TollBooth(gates1);
            TollBooth tollBooth2 = new TollBooth(gates2);

            await Task.Run(() => StartSimulation(gates1, gates2, tollBooth1, tollBooth2, dT1, dT2, dT3, exitProbability, dT4));
        }

        private async Task StartSimulation(int gates1, int gates2, TollBooth tollBooth1, TollBooth tollBooth2, int dT1, int dT2, int dT3, int exitProbability, int dT4)
        {
            int carId = 1;

            while (true)
            {
                await Task.Delay(dT1);

                Task.Run(async () =>
                {
                    var car = new Car { Id = carId++ };
                    Application.Current.Dispatcher.Invoke(() => AddCarToCanvas(car, gates1));
                    Application.Current.Dispatcher.Invoke(() => MoveCarRight(car, 175));
                    await tollBooth1.ProcessCar(car, dT2);
                    await Task.Delay(dT3);
                    Application.Current.Dispatcher.Invoke(() => ReassignCarToExitLane(car, gates2));

                    await Application.Current.Dispatcher.Invoke(() => MoveCarBetweenTollBooths(car, 185, 450, dT3));
                    if (_random.Next(100) < exitProbability)
                    {
                        Application.Current.Dispatcher.Invoke(() => MoveCarTop(car));
                        return;
                    }
                    await tollBooth2.ProcessCar(car, dT2);
                    await Application.Current.Dispatcher.Invoke(() => MoveCarRight(car, 650));
                    Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
                });
            }
        }

        private async Task ProcessCarAsync(Car car, TollBooth tollBooth1, TollBooth tollBooth2, int dT2, int dT3, int exitProbability, int dT4)
        {
            Application.Current.Dispatcher.Invoke(() => SetBarrierState(entryBarrier, true)); // Зеленый сигнал
            await tollBooth1.ProcessCar(car, dT2);
            Application.Current.Dispatcher.Invoke(() => SetBarrierState(entryBarrier, false)); // Красный сигнал
            Application.Current.Dispatcher.Invoke(() => MoveCarOnCanvas(car, 200));
            await Task.Delay(dT3);
            Application.Current.Dispatcher.Invoke(() => MoveCarOnCanvas(car, 400));
            if (_random.Next(100) < exitProbability)
            {
                Application.Current.Dispatcher.Invoke(() => MoveCarTop(car));
                return;
            }
            Application.Current.Dispatcher.Invoke(() => SetBarrierState(exitBarrier, true)); 
            await tollBooth2.ProcessCar(car, dT2);
            Application.Current.Dispatcher.Invoke(() => SetBarrierState(exitBarrier, false));
            Application.Current.Dispatcher.Invoke(() => MoveCarOnCanvas(car, 600));

            Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
        }

        private void AddCarToCanvas(Car car, int barrier)
        {
            Rectangle carRectangle = new Rectangle
            {
                Width = 30, 
                Height = 15,
                Fill = GetRandomColor() 
            };
            TextBlock carIdText = new TextBlock
            {
                Text = car.Id.ToString(),
                Foreground = Brushes.White,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid carGrid = new Grid
            {
                Width = 30,
                Height = 15
            };

            carGrid.Children.Add(carRectangle);
            carGrid.Children.Add(carIdText);
            double laneHeight = 50;
            double topPosition = 60 + _random.Next(barrier) * laneHeight;

            Canvas.SetLeft(carGrid, 10);
            Canvas.SetTop(carGrid, topPosition);

            carGrid.Tag = car;

            _cars.Add(carGrid);
            canvas.Children.Add(carGrid);
        }

        private void CreateEntryBarriers(int gates1)
        {
            foreach (var element in canvas.Children.OfType<Rectangle>().Where(r => r.Name?.StartsWith("entryBarrier") == true).ToList())
            {
                canvas.Children.Remove(element);
            }

            for (int i = 0; i < gates1; i++)
            {
                Rectangle entryBarrier = new Rectangle
                {
                    Width = 20,
                    Height = 35,
                    Fill = Brushes.Red,
                    Name = $"entryBarrier_{i}"
                };

                double laneHeight = 50; 
                double topPosition = 50 + i * laneHeight; 

                Canvas.SetLeft(entryBarrier, 180);
                Canvas.SetTop(entryBarrier, topPosition); 

                canvas.Children.Add(entryBarrier);
            }
        }

        private async Task GenerateCarsFromOtherHighway(TollBooth tollBooth2, int dT2, int dT4, int gates2)
        {
            int carId = 1000;
            Random random = new Random();

            while (true)
            {
                await Task.Delay(random.Next(dT4 / 2, dT4 * 2));
                var car = new Car { Id = carId++ };
                Application.Current.Dispatcher.Invoke(() => AddCarToCanvas(car, gates2));
                await tollBooth2.ProcessCar(car, dT2);
                Application.Current.Dispatcher.Invoke(() => MoveCarOnCanvas(car, 400));
                Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
            }
        }

        private void CreateExitBarriers(int gates2)
        {
            foreach (var element in canvas.Children.OfType<Rectangle>().Where(r => r.Name?.StartsWith("exitBarrier") == true).ToList())
            {
                canvas.Children.Remove(element);
            }

            for (int i = 0; i < gates2; i++)
            {
                Rectangle exitBarrier = new Rectangle
                {
                    Width = 20,
                    Height = 35,
                    Fill = Brushes.Red,
                    Name = $"exitBarrier_{i}"
                };
                double laneHeight = 50;
                double topPosition = 50 + i * laneHeight;
                Canvas.SetLeft(exitBarrier, 490);
                Canvas.SetTop(exitBarrier, topPosition);

                canvas.Children.Add(exitBarrier);
            }
        }

        private void ReassignCarToExitLane(Car car, int gatesOnExit)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double laneHeight = 50;
                double newTopPosition = 60 + _random.Next(gatesOnExit) * laneHeight;
                Canvas.SetTop(carElement, newTopPosition);
            }
        }

        private void MoveCarOnCanvas(Car car, double newPosition)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                Canvas.SetLeft(carElement, newPosition);
            }
        }

        private async Task MoveCarRight(Car car, double newPosition)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double currentLeft = Canvas.GetLeft(carElement);
                double step = 3;
                int delay = 10;
                var carInQueue = _cars
                    .Where(c => Canvas.GetTop(c) == Canvas.GetTop(carElement) && Canvas.GetLeft(c) > currentLeft)
                    .OrderBy(c => Canvas.GetLeft(c))
                    .FirstOrDefault();
                if (carInQueue != null)
                {
                    newPosition = Math.Min(newPosition, Canvas.GetLeft(carInQueue) - 40);
                }
                while (currentLeft < newPosition)
                {
                    currentLeft += step;
                    Canvas.SetLeft(carElement, currentLeft);
                    await Task.Delay(delay);
                    if (carInQueue != null)
                    {
                        double carInQueueLeft = Canvas.GetLeft(carInQueue);
                        if (currentLeft + 40 > carInQueueLeft)
                        {
                            break;
                        }
                    }
                }
                Canvas.SetLeft(carElement, newPosition);
            }
        }
        private async Task<bool> IsCarMoving(Car car)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double initialLeft = Canvas.GetLeft(carElement);
                await Task.Delay(100);
                double currentLeft = Canvas.GetLeft(carElement);
                return Math.Abs(currentLeft - initialLeft) > 0.1;
            }
            return false;
        }
        private async Task MoveCarBetweenTollBooths(Car car, double startPosition, double endPosition, int dT3)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double currentLeft = Canvas.GetLeft(carElement);
                double totalDistance = endPosition - startPosition;
                int delay = 10;
                int steps = dT3 / delay;
                double stepDistance = totalDistance / steps; 
                for (int i = 0; i < steps; i++)
                {
                    currentLeft += stepDistance;
                    var carInQueue = _cars
                        .Where(c => Canvas.GetTop(c) == Canvas.GetTop(carElement) && Canvas.GetLeft(c) > currentLeft)
                        .OrderBy(c => Canvas.GetLeft(c))
                        .FirstOrDefault();
                    if (carInQueue != null)
                    {
                        double carInQueueLeft = Canvas.GetLeft(carInQueue);
                        if (currentLeft + 40 > carInQueueLeft)
                        {
                            var carInQueueTag = (Car)carInQueue.Tag;
                            if (!await IsCarMoving(carInQueueTag))
                            {
                                break;
                            }
                        }
                    }
                    Canvas.SetLeft(carElement, currentLeft);

                    await Task.Delay(delay);
                }
                Canvas.SetLeft(carElement, endPosition);
            }
        }

        private async Task MoveCarTop(Car car)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                while (Canvas.GetTop(carElement) < 320.0)
                {
                    double currentTop = Canvas.GetTop(carElement);
                    Canvas.SetTop(carElement, currentTop - 5);
                    await Task.Delay(10);
                }
                Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
            }
        }

        private void RemoveCarFromCanvas(Car car)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                canvas.Children.Remove(carElement);
                _cars.Remove(carElement);
            }
        }
        private void SetBarrierState(Rectangle barrier, bool isGreen)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isGreen)
                {
                    barrier.Fill = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    barrier.Fill = new SolidColorBrush(Colors.Red);
                }
            });
        }

        private SolidColorBrush GetRandomColor()
        {
            byte[] colorBytes = new byte[3];
            _random.NextBytes(colorBytes);
            return new SolidColorBrush(Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]));
        }
    }
}