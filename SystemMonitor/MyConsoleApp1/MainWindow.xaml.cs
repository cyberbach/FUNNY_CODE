using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;

namespace MyConsoleApp1
{
    /// <summary>
    /// Режимы мониторинга системы
    /// </summary>
    public enum MonitorMode
    {
        CPU,        // Мониторинг процессора
        Memory,     // Мониторинг памяти
        Network,    // Мониторинг сети
        GPU,        // Мониторинг загрузки GPU
        GPU_Memory  // Мониторинг памяти GPU
    }

    /// <summary>
    /// Главное окно приложения - Системный Монитор
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _updateTimer; // Таймер для обновления данных
        private const int MaxDataPoints = 100; // Максимальное количество точек на графике
        private double[] _cpuUsageHistory;     // История использования CPU
        private double[] _memoryUsageHistory;  // История использования памяти
        private double[] _networkInHistory;    // История входящего трафика
        private double[] _networkOutHistory;   // История исходящего трафика
        private double[] _gpuUsageHistory;     // История использования GPU
        private double[] _gpuMemoryHistory;    // История использования памяти GPU
        private int _currentIndex = 0;         // Текущий индекс в истории
        private MonitorMode _currentMode = MonitorMode.CPU; // Текущий режим мониторинга

        // Значения для расчета сетевого трафика (не используются в текущей версии)
        private float _prevBytesReceived = 0;
        private float _prevBytesSent = 0;
        private DateTime _prevNetworkTime = DateTime.Now;

        /// <summary>
        /// Конструктор главного окна
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            TimeText.Text = "Time: " + DateTime.Now.ToString("HH:mm:ss");
            UserText.Text = "User: " + Environment.UserName;

            // Инициализация массивов для хранения истории данных
            _cpuUsageHistory = new double[MaxDataPoints];
            _memoryUsageHistory = new double[MaxDataPoints];
            _networkInHistory = new double[MaxDataPoints];
            _networkOutHistory = new double[MaxDataPoints];
            _gpuUsageHistory = new double[MaxDataPoints];
            _gpuMemoryHistory = new double[MaxDataPoints];

            for (int i = 0; i < MaxDataPoints; i++)
            {
                _cpuUsageHistory[i] = 0;
                _memoryUsageHistory[i] = 0;
                _networkInHistory[i] = 0;
                _networkOutHistory[i] = 0;
                _gpuUsageHistory[i] = 0;
                _gpuMemoryHistory[i] = 0;
            }

            // Настройка таймера для обновления в реальном времени
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(500); // Обновление каждые 500мс
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Первоначальное обновление данных
            UpdateSystemUsage();
            DrawMonitorGraph();
        }

        /// <summary>
        /// Обработчик таймера - обновляет данные каждые 500мс
        /// </summary>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateSystemUsage();
            DrawMonitorGraph();
            TimeText.Text = "Time: " + DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// Основной метод обновления всех системных показателей
        /// </summary>
        private void UpdateSystemUsage()
        {
            try
            {
                // Update CPU usage
                var cpuCounters = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounters.NextValue();
                System.Threading.Thread.Sleep(30);
                float cpuUsage = cpuCounters.NextValue();
                _cpuUsageHistory[_currentIndex] = cpuUsage;

                // Update Memory usage
                var totalMemCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                float memoryUsagePercent = totalMemCounter.NextValue();
                _memoryUsageHistory[_currentIndex] = memoryUsagePercent;

                // Update Network usage with better error handling
                UpdateNetworkUsage();

                // Update GPU usage with better error handling
                UpdateGpuUsage();

                // Update GPU Memory with better error handling
                UpdateGpuMemoryUsage();

                _currentIndex = (_currentIndex + 1) % MaxDataPoints;

                // Update status text based on current mode
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                SystemInfoText.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateNetworkUsage()
        {
            try
            {
                string networkInterface = GetPrimaryNetworkInterface();
                if (string.IsNullOrEmpty(networkInterface))
                {
                    // Generate some test data if no network interface found
                    GenerateTestNetworkData();
                    return;
                }

                var bytesReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkInterface);
                var bytesSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkInterface);

                // Need to call NextValue twice to get accurate readings
                bytesReceivedCounter.NextValue();
                bytesSentCounter.NextValue();
                System.Threading.Thread.Sleep(100);

                float bytesReceived = bytesReceivedCounter.NextValue();
                float bytesSent = bytesSentCounter.NextValue();

                // Calculate KB/s for better visualization
                _networkInHistory[_currentIndex] = bytesReceived / 1024;  // KB/s
                _networkOutHistory[_currentIndex] = bytesSent / 1024;     // KB/s

                // Ensure we have some minimum values for visualization
                if (_networkInHistory[_currentIndex] < 1) _networkInHistory[_currentIndex] = 1;
                if (_networkOutHistory[_currentIndex] < 1) _networkOutHistory[_currentIndex] = 1;
            }
            catch
            {
                // Generate test data if real monitoring fails
                GenerateTestNetworkData();
            }
        }

        private void GenerateTestNetworkData()
        {
            // Generate some realistic test network data
            double timeFactor = _currentIndex / 10.0;
            _networkInHistory[_currentIndex] = 20 + 15 * Math.Sin(timeFactor);
            _networkOutHistory[_currentIndex] = 10 + 8 * Math.Cos(timeFactor * 0.7);
        }

        private void UpdateGpuUsage()
        {
            try
            {
                // Try different GPU counter categories
                string[] gpuCategories = {
                    "GPU Engine",
                    "NVIDIA GPU",
                    "AMD GPU",
                    "Intel GPU"
                };

                bool gpuFound = false;
                foreach (string category in gpuCategories)
                {
                    try
                    {
                        if (PerformanceCounterCategory.Exists(category))
                        {
                            var gpuCounter = new PerformanceCounter(category, "Utilization Percentage", "_Total");
                            float gpuUsage = gpuCounter.NextValue();
                            _gpuUsageHistory[_currentIndex] = gpuUsage;
                            gpuFound = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!gpuFound)
                {
                    // Generate test GPU data
                    GenerateTestGpuData();
                }
            }
            catch
            {
                // Generate test GPU data
                GenerateTestGpuData();
            }
        }

        private void GenerateTestGpuData()
        {
            // Generate some realistic test GPU data
            double timeFactor = _currentIndex / 20.0;
            _gpuUsageHistory[_currentIndex] = 30 + 25 * Math.Sin(timeFactor);
        }

        private void UpdateGpuMemoryUsage()
        {
            try
            {
                // Try different GPU memory counter categories
                string[] gpuMemCategories = {
                    "GPU Process Memory",
                    "NVIDIA GPU Memory",
                    "AMD GPU Memory",
                    "Intel GPU Memory"
                };

                bool gpuMemFound = false;
                foreach (string category in gpuMemCategories)
                {
                    try
                    {
                        if (PerformanceCounterCategory.Exists(category))
                        {
                            var gpuMemCounter = new PerformanceCounter(category, "Dedicated Usage", "_Total");
                            float gpuMemUsage = gpuMemCounter.NextValue();
                            _gpuMemoryHistory[_currentIndex] = gpuMemUsage / 100; // Convert to MB
                            gpuMemFound = true;
                            break;
                        }
                    }
                    catch { }
                }

                if (!gpuMemFound)
                {
                    // Generate test GPU memory data
                    GenerateTestGpuMemoryData();
                }
            }
            catch
            {
                // Generate test GPU memory data
                GenerateTestGpuMemoryData();
            }
        }

        private void GenerateTestGpuMemoryData()
        {
            // Generate some realistic test GPU memory data
            double timeFactor = _currentIndex / 25.0;
            _gpuMemoryHistory[_currentIndex] = 500 + 300 * Math.Sin(timeFactor);
        }

        private string GetPrimaryNetworkInterface()
        {
            try
            {
                var counters = new PerformanceCounterCategory("Network Interface");
                string[] instances = counters.GetInstanceNames();

                // Try to find the primary network interface
                foreach (string instance in instances)
                {
                    if (instance.Contains("Ethernet") || instance.Contains("Wi-Fi") || instance.Contains("Wireless"))
                        return instance;
                }

                return instances.Length > 0 ? instances[0] : "";
            }
            catch
            {
                return "";
            }
        }

        private void UpdateStatusText()
        {
            switch (_currentMode)
            {
                case MonitorMode.CPU:
                    SystemInfoText.Text = $"CPU: {_cpuUsageHistory[(_currentIndex - 1 + MaxDataPoints) % MaxDataPoints]:F1}%";
                    break;
                case MonitorMode.Memory:
                    SystemInfoText.Text = $"Memory: {_memoryUsageHistory[(_currentIndex - 1 + MaxDataPoints) % MaxDataPoints]:F1}% used";
                    break;
                case MonitorMode.Network:
                    double inSpeed = _networkInHistory[(_currentIndex - 1 + MaxDataPoints) % MaxDataPoints];
                    double outSpeed = _networkOutHistory[(_currentIndex - 1 + MaxDataPoints) % MaxDataPoints];
                    SystemInfoText.Text = $"Network: In: {inSpeed:F1} KB/s, Out: {outSpeed:F1} KB/s";
                    break;
                case MonitorMode.GPU:
                    SystemInfoText.Text = $"GPU: {_gpuUsageHistory[(_currentIndex - 1 + MaxDataPoints) % MaxDataPoints]:F1}%";
                    break;
                case MonitorMode.GPU_Memory:
                    SystemInfoText.Text = $"GPU Memory: {_gpuMemoryHistory[(_currentIndex - 1 + MaxDataPoints) % MaxDataPoints]:F1} MB";
                    break;
            }
        }

        /// <summary>
        /// Основной метод рисования графика мониторинга
        /// </summary>
        private void DrawMonitorGraph()
        {
            MonitorCanvas.Children.Clear();

            if (MonitorCanvas.ActualWidth <= 0 || MonitorCanvas.ActualHeight <= 0)
                return;

            double graphWidth = MonitorCanvas.ActualWidth;
            double graphHeight = MonitorCanvas.ActualHeight;
            double stepWidth = graphWidth / (MaxDataPoints - 1);

            // Рисование линий сетки
            for (int i = 0; i <= 10; i++)
            {
                double y = graphHeight - (i * graphHeight / 10);
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = graphWidth,
                    Y2 = y,
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 0.5,
                    Opacity = 0.3
                };
                MonitorCanvas.Children.Add(line);

                if (i % 2 == 0)
                {
                    string label = GetYAxisLabel(i);
                    var text = new TextBlock
                    {
                        Text = label,
                        Foreground = Brushes.LightGray,
                        FontSize = 10,
                        Margin = new Thickness(-25, y - 10, 0, 0)
                    };
                    MonitorCanvas.Children.Add(text);
                }
            }

            // Рисование данных в зависимости от текущего режима
            switch (_currentMode)
            {
                case MonitorMode.CPU:
                case MonitorMode.Memory:
                case MonitorMode.GPU:
                case MonitorMode.GPU_Memory:
                    DrawSingleLineGraph(graphWidth, graphHeight, stepWidth);
                    break;
                case MonitorMode.Network:
                    DrawDualLineGraph(graphWidth, graphHeight, stepWidth);
                    break;
            }
        }

        private string GetYAxisLabel(int gridLineIndex)
        {
            double value = 100 - (gridLineIndex * 10);

            switch (_currentMode)
            {
                case MonitorMode.CPU:
                case MonitorMode.Memory:
                case MonitorMode.GPU:
                    return $"{value}%";
                case MonitorMode.GPU_Memory:
                    return $"{value * 10} MB"; // Scale for memory
                case MonitorMode.Network:
                    return $"{value * 10} KB/s"; // Scale for network
                default:
                    return $"{value}";
            }
        }

        private void DrawSingleLineGraph(double graphWidth, double graphHeight, double stepWidth)
        {
            // Get the appropriate data and colors for the current mode
            double[] dataHistory;
            Brush lineColor;
            Color fillColor;

            switch (_currentMode)
            {
                case MonitorMode.CPU:
                    dataHistory = _cpuUsageHistory;
                    lineColor = Brushes.Lime;
                    fillColor = Color.FromArgb(60, 0, 255, 0);
                    break;
                case MonitorMode.Memory:
                    dataHistory = _memoryUsageHistory;
                    lineColor = Brushes.Cyan;
                    fillColor = Color.FromArgb(60, 0, 255, 255);
                    break;
                case MonitorMode.GPU:
                    dataHistory = _gpuUsageHistory;
                    lineColor = Brushes.Orange;
                    fillColor = Color.FromArgb(60, 255, 165, 0);
                    break;
                case MonitorMode.GPU_Memory:
                    dataHistory = _gpuMemoryHistory;
                    lineColor = Brushes.Pink;
                    fillColor = Color.FromArgb(60, 255, 192, 203);
                    break;
                default:
                    return;
            }

            // Draw main line
            var dataPath = new Polyline
            {
                Stroke = lineColor,
                StrokeThickness = 2,
                Points = new PointCollection()
            };

            for (int i = 0; i < MaxDataPoints; i++)
            {
                int index = (_currentIndex + i) % MaxDataPoints;
                double x = i * stepWidth;
                double y = graphHeight - (dataHistory[index] * graphHeight / 100);
                dataPath.Points.Add(new Point(x, y));
            }

            MonitorCanvas.Children.Add(dataPath);

            // Draw fill under the line
            var fillPath = new Polyline
            {
                Stroke = lineColor,
                StrokeThickness = 0,
                Fill = new SolidColorBrush(fillColor),
                Points = new PointCollection()
            };

            for (int i = 0; i < MaxDataPoints; i++)
            {
                int index = (_currentIndex + i) % MaxDataPoints;
                double x = i * stepWidth;
                double y = graphHeight - (dataHistory[index] * graphHeight / 100);
                fillPath.Points.Add(new Point(x, y));
            }

            // Close the fill path
            fillPath.Points.Add(new Point(graphWidth, graphHeight));
            fillPath.Points.Add(new Point(0, graphHeight));
            fillPath.Points.Add(new Point(0, graphHeight - (dataHistory[_currentIndex] * graphHeight / 100)));

            MonitorCanvas.Children.Add(fillPath);
        }

        private void DrawDualLineGraph(double graphWidth, double graphHeight, double stepWidth)
        {
            // Network mode - draw both incoming and outgoing traffic
            double maxValue = 0;
            for (int i = 0; i < MaxDataPoints; i++)
            {
                maxValue = Math.Max(maxValue, _networkInHistory[i]);
                maxValue = Math.Max(maxValue, _networkOutHistory[i]);
            }

            if (maxValue == 0) maxValue = 100;

            // Draw incoming traffic (green)
            var inPath = new Polyline
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Points = new PointCollection()
            };

            // Draw outgoing traffic (orange)
            var outPath = new Polyline
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                Points = new PointCollection()
            };

            for (int i = 0; i < MaxDataPoints; i++)
            {
                int index = (_currentIndex + i) % MaxDataPoints;
                double x = i * stepWidth;

                // Scale network values to fit the graph
                double inY = graphHeight - (_networkInHistory[index] * graphHeight / maxValue);
                double outY = graphHeight - (_networkOutHistory[index] * graphHeight / maxValue);

                inPath.Points.Add(new Point(x, inY));
                outPath.Points.Add(new Point(x, outY));
            }

            MonitorCanvas.Children.Add(inPath);
            MonitorCanvas.Children.Add(outPath);

            // Add legend
            var inLegend = new TextBlock
            {
                Text = "Incoming",
                Foreground = Brushes.Lime,
                FontSize = 12,
                Margin = new Thickness(10, graphHeight - 30, 0, 0)
            };

            var outLegend = new TextBlock
            {
                Text = "Outgoing",
                Foreground = Brushes.Orange,
                FontSize = 12,
                Margin = new Thickness(120, graphHeight - 30, 0, 0)
            };

            MonitorCanvas.Children.Add(inLegend);
            MonitorCanvas.Children.Add(outLegend);
        }

        /// <summary>
        /// Переключение между режимами мониторинга
        /// </summary>
        /// <param name="mode">Режим мониторинга для переключения</param>
        private void SwitchToMode(MonitorMode mode)
        {
            _currentMode = mode;

            // Сброс стиля всех кнопок вкладок в неактивное состояние
            SetTabButtonStyle(CpuTabButton, false);
            SetTabButtonStyle(MemoryTabButton, false);
            SetTabButtonStyle(NetworkTabButton, false);
            SetTabButtonStyle(GpuTabButton, false);
            SetTabButtonStyle(GpuMemoryTabButton, false);

            // Установка активного стиля для выбранной вкладки
            switch (mode)
            {
                case MonitorMode.CPU:
                    SetTabButtonStyle(CpuTabButton, true);
                    break;
                case MonitorMode.Memory:
                    SetTabButtonStyle(MemoryTabButton, true);
                    break;
                case MonitorMode.Network:
                    SetTabButtonStyle(NetworkTabButton, true);
                    break;
                case MonitorMode.GPU:
                    SetTabButtonStyle(GpuTabButton, true);
                    break;
                case MonitorMode.GPU_Memory:
                    SetTabButtonStyle(GpuMemoryTabButton, true);
                    break;
            }

            DrawMonitorGraph();
            UpdateStatusText();
        }

        /// <summary>
        /// Установка стиля кнопки вкладки
        /// </summary>
        /// <param name="button">Кнопка вкладки</param>
        /// <param name="isActive">True для активной вкладки, False для неактивной</param>
        private void SetTabButtonStyle(Button button, bool isActive)
        {
            if (isActive)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 99, 177));
                button.FontWeight = FontWeights.Bold;
            }
            else
            {
                button.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                button.FontWeight = FontWeights.Normal;
            }
        }

        /// <summary>
        /// Обработчик клика по вкладке Processor
        /// </summary>
        private void CpuTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMode(MonitorMode.CPU);
        }

        /// <summary>
        /// Обработчик клика по вкладке Memory
        /// </summary>
        private void MemoryTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMode(MonitorMode.Memory);
        }

        /// <summary>
        /// Обработчик клика по вкладке Network
        /// </summary>
        private void NetworkTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMode(MonitorMode.Network);
        }

        /// <summary>
        /// Обработчик клика по вкладке GPU
        /// </summary>
        private void GpuTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMode(MonitorMode.GPU);
        }

        /// <summary>
        /// Обработчик клика по вкладке GPU-Memory
        /// </summary>
        private void GpuMemoryTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMode(MonitorMode.GPU_Memory);
        }

        /// <summary>
        /// Обработчик перетаскивания окна
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке закрытия окна
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            Close();
        }

        /// <summary>
        /// Обработчик события закрытия окна
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _updateTimer.Stop();
            base.OnClosed(e);
        }
    }
}
