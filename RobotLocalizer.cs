using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotLocalization
{

    public class RobotLocalizer
    {
        // Размеры карты в клетках
        public const int MapWidth = 65;
        public const int MapHeight = 40;

        // Размер клетки в единицах лидара
        public const int CellSize = 250;

        // Максимальная дальность лидара
        public const int LidarMaxDistance = 10000;

        // Шаг по углу (в градусах) при переборе ориентаций
        public const int AngleStep = 15; // можно сделать 5 для точнее

        // Карта: true = стена, false = свободно
        private readonly bool[,] _map;

        public RobotLocalizer(bool[,] map)
        {
            if (map.GetLength(1) != MapWidth || map.GetLength(0) != MapHeight)
                throw new ArgumentException("Неверный размер карты");

            _map = map;
        }

        public struct Pose
        {
            public int CellX;
            public int CellY;
            public int AngleDeg;
            public double Error;
        }

        public struct PoseFine
        {
            public int CellX;
            public int CellY;
            public int AngleDeg;
            public double WorldX; // в единицах лидара
            public double WorldY; // в единицах лидара
            public double Error;
        }

        public bool[,] GetMap()
        {
            return _map;
        }

        /// <summary>
        /// Основной метод: по данным лидара находит лучшую клетку и угол.
        /// lidarData[0..359] - расстояния в единицах лидара (0..10000, 0 = нет препятствия).
        /// </summary>
        public Pose Localize(int[] lidarData)
        {
            if (lidarData == null || lidarData.Length != 360)
                throw new ArgumentException("Ожидается массив из 360 значений лидара");

            Pose bestPose = new Pose
            {
                CellX = -1,
                CellY = -1,
                AngleDeg = 0,
                Error = double.MaxValue
            };

            // Перебираем все клетки карты
            for (int cy = 0; cy < MapHeight; cy++)
            {
                for (int cx = 0; cx < MapWidth; cx++)
                {
                    // Пропускаем клетки со стеной
                    if (_map[cy, cx])
                        continue;

                    // Центр клетки в координатах лидара
                    double robotX = cx * CellSize + CellSize / 2.0;
                    double robotY = cy * CellSize + CellSize / 2.0;

                    // Перебираем возможные углы
                    for (int angle = 0; angle < 360; angle += AngleStep)
                    {
                        double error = ComputeScanError(robotX, robotY, angle, lidarData);

                        if (error < bestPose.Error)
                        {
                            bestPose = new Pose
                            {
                                CellX = cx,
                                CellY = cy,
                                AngleDeg = angle,
                                Error = error
                            };
                        }
                    }
                }
            }

            return bestPose;
        }

        public Pose LocalizeFast(int[] lidar)
        {
            int[] beamIndices = new int[36];
            for (int i = 0; i < 36; i++)
                beamIndices[i] = i * 10; // каждые 10°

            Pose best = new Pose { Error = double.MaxValue };

            object lockObj = new object();

            Parallel.For(0, MapHeight, cy =>
            {
                for (int cx = 0; cx < MapWidth; cx++)
                {
                    if (_map[cy, cx]) continue;

                    double x = cx * CellSize + CellSize / 2.0;
                    double y = cy * CellSize + CellSize / 2.0;

                    for (int angle = 0; angle < 360; angle += AngleStep)
                    {
                        double err = 0;

                        foreach (int bi in beamIndices)
                        {
                            int real = lidar[bi];
                            double sim = CastRayDDA(x, y, angle + bi);

                            if (real == 0 && sim >= LidarMaxDistance) continue;
                            if (real == 0 && sim < LidarMaxDistance) { err += 5000; continue; }
                            if (real > 0 && sim >= LidarMaxDistance) { err += 5000; continue; }

                            err += Math.Abs(real - sim);
                        }

                        if (err < best.Error)
                        {
                            lock (lockObj)
                            {
                                if (err < best.Error)
                                {
                                    best = new Pose
                                    {
                                        CellX = cx,
                                        CellY = cy,
                                        AngleDeg = angle,
                                        Error = err
                                    };
                                }
                            }
                        }
                    }
                }
            });

            return best;
        }


        /// <summary>
        /// Считает ошибку между реальным сканом и симулированным для данной позы.
        /// </summary>
        private double ComputeScanError(double robotX, double robotY, int robotAngleDeg, int[] realScan)
        {
            double error = 0.0;

            for (int i = 0; i < 360; i++)
            {
                int beamAngleDeg = (robotAngleDeg + i) % 360;
                double simulatedDist = CastRay(robotX, robotY, beamAngleDeg);

                int realDist = realScan[i];

                // Обработка "0" как "нет препятствия в пределах дальности"
                // Простейший вариант: если один луч видит стену, а другой нет — штрафуем.
                if (realDist == 0 && simulatedDist >= LidarMaxDistance)
                {
                    // оба не видят препятствий — ок, ошибка 0
                    continue;
                }
                else if (realDist == 0 && simulatedDist < LidarMaxDistance)
                {
                    // реальный не видит, симуляция видит — штраф
                    error += LidarMaxDistance;
                }
                else if (realDist > 0 && simulatedDist >= LidarMaxDistance)
                {
                    // реальный видит, симуляция не видит — штраф
                    error += LidarMaxDistance;
                }
                else
                {
                    // Оба видят препятствие — сравниваем расстояния
                    error += Math.Abs(realDist - simulatedDist);
                }
            }

            return error;
        }

        /// <summary>
        /// Ray casting: от (robotX, robotY) в направлении angleDeg ищем первую стену.
        /// Возвращает расстояние до стены или LidarMaxDistance, если стены нет.
        /// </summary>
        private double CastRay(double robotX, double robotY, int angleDeg)
        {
            double angleRad = angleDeg * Math.PI / 180.0;
            double dx = Math.Cos(angleRad);
            double dy = Math.Sin(angleRad);

            // Шаг по лучу в единицах лидара
            double step = 20.0; // можно уменьшить для точности, увеличить для скорости

            double dist = 0.0;

            while (dist < LidarMaxDistance)
            {
                double x = robotX + dx * dist;
                double y = robotY + dy * dist;

                // Проверка выхода за границы карты
                if (x < 0 || y < 0 || x >= MapWidth * CellSize || y >= MapHeight * CellSize)
                {
                    return LidarMaxDistance;
                }

                int cellX = (int)(x / CellSize);
                int cellY = (int)(y / CellSize);

                if (_map[cellY, cellX])
                {
                    // Стена найдена
                    return dist;
                }

                dist += step;
            }

            return LidarMaxDistance;
        }

        private double CastRayDDA(double startX, double startY, double angleDeg)
        {
            double angle = angleDeg * Math.PI / 180.0;
            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle);

            // Текущая позиция
            double x = startX;
            double y = startY;

            // В какую клетку попадает старт
            int mapX = (int)(x / CellSize);
            int mapY = (int)(y / CellSize);

            // Длина луча до границы клетки
            double deltaDistX = Math.Abs(CellSize / dx);
            double deltaDistY = Math.Abs(CellSize / dy);

            int stepX = dx > 0 ? 1 : -1;
            int stepY = dy > 0 ? 1 : -1;

            double sideDistX;
            double sideDistY;

            double cellX = mapX * CellSize + (dx > 0 ? CellSize : 0);
            double cellY = mapY * CellSize + (dy > 0 ? CellSize : 0);

            sideDistX = Math.Abs((cellX - x) / dx);
            sideDistY = Math.Abs((cellY - y) / dy);

            double dist = 0;

            while (dist < LidarMaxDistance)
            {
                if (sideDistX < sideDistY)
                {
                    mapX += stepX;
                    dist = sideDistX;
                    sideDistX += deltaDistX;
                }
                else
                {
                    mapY += stepY;
                    dist = sideDistY;
                    sideDistY += deltaDistY;
                }

                // Выход за карту
                if (mapX < 0 || mapY < 0 || mapX >= MapWidth || mapY >= MapHeight)
                    return LidarMaxDistance;

                // Стена
                if (_map[mapY, mapX])
                    return dist;
            }

            return LidarMaxDistance;
        }

        //уточненная поза
        public Pose RefinePose(int[] lidar, Pose coarse)
        {
            int[] beamIndices = new int[36];
            for (int i = 0; i < 36; i++)
                beamIndices[i] = i * 10;

            Pose best = coarse;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = coarse.CellX + dx;
                    int cy = coarse.CellY + dy;

                    if (cx < 0 || cy < 0 || cx >= MapWidth || cy >= MapHeight)
                        continue;
                    if (_map[cy, cx])
                        continue;

                    double x = cx * CellSize + CellSize / 2.0;
                    double y = cy * CellSize + CellSize / 2.0;

                    // Уточняем угол с шагом 1°
                    for (int angle = coarse.AngleDeg - 15; angle <= coarse.AngleDeg + 15; angle++)
                    {
                        int a = (angle + 360) % 360;

                        double err = 0;

                        foreach (int bi in beamIndices)
                        {
                            int real = lidar[bi];
                            double sim = CastRayDDA(x, y, a + bi);

                            if (real == 0 && sim >= LidarMaxDistance) continue;
                            if (real == 0 && sim < LidarMaxDistance) { err += 5000; continue; }
                            if (real > 0 && sim >= LidarMaxDistance) { err += 5000; continue; }

                            err += Math.Abs(real - sim);
                        }

                        if (err < best.Error)
                        {
                            best = new Pose
                            {
                                CellX = cx,
                                CellY = cy,
                                AngleDeg = a,
                                Error = err
                            };
                        }
                    }
                }
            }

            return best;
        }

        //Метод субклеточной локализации
        public PoseFine RefineSubcell(int[] lidar, Pose refined)
        {
            int[] beamIndices = new int[36];
            for (int i = 0; i < 36; i++)
                beamIndices[i] = i * 10;

            double baseX = refined.CellX * CellSize + CellSize / 2.0;
            double baseY = refined.CellY * CellSize + CellSize / 2.0;

            int pointsOfInterest = 21;
            // шаг смещения внутри клетки (например, треть клетки)
            double offsetStep = CellSize / pointsOfInterest;
            int halfDistance = pointsOfInterest / 2;

            PoseFine best = new PoseFine
            {
                CellX = refined.CellX,
                CellY = refined.CellY,
                AngleDeg = refined.AngleDeg,
                WorldX = baseX,
                WorldY = baseY,
                Error = refined.Error
            };

            for (int oy = -halfDistance; oy <= halfDistance; oy++)
            {
                for (int ox = -halfDistance; ox <= halfDistance; ox++)
                {
                    double x = baseX + ox * offsetStep;
                    double y = baseY + oy * offsetStep;

                    // проверка выхода за карту
                    if (x < 0 || y < 0 || x >= MapWidth * CellSize || y >= MapHeight * CellSize)
                        continue;

                    int cellX = (int)(x / CellSize);
                    int cellY = (int)(y / CellSize);

                    // если ушли в стену — пропускаем
                    if (_map[cellY, cellX])
                        continue;

                    double err = 0;

                    foreach (int bi in beamIndices)
                    {
                        int real = lidar[bi];
                        double sim = CastRayDDA(x, y, refined.AngleDeg + bi);

                        if (real == 0 && sim >= LidarMaxDistance) continue;
                        if (real == 0 && sim < LidarMaxDistance) { err += 5000; continue; }
                        if (real > 0 && sim >= LidarMaxDistance) { err += 5000; continue; }

                        err += Math.Abs(real - sim);
                    }

                    if (err < best.Error)
                    {
                        best = new PoseFine
                        {
                            CellX = cellX,
                            CellY = cellY,
                            AngleDeg = refined.AngleDeg,
                            WorldX = x,
                            WorldY = y,
                            Error = err
                        };
                    }
                }
            }

            return best;
        }



    }

}
