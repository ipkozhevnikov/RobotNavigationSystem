using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static RobotLocalization.RobotLocalizer;

namespace RobotLocalization
{

    public class LidarDataManager
    {
        private UdpClient _udp;
        private Thread _thread;
        private bool _running;
        private bool _connected;

        private readonly object _lock = new object();
        private int[] _lastDistances = null;

        public bool IsConnected => _connected;

        private PoseFine _robotPosition;
        public PoseFine RobotPosition => _robotPosition;

        public event Action<int[]> LidarDataReceived;

        // -----------------------------
        // Подключение
        // -----------------------------
        public void Connect(int port)
        {
            try
            {
                _udp = new UdpClient(port);

                _running = true;
                _connected = true;

                _thread = new Thread(ReceiveLoop);
                _thread.IsBackground = true;
                _thread.Start();
            }
            catch
            {
                _connected = false;
            }
        }

        // -----------------------------
        // Отключение
        // -----------------------------
        public void Disconnect()
        {
            _running = false;
            _connected = false;

            try
            {
                _udp?.Close();
            }
            catch { }

            _udp = null;
        }

        // -----------------------------
        // Получение последних данных
        // -----------------------------
        public int[] GetDistances()
        {
            lock (_lock)
            {
                if (_lastDistances == null) return null;
                return (int[])_lastDistances.Clone();
            }
        }

        // -----------------------------
        // Основной поток приёма UDP
        // -----------------------------
        private void ReceiveLoop()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                try
                {
                    byte[] data = _udp.Receive(ref remote);
                    string text = System.Text.Encoding.ASCII.GetString(data);
                    // Парсим строку
                    var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 10)
                        continue;

                    int[] distances = new int[parts.Length];

                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (int.TryParse(parts[i], out int v))
                            distances[i] = v;
                        else
                            distances[i] = 0;
                    }

                    lock (_lock)
                    {
                        _lastDistances = distances;
                    }
                    LidarDataReceived?.Invoke(_lastDistances);
                }
                catch (SocketException)
                {
                    // таймаут или ошибка — продолжаем слушать
                    continue;
                }
                catch
                {
                    // любая другая ошибка — отключаемся
                    _connected = false;
                    break;
                }
            }
        }

        // -----------------------------
        // Отрисовка кадра лидара
        // -----------------------------
        public Bitmap DrawFrame(int width,int height)
        {
            int[] distances = GetDistances();
            if (distances == null)
                return null;

            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Переворот координат
                g.TranslateTransform(width, height);
                g.ScaleTransform(-1, -1);

                float centerX = width / 2f;
                float centerY = height / 2f;
                float scale = width / 16000f;

                // Робот
                float robotRadius = 7.5f;
                g.FillEllipse(Brushes.Gold,
                    centerX - robotRadius,
                    centerY - robotRadius,
                    robotRadius * 2,
                    robotRadius * 2);

                // Стрелка направления (вверх)
                float arrowLength = 7.5f;
                float arrowX = centerX + arrowLength * (float)Math.Cos(90 * Math.PI / 180);
                float arrowY = centerY + arrowLength * (float)Math.Sin(90 * Math.PI / 180);
                g.DrawLine(new Pen(Color.Green, 1), centerX, centerY, arrowX, arrowY);
               
                // Точки лидара
                for (int i = 0; i < distances.Length; i++)
                {
                    float angle = i - 90;
                    float distance = distances[i];

                    float x = centerX + distance * scale * (float)Math.Cos(angle * Math.PI / 180);
                    float y = centerY + distance * scale * (float)Math.Sin(angle * Math.PI / 180);

                    g.FillEllipse(Brushes.Blue, x - 1, y - 1, 2, 2);
                }
            }

            return bitmap;
        }
    }

}
