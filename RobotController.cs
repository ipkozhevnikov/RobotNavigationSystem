using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;


namespace RobotLocalization
{
    public class RobotController
    {
        private UdpClient _udp;
        private Thread _thread;
        private bool _running;
        private bool _connected;

        private readonly object _lock = new object();

        public event Action<RobotState> RobotStateReceived;

        public event Action<string> LogMessage;

        // состояние робота
        public class RobotState
        {
            public int n { get; set; }
            public int s { get; set; }
            public int c { get; set; }
            public int le { get; set; }
            public int re { get; set; }
            public int az { get; set; }
            public int b { get; set; }
            public int d0 { get; set; }
            public int d1 { get; set; }
            public int d2 { get; set; }
            public int d3 { get; set; }
            public int d4 { get; set; }
            public int d5 { get; set; }
            public int d6 { get; set; }
            public int d7 { get; set; }
        }


        private RobotState _state = new RobotState();

        // параметры подключения
        private readonly int _localPort;
        private readonly int _remotePort;
        private readonly string _remoteIp;

        // номер команды для отправки
        private int _nextCommandNumber = 1;

        public bool IsConnected => _connected;

        public RobotController(int localPort, int remotePort, string remoteIp)
        {
            _localPort = localPort;
            _remotePort = remotePort;
            _remoteIp = remoteIp;
        }

        // -----------------------------
        // Подключение
        // -----------------------------
        public void Connect()
        {
            try
            {
                _udp = new UdpClient(_localPort);
                //_udp.Client.ReceiveTimeout = 2000;

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

            try { _udp?.Close(); } catch { }
            _udp = null;
        }

        // -----------------------------
        // Получение состояния робота
        // -----------------------------
        public RobotState GetState()
        {
            lock (_lock)
            {
                return new RobotState
                {
                    n = _state.n,
                    le = _state.le,
                    re = _state.re,
                    b = _state.b,
                    c = _state.c
                };
            }
        }

        // -----------------------------
        // Поток приёма данных
        // -----------------------------
        private void ReceiveLoop()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                try
                {
                    byte[] data = _udp.Receive(ref remote);
                    string json = Encoding.ASCII.GetString(data);
                    
                    RobotState parsed = JsonSerializer.Deserialize<RobotState>(json, new JsonSerializerOptions
                    {
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    });

                    if (parsed != null)
                    {
                        lock (_lock)
                        {
                            _state = parsed;

                            // обновляем номер следующей команды
                            // чтобы не было рассинхронизации с роботом
                            var n = parsed.n;
                            if (n >= _nextCommandNumber)
                                _nextCommandNumber = n + 1;
                        }

                        // вызываем событие
                        RobotStateReceived?.Invoke(parsed);
                    }
                }
                catch (SocketException)
                {
                    continue; // таймаут — слушаем дальше
                }
                catch
                {
                    _connected = false;
                    break;
                }
            }
        }

        // -----------------------------
        // Отправка команды роботу
        // -----------------------------
        public void SendCommand(int F, int B)
        {
            if (!_connected || _udp == null)
                return;

            if (F < -100) F = -100;
            if (F > 100) F = 100;
            if (B < -100) B = -100;
            if (B > 100) B = 100;

            int cmd = Interlocked.Increment(ref _nextCommandNumber);

            var command = new
            {
                N = cmd,
                M = 0,
                F = F,
                B = B,
                T = 0
            };

            string json = JsonSerializer.Serialize(command);
            Log(json);
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(json + "\n");
                _udp.Send(bytes, bytes.Length, _remoteIp, _remotePort);
            }
            catch
            {
                _connected = false;
            }
        }

        private void Log(string message)
        {
            LogMessage?.Invoke(message);
        }
    }
}
