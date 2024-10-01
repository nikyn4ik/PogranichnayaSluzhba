using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PR2.Models;

namespace PR2
{
    public partial class MainWindow : Window
    {
        private int _carIdFromOtherHighway = 1000;
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
            int gates2 = int.Parse(txtGates2.Text); // Здесь мы считываем значение gates2
            int dT4 = int.Parse(txtDT4.Text);

            double P = exitProbability * 0.01;
            double avgTime = (dT1 + dT2 + (P * dT3) + ((1 - P) * (dT3 + dT4 + dT2))) / 1000;

            txtAvgTime.Text = $"Среднее время проезда: {avgTime.ToString("0.00")} сек";

            // Создаем шлагбаумы в соответствии с пользовательским вводом
            Application.Current.Dispatcher.Invoke(() => CreateEntryBarriers(gates1, gates2));
            Application.Current.Dispatcher.Invoke(() => CreateExitBarriers(gates2, gates1));

            TollBooth tollBooth1 = new TollBooth(gates1);
            TollBooth tollBooth2 = new TollBooth(gates2);

            // Очистить предыдущие линии
            //canvas.Children.Clear();

            // Получить количество шлагбаумов с интерфейса
            int gates_1 = int.Parse(txtGates1.Text);
            int gates_2 = int.Parse(txtGates2.Text);

            // Определить количество линий (дорожек)
            int minGates = Math.Min(gates_1, gates_2);

            // Начальная позиция для линий
            double entryXStart = 50;
            double entryYStart = 50;
            double exitXStart = 510;
            double exitYStart = 50;
            double lineSpacing = 50; // расстояние между линиями

            // Рисуем линии для въезда
            for (int i = 0; i < gates_1; i++)
            {
                Line line = new Line
                {
                    X1 = entryXStart,
                    Y1 = entryYStart + i * lineSpacing,
                    X2 = 180,
                    Y2 = entryYStart + i * lineSpacing,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                canvas.Children.Add(line);
            }

            // Рисуем линии для выезда
            for (int i = 0; i < gates2 + 1; i++)
            {
                Line line = new Line
                {
                    X1 = exitXStart,
                    Y1 = exitYStart + i * lineSpacing,
                    X2 = 640,
                    Y2 = exitYStart + i * lineSpacing,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                canvas.Children.Add(line);
            }

            // Рисуем соединительные линии
            for (int i = 0; i < minGates + 1; i++)
            {
                Line connectorLine = new Line
                {
                    X1 = 180, // конец линии для въезда
                    Y1 = entryYStart + i * lineSpacing,
                    X2 = 510, // начало линии для выезда
                    Y2 = exitYStart + i * lineSpacing,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection() { 2, 2 } // пунктирная линия
                };
                canvas.Children.Add(connectorLine);
            }

            Task.Run(() => Application.Current.Dispatcher.Invoke(() => GenerateCarsFromOtherHighway(tollBooth2, dT2, dT4, gates2, exitProbability, dT3)));
            Task.Run(() => StartSimulation(gates1, gates2, tollBooth1, tollBooth2, dT1, dT2, dT3, exitProbability, dT4));

        }


        private async Task StartSimulation(int gates1, int gates2, TollBooth tollBooth1, TollBooth tollBooth2, int dT1, int dT2, int dT3, int exitProbability, int dT4)
        {
            int carId = 1;

            while (true)
            {
                await Task.Delay(dT1); // Интервал перед появлением следующего автомобиля

                Task.Run(async () =>
                {
                    var car = new Car { Id = carId++ };

                    // Добавляем машину на канвас
                    Application.Current.Dispatcher.Invoke(() => AddCarToCanvas(car, gates1));
                    //Application.Current.Dispatcher.Invoke(() => GenerateCarsFromOtherHighway(tollBooth2, dT2, dT4, gates2, exitProbability, dT3));

                    // Проход через первый пункт оплаты
                    await Application.Current.Dispatcher.Invoke(() => MoveCarToEntrance(car, 175, dT2));

                    await tollBooth1.ProcessCar(car, dT2);

                    // Перемещение между пунктами оплаты
                    await Task.Delay(dT3);

                    // Перераспределение на второй пункт оплаты
                    Application.Current.Dispatcher.Invoke(() => ReassignCarToExitLane(car, gates2));

                    // await Application.Current.Dispatcher.Invoke(() => MoveCarBetweenTollBooths(car, 185, 485, dT3));
                    await Application.Current.Dispatcher.Invoke(() => MoveCarToEntrance(car, 485, dT3));

                    // Проверка вероятности съезда с шоссе
                    if (_random.Next(100) < exitProbability)
                    {
                        Application.Current.Dispatcher.Invoke(() => MoveCarTop(car));
                        return;
                    }

                    // Проход через второй пункт оплаты
                    await tollBooth2.ProcessCar(car, dT2);

                    // Используем новый метод для перемещения на втором пункте оплаты
                    //await Application.Current.Dispatcher.Invoke(() => MoveCarRight(car, 650));
                    await Application.Current.Dispatcher.Invoke(() => MoveCarToEntrance(car, 800, dT2));


                    // Удаление автомобиля после прохождения второго пункта
                    Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
                });

            }
        }

        private void AddCarToCanvas(Car car, int barrier)
        {
            // Создаем прямоугольник для визуализации автомобиля с меньшими размерами
            Rectangle carRectangle = new Rectangle
            {
                Width = 30, // Ширина автомобиля
                Height = 15, // Высота автомобиля
                Fill = GetRandomColor() // Устанавливаем случайный цвет для каждой новой машины
            };

            // Создаем текстовый блок для отображения Id машины
            TextBlock carIdText = new TextBlock
            {
                Text = car.Id.ToString(), // Используем Id автомобиля для отображения
                Foreground = Brushes.White,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Создаем Grid, который будет содержать и прямоугольник, и текст
            Grid carGrid = new Grid
            {
                Width = 30,
                Height = 15
            };

            // Добавляем прямоугольник и текст в Grid
            carGrid.Children.Add(carRectangle);
            carGrid.Children.Add(carIdText);

            // Определяем количество линий в зависимости от числа шлагбаумов
            double laneHeight = 50; // Расстояние между линиями
            double topPosition = 60 + _random.Next(barrier) * laneHeight; // Расчет позиции по вертикали

            // Устанавливаем начальные позиции для Grid
            Canvas.SetLeft(carGrid, 10); // Начальная позиция по горизонтали
            Canvas.SetTop(carGrid, topPosition); // Позиция по вертикали в зависимости от линии

            // Устанавливаем объект Car в тег для будущего использования
            carGrid.Tag = car;

            // Добавляем Grid с машиной в список и на Canvas
            _cars.Add(carGrid);
            canvas.Children.Add(carGrid);
        }

        private void CreateEntryBarriers(int gates1, int gates2)
        {
            // Очищаем все предыдущие шлагбаумы, если они были созданы ранее
            foreach (var element in canvas.Children.OfType<Rectangle>().Where(r => r.Name?.StartsWith("entryBarrier") == true).ToList())
            {
                canvas.Children.Remove(element);
            }

            // Создаем шлагбаумы на въезд
            double topPosition = 0;
            for (int i = 0; i < gates1; i++)
            {
                Rectangle entryBarrier = new Rectangle
                {
                    Width = 20,
                    Height = 35, // Высота шлагбаума
                    Fill = Brushes.Red,
                    Name = $"entryBarrier_{i}"
                };

                double laneHeight = 50; // Расстояние между полосами
                topPosition = 50 + i * laneHeight; // Координата по вертикали для каждого шлагбаума

                // Устанавливаем координаты шлагбаума на канвасе
                Canvas.SetLeft(entryBarrier, 180); // Фиксированное значение по X (въезд)
                Canvas.SetTop(entryBarrier, topPosition); // Динамическое значение по Y для выравнивания с машиной

                canvas.Children.Add(entryBarrier); // Добавляем шлагбаум на канвас
            }

            if (gates1 >= gates2)
            {
                Linebott1.Y2 = topPosition + 50;
                Linebott2.Y2 = topPosition + 50;
            }
        }

        private void CreateExitBarriers(int gates2, int gates1)
        {
            // Очищаем все предыдущие шлагбаумы, если они были созданы ранее
            foreach (var element in canvas.Children.OfType<Rectangle>().Where(r => r.Name?.StartsWith("exitBarrier") == true).ToList())
            {
                canvas.Children.Remove(element);
            }

            // Создаем шлагбаумы на выезд
            double topPosition = 0;
            for (int i = 0; i < gates2; i++)
            {
                Rectangle exitBarrier = new Rectangle
                {
                    Width = 20,
                    Height = 35, // Высота шлагбаума
                    Fill = Brushes.Red,
                    Name = $"exitBarrier_{i}"
                };

                double laneHeight = 50; // Расстояние между полосами
                topPosition = 50 + i * laneHeight; // Координата по вертикали для каждого шлагбаума

                // Устанавливаем координаты шлагбаума на канвасе
                Canvas.SetLeft(exitBarrier, 490); // Фиксированное значение по X (выезд)
                Canvas.SetTop(exitBarrier, topPosition); // Динамическое значение по Y для выравнивания с машиной

                canvas.Children.Add(exitBarrier); // Добавляем шлагбаум на канвас
            }

            if (gates1 <= gates2)
            {
                Linebott1.Y2 = topPosition + 50;
                Linebott2.Y2 = topPosition + 50;
            }
        }

        private async Task GenerateCarsFromOtherHighway(TollBooth tollBooth2, int dT2, int dT4, int gates2, int exitProbability, int dT3)
        {
            Random random = new Random();

            while (true)
            {
                await Task.Delay(dT4);


                // await Task.Delay(random.Next(dT4 / 2, dT4 * 2)); // Случайное время ожидания перед появлением следующего автомобиля

                var car = new Car { Id = _carIdFromOtherHighway++ };

                // Создаем прямоугольник для визуализации автомобиля
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
                double topPosition = 380 + _random.Next(gates2) * laneHeight;

                Canvas.SetLeft(carGrid, 298);
                Canvas.SetTop(carGrid, topPosition);

                carGrid.Tag = car;
                _cars.Add(carGrid);
                canvas.Children.Add(carGrid);

                // Проверка возможности поднятия
                var carInQueue = _cars
                    .Where(c => Canvas.GetTop(c) == 60 && Canvas.GetLeft(c) > 298) // Проверка на основном шоссе
                    .OrderBy(c => Canvas.GetLeft(c))
                    .FirstOrDefault();

                if (carInQueue == null || Canvas.GetLeft(carInQueue) > 340) // Условие для подъема
                {
                    // Передвигаем машину вверх на основное шоссе
                    await Application.Current.Dispatcher.Invoke(() => MoveCarUp(car));
                    // Перераспределяем машину на полосу основного шоссе
                    Application.Current.Dispatcher.Invoke(() => ReassignCarToExitLane(car, gates2));

                    // Теперь машина на главном шоссе, перемещаем вправо через первый пункт оплаты
                    await Application.Current.Dispatcher.Invoke(() => MoveCarToEntrance(car, 485, dT3));

                    if (_random.Next(100) < exitProbability)
                    {
                        Application.Current.Dispatcher.Invoke(() => MoveCarTop(car));
                        return;
                    }

                    await tollBooth2.ProcessCar(car, dT2);
                    await Application.Current.Dispatcher.Invoke(() => MoveCarToEntrance(car, 800, dT2));

                    // Удаление автомобиля после прохождения второго пункта
                    Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
                }
            }
        }


        private async Task MoveCarUp(Car car)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double currentTop = Canvas.GetTop(carElement);
                double targetTop = 60.0; // Высота главного шоссе

                // Определение шага перемещения и времени задержки
                double step = 5.0; // Шаг перемещения вверх
                int delayTime = 10; // Задержка для плавности

                // Перемещение автомобиля к новой позиции
                while (currentTop > targetTop)
                {
                    currentTop -= step; // Уменьшаем текущую позицию
                    Canvas.SetTop(carElement, currentTop);
                    await Task.Delay(delayTime); // Задержка для плавного движения
                }

                // Убедимся, что машина достигла конечной позиции
                Canvas.SetTop(carElement, targetTop);
            }
        }

        private void ReassignCarToExitLane(Car car, int gatesOnExit)
        {
            // Найти элемент на canvas по идентификатору машины
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                // Определяем количество шлагбаумов на втором пункте (exit)
                double laneHeight = 50;
                double newTopPosition = 60 + _random.Next(gatesOnExit) * laneHeight; // Перераспределяем на доступную полосу

                // Устанавливаем новую вертикальную позицию на втором пункте
                Canvas.SetTop(carElement, newTopPosition);
            }
        }

        private void MoveCarOnCanvas(Car car, double newPosition)
        {
            // Найти элемент на canvas по идентификатору машины и изменить его положение
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                Canvas.SetLeft(carElement, newPosition);
            }
        }


        private async Task MoveCarTop(Car car)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                //Canvas.GetLeft(3);
                while (Canvas.GetTop(carElement) < 320.0)
                {
                    double currentTop = Canvas.GetTop(carElement);
                    Canvas.SetTop(carElement, currentTop - 5);
                    await Task.Delay(10); // Задержка для плавности
                }
                Application.Current.Dispatcher.Invoke(() => RemoveCarFromCanvas(car));
            }
        }

        private void RemoveCarFromCanvas(Car car)
        {
            // Найти элемент на canvas по идентификатору машины и удалить его
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                canvas.Children.Remove(carElement);
                _cars.Remove(carElement);
            }
        }

        private SolidColorBrush GetRandomColor()
        {
            // Генерация случайного цвета
            byte[] colorBytes = new byte[3];
            _random.NextBytes(colorBytes);
            return new SolidColorBrush(Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]));
        }

        private async Task MoveCarToEntrance(Car car, double targetPosition, int dT)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double currentLeft = Canvas.GetLeft(carElement);

                // Рассчитываем общее расстояние, которое машине нужно проехать
                double distanceToTarget = targetPosition - currentLeft;

                // Определяем количество шагов за все время (делаем шаг каждые 10 мс, например)
                int totalSteps = dT / 10; // Сколько шагов сделаем за dT мс (каждый шаг через 10 мс)

                // Рассчитываем, на сколько пикселей сдвигать машину за один шаг
                double step = distanceToTarget / totalSteps;

                // Определяем задержку между шагами
                int delay = 10;  // 10 мс задержка между шагами

                // Основной цикл движения
                while (currentLeft < targetPosition)
                {
                    var carInQueue = _cars
                        .Where(c => Canvas.GetTop(c) == Canvas.GetTop(carElement) && Canvas.GetLeft(c) > currentLeft)
                        .OrderBy(c => Canvas.GetLeft(c))
                        .FirstOrDefault();

                    bool shouldStop = false;

                    if (carInQueue != null)
                    {
                        double carInQueueLeft = Canvas.GetLeft(carInQueue);
                        if (carInQueueLeft - currentLeft < 60)
                        {
                            currentLeft = carInQueueLeft - 40; // Остановка перед машиной
                            shouldStop = true;
                        }
                    }

                    if (!shouldStop)
                    {
                        currentLeft += step;
                    }

                    // Корректируем позицию, если она уже превысила targetPosition
                    if (currentLeft > targetPosition)
                    {
                        currentLeft = targetPosition;
                    }

                    Canvas.SetLeft(carElement, currentLeft);

                    await Task.Delay(delay);
                }

                // Убедимся, что машина точно установлена в конечной точке
                Canvas.SetLeft(carElement, targetPosition);
            }
        }


        private async Task MoveCarBetweenTollBooths(Car car, double startPosition, double endPosition, int dT3)
        {
            var carElement = _cars.Find(c => ((Car)c.Tag).Id == car.Id);
            if (carElement != null)
            {
                double currentLeft = Canvas.GetLeft(carElement);
                double totalDistance = endPosition - startPosition;

                // Рассчитываем количество шагов на основе dT3 (время перемещения между пунктами)
                int delay = 10; // Время задержки между шагами (мс)
                int steps = dT3 / delay; // Количество шагов, за которые машина пройдет дистанцию
                double stepDistance = totalDistance / steps; // Расстояние, которое машина проходит за один шаг

                // Плавное движение к новой позиции
                for (int i = 0; i < steps; i++)
                {
                    currentLeft += stepDistance;

                    // Проверяем наличие машин впереди
                    var carInQueue = _cars
                        .Where(c => Canvas.GetTop(c) == Canvas.GetTop(carElement) && Canvas.GetLeft(c) > currentLeft)
                        .OrderBy(c => Canvas.GetLeft(c))
                        .FirstOrDefault();

                    // Если перед машиной есть другая машина, проверяем ее положение
                    if (carInQueue != null)
                    {
                        double carInQueueLeft = Canvas.GetLeft(carInQueue);

                        // Проверяем, слишком ли близко машины друг к другу
                        if (currentLeft + 40 > carInQueueLeft)
                        {
                            // Если расстояние меньше 40, останавливаем движение
                            currentLeft = carInQueueLeft - 40; // Остановка перед машиной
                            break; // Выход из цикла
                        }
                    }

                    // Двигаем машину
                    Canvas.SetLeft(carElement, currentLeft);
                    await Task.Delay(delay);
                }

                // Убедимся, что машина достигла конечной позиции
                Canvas.SetLeft(carElement, endPosition);
            }
        }

    }
}
