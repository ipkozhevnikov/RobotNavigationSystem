using System;
using System.Collections.Generic;
using System.Timers;
using static RobotLocalization.RobotLocalizer;

namespace RobotLocalization
{
    public class MotionController
    {
        private enum MotionState
        {
            Idle,
            RefineStartPose,
            RotateToTarget,
            DriveStraight,
            RefineEndPose,
            CorrectPosition,
            NextCell
        }

        private MotionState _state = MotionState.Idle;

        private readonly RobotController _robot;
        private readonly LidarDataManager _lidar;
        private readonly RobotLocalizer _localizer;
        private readonly Map _map;

        private List<Cell> _path;
        private int _targetIndex;

        public bool IsRunning => _state != MotionState.Idle;

        public System.Timers.Timer timer;

        private Pose _robotPosition;
        public Pose RobotPosition => _robotPosition;

        public event Action<string> LogMessage;

        public MotionController(RobotController robot, LidarDataManager lidar, RobotLocalizer robotLocalizer, Map map)
        {
            _robot = robot;
            _lidar = lidar;
            _localizer = robotLocalizer;
            _map = map;

            _robot.RobotStateReceived += OnRobotStateReceived;

            timer = new System.Timers.Timer();
            timer.AutoReset = false;
            timer.Elapsed += OnTimerElapsed;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _robot.SendCommand(0, 0);
        }

        private void SetState(MotionState state)
        {
            _state = state;
            Log($"Статус: {state}");
        }

        private void SetTargetIndex(int targetIndex)
        {
            _targetIndex = targetIndex;
            Log($"TargetIndex: {targetIndex}: ({_path[targetIndex].X}, {_path[targetIndex].Y})");
        }
        
        // ---------------------------------------------------------
        // Запуск движения
        // ---------------------------------------------------------
        public void StartPath(List<Cell> path)
        {
            if (path == null || path.Count < 2)
                return;

            _path = path;
            SetTargetIndex(1);

            SetState(MotionState.RefineStartPose);
            _robot.SendCommand(0, 0);
        }

        public void Stop()
        {
            SetState(MotionState.Idle);
            _robot.SendCommand(0, 0);
        }

        // ---------------------------------------------------------
        // Главный обработчик события
        // ---------------------------------------------------------
        private void OnRobotStateReceived(RobotController.RobotState s)
        {
            if (_state == MotionState.Idle || timer.Enabled)
                return;

            // удар — стоп
            if (s.b != 0)
            {
                Stop();
                return;
            }

            switch (_state)
            {
                case MotionState.RefineStartPose:
                    HandleRefineStartPose(s);
                    break;

                case MotionState.RotateToTarget:
                    HandleRotateToTarget(s);
                    break;

                case MotionState.DriveStraight:
                    HandleDriveStraight(s);
                    break;

                case MotionState.RefineEndPose:
                    HandleRefineEndPose(s);
                    break;

                case MotionState.CorrectPosition:
                    HandleCorrectPosition(s);
                    break;

                case MotionState.NextCell:
                    HandleNextCell(s);
                    break;
            }
        }

        // ---------------------------------------------------------
        // 1. Уточнение начальной позы
        // ---------------------------------------------------------
        private void HandleRefineStartPose(RobotController.RobotState s)
        {
            var pose = RefineSubcell();

            SetState(MotionState.RotateToTarget);
            HandleRotateToTarget(s);
        }

        // ---------------------------------------------------------
        // 2. Поворот на нужный угол
        // ---------------------------------------------------------
        private void HandleRotateToTarget(RobotController.RobotState s)
        {
            Cell from = _path[_targetIndex - 1];
            Cell to = _path[_targetIndex];

            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            int desiredAngle = 0;
            if (dx == 1) desiredAngle = 180;
            if (dx == -1) desiredAngle = 0;
            if (dy == -1) desiredAngle = 90;
            if (dy == 1) desiredAngle = 270;

            double delta = desiredAngle - _robotPosition.AngleDeg;
            if (Math.Abs(delta) > 180)
            {
                if (delta < 0)
                {
                    delta = delta % 360 + 360;
                } else
                {
                    delta = delta % 360 - 360;
                }
            }

            Log($"_angleDeg={_robotPosition.AngleDeg}, desiredAngle ={desiredAngle}. Нужен поворот на {delta} град");

            if (Math.Abs(delta) < 3)
            {
                SetState(MotionState.DriveStraight);
                HandleDriveStraight(s);
            }
            else
            {
                _robot.SendCommand(0, 50 * (delta < 0 ? 1 : -1));
                timer.Interval = 17.4533 * 2 * Math.Abs(delta);
                timer.Start();
                SetState(MotionState.DriveStraight);
            }
        }

        // ---------------------------------------------------------
        // 3. Прямолинейное движение на 25 см
        // ---------------------------------------------------------
        private void HandleDriveStraight(RobotController.RobotState s)
        {
            _robot.SendCommand(50, 0);
            timer.Interval = 1250 * 2;
            timer.Start();

            SetState(MotionState.RefineEndPose);
        }

        // ---------------------------------------------------------
        // 4. Уточнение позы после движения
        // ---------------------------------------------------------
        private void HandleRefineEndPose(RobotController.RobotState s)
        {
            var pose = RefineSubcell();
            SetState(MotionState.NextCell);
            HandleNextCell(s);
        }

        // ---------------------------------------------------------
        // 5. Коррекция положения
        // ---------------------------------------------------------
        private void HandleCorrectPosition(RobotController.RobotState s)
        {
            SetState(MotionState.NextCell);
            HandleNextCell(s);
            return;
        }

        // ---------------------------------------------------------
        // 6. Переход к следующей клетке
        // ---------------------------------------------------------
        private void HandleNextCell(RobotController.RobotState s)
        {
            if (_targetIndex == _path.Count - 1)
            {
                SetState(MotionState.Idle);
            }
            else
            {
                SetTargetIndex(_targetIndex + 1);

                if (_targetIndex >= _path.Count)
                {
                    Stop();
                    return;
                }

                SetState(MotionState.RotateToTarget);
                HandleRotateToTarget(s);
            }
        }

        // ---------------------------------------------------------
        // Stub: твоя функция уточнения позы
        // ---------------------------------------------------------
        private Pose RefineSubcell()
        {
            var lidarData = _lidar.GetDistances();
            RobotLocalizer.Pose pose = _localizer.LocalizeFast(lidarData);
            Pose refinedPose = _localizer.RefinePose(lidarData, pose);
            _robotPosition = refinedPose;
            Log($"УТОЧНЕННАЯ ПОЗА: {refinedPose.CellX}, {refinedPose.CellY}, {refinedPose.AngleDeg}");
            return refinedPose;
        }

        private void Log(string message)
        {
            LogMessage?.Invoke(message);
        }
    }
}
