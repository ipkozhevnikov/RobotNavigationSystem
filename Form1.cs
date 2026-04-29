using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using static RobotLocalization.RobotLocalizer;

namespace RobotLocalization
{
 
    public partial class Form1 : Form
    {
        private bool isPaused;

        private Map map;
        RobotLocalizer localizer;
        private LidarDataManager lidar;
        private RobotController robotController;
        private MotionController motionController;
        List<Cell> _path;

        public Form1()
        {
            InitializeComponent();
            UpdateUIState(false);
        }
        private void UpdateUIState(bool isRunning)
        {
            startButton.Text = isRunning ? "Отключиться" : "Подключиться";
            portTextBox.Enabled = !isRunning;
            LocalPortTextBox.Enabled = !isRunning;
            RemotePortTextBox.Enabled = !isRunning;
            RemoteIPTextBox.Enabled = !isRunning;
            pointsListBox.Enabled = isRunning;
            moveButton.Enabled = isRunning && pointsListBox.SelectedIndex >= 0;

            if (!isRunning && isPaused)
            {
                isPaused = false;
            }
        }
        private void startButton_Click(object sender, EventArgs e)
        {
            if (!lidar.IsConnected && !robotController.IsConnected)
            {
                lidar.Connect((int)portTextBox.Value);
                robotController.Connect();
            }
            else
            {
                lidar.Disconnect();
                motionController.Stop();
                robotController.Disconnect();
            }
            UpdateUIState(lidar.IsConnected);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            map = new Map();
            map.LoadFromFile("map.txt");
            map.PrintInterestPoints();

            var points = map.GetInterestPoints();
            foreach (var type in points.Keys)
            {
                // Для каждой точки данного типа
                foreach (var point in points[type])
                {
                    // Создаем строку для отображения
                    string displayText = $"Точка интереса";
                    var listItem = new
                    {
                        Display = displayText,
                        Type = type,
                        Point = point
                    };
                    // Добавляем в ListBox
                    pointsListBox.Items.Add(listItem);
                }
            }
            
            localizer = new RobotLocalizer(map.boolMap);
            lidar = new LidarDataManager();
            lidar.LidarDataReceived += OnLidarDataReceived;
            robotController = new RobotController(int.Parse(LocalPortTextBox.Text), int.Parse(RemotePortTextBox.Text), RemoteIPTextBox.Text);
            motionController = new MotionController(robotController, lidar, localizer, map);

            motionController.LogMessage += Log;
            robotController.LogMessage += Log;
        }

        private void OnLidarDataReceived(int[] lidarData)
        {
            Pose currentPose = localizer.LocalizeFast(lidarData);
            currentPose = localizer.RefinePose(lidarData, currentPose);

            Bitmap bmp = lidar.DrawFrame(lidarPictureBox.Width, lidarPictureBox.Height);
            if (bmp != null)
                lidarPictureBox.Image = bmp;

            Bitmap bmp2 = map.RenderMap(mapPictureBox.Width, mapPictureBox.Height, currentPose.CellX, currentPose.CellY, currentPose.AngleDeg, _path);
            if (bmp2 != null)
                mapPictureBox.Image = bmp2;
        }


        private void pointsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUIState(lidar.IsConnected);
            var selected = pointsListBox.SelectedItem;

            if (selected != null)
            {
                var typeProperty = selected.GetType().GetProperty("Type");
                var pointProperty = selected.GetType().GetProperty("Point");

                if (typeProperty != null && pointProperty != null)
                {
                    char type = (char)typeProperty.GetValue(selected);
                    Cell targetCell = (Cell)pointProperty.GetValue(selected);//объект куда нужно прийти (точка интереса)
                    Pose startPose = localizer.LocalizeFast(lidar.GetDistances());//расчет положения робота
                    Cell startCell = new Cell(startPose.CellX, startPose.CellY);//точка старта
                    _path = map.FindPath(startCell, targetCell, type);//расчет пути
                }
            }
        }

        private void moveButton_Click(object sender, EventArgs e)
        {
            if (_path.Count > 0)
            {
                    motionController.StartPath(_path);
            }
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
            if (reportListBox.InvokeRequired) {
                reportListBox.Invoke(new Action(() => {
                    reportListBox.Items.Add(message);
                    reportListBox.TopIndex = reportListBox.Items.Count - 1;
                }));
            }
            else {
                reportListBox.Items.Add(message);
                reportListBox.TopIndex = reportListBox.Items.Count - 1;
            }

            
        }
    }
}