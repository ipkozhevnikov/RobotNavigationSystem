using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace RobotLocalization
{
    public class Map
    {
        private const int WIDTH = 65;
        private const int HEIGHT = 40;

        private char[,] grid;
        public Cell[,] cells { get; }
        private List<Cell> highwayPoints;
        private Dictionary<char, List<Cell>> interestPoints;
        private Cell startPoint;

        public bool[,] boolMap { get; }

        // Граф шоссе (узлы и соединения между ними)
        private Dictionary<Cell, List<Cell>> highwayGraph;

        // Локальные области (кластеры точек интереса)
        private Dictionary<char, List<Cell>> localAreas;

        // Кэш для быстрого поиска путей
        private Dictionary<string, List<Cell>> pathCache;

        private char _targetType;
        public char TargetType => _targetType;

        public Map()
        {
            grid = new char[HEIGHT, WIDTH];
            cells = new Cell[HEIGHT, WIDTH];
            boolMap = new bool[HEIGHT, WIDTH];
            highwayPoints = new List<Cell>();
            interestPoints = new Dictionary<char, List<Cell>>();
            highwayGraph = new Dictionary<Cell, List<Cell>>();
            localAreas = new Dictionary<char, List<Cell>>();
            pathCache = new Dictionary<string, List<Cell>>();

            for (char c = '1'; c <= '4'; c++)
            {
                interestPoints[c] = new List<Cell>();
            }
        }

        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл {filePath} не найден");

            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length < HEIGHT)
                throw new ArgumentException($"Файл должен содержать минимум {HEIGHT} строк");

            // Временные списки для хранения точек до группировки
            Dictionary<char, List<Cell>> rawInterestPoints = new Dictionary<char, List<Cell>>()
            {
                {'1', new List<Cell>()},
                {'2', new List<Cell>()},
                {'3', new List<Cell>()},
                {'4', new List<Cell>()}
            };

            for (int y = 0; y < HEIGHT; y++)
            {
                if (lines[y].Length < WIDTH)
                    throw new ArgumentException($"Строка {y + 1} должна содержать минимум {WIDTH} символов");

                for (int x = 0; x < WIDTH; x++)
                {
                    char cellChar = lines[y][x];
                    grid[y, x] = cellChar;

                    switch (cellChar)
                    {
                        case '#':
                            cells[y, x] = new Cell(x, y, true);
                            boolMap[y, x] = true;
                            break;
                        case '5':
                            highwayPoints.Add(new Cell(x, y, false));
                            break;
                        case '0':
                            startPoint = new Cell(x, y, false);
                            break;
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                            // Сначала сохраняем все точки во временный список
                            rawInterestPoints[cellChar].Add(new Cell(x, y, false));
                            cells[y, x] = new Cell(x, y, false);
                            boolMap[y, x] = false;
                            break;
                        default:
                            cells[y, x] = new Cell(x, y, false);
                            boolMap[y, x] = false;
                            break;
                    }
                }
            }

            // Группировка соседних точек интереса
            foreach (var type in rawInterestPoints.Keys)
            {
                var groupedPoints = GroupAdjacentPoints(rawInterestPoints[type]);
                interestPoints[type].AddRange(groupedPoints);
            }

            BuildHighwayGraph();
            BuildLocalAreas();
            Console.WriteLine("Карта успешно загружена.");
        }

        // Метод для группировки соседних точек
        private List<Cell> GroupAdjacentPoints(List<Cell> points)
        {
            if (points.Count == 0)
                return new List<Cell>();

            List<List<Cell>> groups = new List<List<Cell>>();
            bool[,] visited = new bool[HEIGHT, WIDTH];

            // Помечаем все точки на карте
            foreach (var point in points)
            {
                visited[point.Y, point.X] = true;
            }

            // Находим группы соседних точек
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    if (visited[y, x])
                    {
                        var group = FindConnectedGroup(x, y, visited);
                        if (group.Count > 0)
                        {
                            groups.Add(group);
                        }
                    }
                }
            }

            // Создаем результирующие точки с центрами в середине групп
            List<Cell> result = new List<Cell>();

            foreach (var group in groups)
            {
                if (group.Count == 1)
                {
                    // Если точка одна - оставляем как есть
                    result.Add(group[0]);
                }
                else
                {
                    // Находим центр группы
                    int centerX = (int)Math.Round(group.Average(p => p.X));
                    int centerY = (int)Math.Round(group.Average(p => p.Y));

                    // Создаем новую точку в центре группы
                    result.Add(new Cell(centerX, centerY, false));
                }
            }

            return result;
        }

        // Поиск связанной группы точек (по 4-соседству: вверх, вниз, влево, вправо)
        private List<Cell> FindConnectedGroup(int startX, int startY, bool[,] visited)
        {
            List<Cell> group = new List<Cell>();
            Stack<(int x, int y)> stack = new Stack<(int x, int y)>();

            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();

                if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT || !visited[y, x])
                    continue;

                visited[y, x] = false; // Помечаем как посещенное
                group.Add(new Cell(x, y, false));

                // Проверяем 4 соседей
                stack.Push((x + 1, y));
                stack.Push((x - 1, y));
                stack.Push((x, y + 1));
                stack.Push((x, y - 1));
            }

            return group;
        }

        // Построение графа шоссе
        private void BuildHighwayGraph()
        {
            // Очищаем граф
            highwayGraph.Clear();

            // Максимальное расстояние для соединения путевых точек
            int maxConnectionDistance = 20;

            foreach (var point in highwayPoints)
            {
                var neighbors = new List<Cell>();

                foreach (var otherPoint in highwayPoints)
                {
                    if (point.Equals(otherPoint))
                        continue;

                    // Проверяем расстояние
                    double distance = Math.Sqrt(Math.Pow(point.X - otherPoint.X, 2) +
                                                Math.Pow(point.Y - otherPoint.Y, 2));

                    if (distance <= maxConnectionDistance)
                    {
                        // Проверяем, есть ли препятствия на пути
                        if (!HasObstaclesBetween(point, otherPoint))
                        {
                            neighbors.Add(otherPoint);
                        }
                    }
                }

                highwayGraph[point] = neighbors;
            }

            Console.WriteLine($"Построен граф шоссе с {highwayGraph.Count} узлами.");

            // Выводим информацию о графе для отладки
            int totalConnections = highwayGraph.Values.Sum(list => list.Count);
            Console.WriteLine($"Всего соединений: {totalConnections}");

            // Выводим несколько узлов для примера
            int count = 0;
            foreach (var kvp in highwayGraph)
            {
                if (count++ < 5 && kvp.Value.Count > 0)
                {
                    Console.WriteLine($"Узел {kvp.Key} соединен с {kvp.Value.Count} узлами:");
                    foreach (var neighbor in kvp.Value.Take(3))
                    {
                        Console.WriteLine($"  -> {neighbor}");
                    }
                }
            }
        }

        // Построение локальных областей
        private void BuildLocalAreas()
        {
            foreach (var kvp in interestPoints)
            {
                var area = new List<Cell>();

                foreach (var interestPoint in kvp.Value)
                {
                    area.Add(interestPoint);

                    // Добавляем соседние клетки в радиусе
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            int newX = interestPoint.X + dx;
                            int newY = interestPoint.Y + dy;

                            if (newX >= 0 && newX < WIDTH && newY >= 0 && newY < HEIGHT)
                            {
                                var neighbor = new Cell(newX, newY);
                                if (!area.Contains(neighbor) && grid[newY, newX] != '#')
                                {
                                    area.Add(neighbor);
                                }
                            }
                        }
                    }
                }

                localAreas[kvp.Key] = area;
            }
        }

        // 2. Вывод списка путевых точек
        public List<Cell> GetHighwayPoints()
        {
            return new List<Cell>(highwayPoints);
        }

        public void PrintHighwayPoints()
        {
            Console.WriteLine("Путевые точки шоссе:");
            int count = 0;
            foreach (var point in highwayPoints)
            {
                Console.WriteLine($"  {point}");
                if (count++ > 20) // Ограничиваем вывод
                {
                    Console.WriteLine($"  ... и еще {highwayPoints.Count - count} точек");
                    break;
                }
            }
            Console.WriteLine($"Всего: {highwayPoints.Count} точек");
        }

        // 3. Вывод списка точек интереса
        public Dictionary<char, List<Cell>> GetInterestPoints()
        {
            return new Dictionary<char, List<Cell>>(interestPoints);
        }

        public void PrintInterestPoints()
        {
            Console.WriteLine("Точки интереса робота:");
            foreach (var kvp in interestPoints)
            {
                if (kvp.Value.Count > 0)
                {
                    Console.WriteLine($"  Тип {kvp.Key}: {kvp.Value.Count} точек");
                }
            }
        }

        // 4. Метод поиска пути
        public List<Cell> FindPath(Cell start, Cell target, char targetType)
        {
            _targetType = targetType;
            string cacheKey = $"{start.X},{start.Y}|{target.X},{target.Y}|{targetType}";
            if (pathCache.ContainsKey(cacheKey))
            {
                return new List<Cell>(pathCache[cacheKey]);
            }

            bool sameLocalArea = IsInSameLocalArea(start, target, targetType);

            List<Cell> path = null;

            if (sameLocalArea)
            {
                Console.WriteLine("Точки в одной локальной области.");
                path = AStarSearchStandard(start, target);
                if (path.Count > 20)
                {
                    sameLocalArea = false;
                }
            }
            if (!sameLocalArea)            
            {
                Console.WriteLine("Точки в разных областях, используем шоссе...");
                path = FindPathWithHighway(start, target);
            }

            if (path != null)
            {
                pathCache[cacheKey] = new List<Cell>(path);
            }

            return path;
        }

        private bool IsInSameLocalArea(Cell start, Cell target, char targetType)
        {
            // Проверяем, есть ли прямой путь без препятствий
            if (!HasObstaclesBetween(start, target))
            {
                double directDistance = Distance(start, target);

                // Если прямой путь короткий и без препятствий
                if (directDistance < 20)
                {
                    Console.WriteLine($"Прямой путь короткий ({directDistance:F1}) и без препятствий");
                    return true;
                }
            }

            // Находим ближайшие доступные путевые точки
            Cell startHighway = FindNearestAccessibleHighwayNodeSimple(start);
            Cell targetHighway = FindNearestAccessibleHighwayNodeSimple(target);

            if (startHighway == null || targetHighway == null)
            {
                Console.WriteLine("Не найдены доступные путевые точки");
                return true; // Пытаемся идти напрямую
            }

            // Проверяем, насколько выгодно использовать шоссе
            double toStartHighway = CalculatePathLength(start, startHighway);
            double toTargetHighway = CalculatePathLength(target, targetHighway);
            double betweenHighways = CalculatePathLength(startHighway, targetHighway);

            double directPathLength = CalculatePathLength(start, target);

            if (directPathLength <= 0) // Не смогли найти прямой путь
            {
                Console.WriteLine("Прямой путь не найден, используем шоссе");
                return false;
            }

            double highwayPathLength = toStartHighway + betweenHighways + toTargetHighway;

            Console.WriteLine($"Длины: прямой={directPathLength:F1}, шоссе={highwayPathLength:F1}");

            // Используем шоссе, если оно не намного длиннее
            return highwayPathLength > directPathLength * 1.3; // Шоссе на 30% длиннее
        }

        // Оценивает длину пути между точками (упрощенная версия)
        private double CalculatePathLength(Cell a, Cell b)
        {
            // Проверяем прямую видимость
            if (!HasObstaclesBetween(a, b))
            {
                return Distance(a, b);
            }

            // Используем эвристику Манхэттенского расстояния с коэффициентом
            double manhattan = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
            return manhattan * 1.5; // Коэффициент для учета обхода препятствий
        }

        // Быстрый поиск ближайшей доступной путевой точки
        private Cell FindNearestAccessibleHighwayNodeSimple(Cell point)
        {
            Cell nearest = null;
            double minDistance = double.MaxValue;

            // Проверяем только ближайшие путевые точки
            var nearbyHighways = highwayPoints
                .OrderBy(p => Distance(point, p))
                .Take(20) // Проверяем только 20 ближайших
                .ToList();

            foreach (var highwayPoint in nearbyHighways)
            {
                // Проверяем доступность (прямую видимость)
                if (!HasObstaclesBetween(point, highwayPoint))
                {
                    double distance = Distance(point, highwayPoint);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = highwayPoint;
                    }
                }
            }

            return nearest;
        }

        // Основной метод поиска пути через шоссе
        private List<Cell> FindPathWithHighway(Cell start, Cell target)
        {
            Console.WriteLine("Поиск пути через шоссе...");

            // 1. Находим ближайшие доступные узлы шоссе
            Cell startHighwayNode = FindNearestAccessibleHighwayNode(start);
            if (startHighwayNode == null)
            {
                Console.WriteLine("Не найден доступный узел шоссе от стартовой точки.");
                return null;
            }

            Cell targetHighwayNode = FindNearestAccessibleHighwayNode(target);
            if (targetHighwayNode == null)
            {
                Console.WriteLine("Не найден доступный узел шоссе к целевой точке.");
                return null;
            }

            Console.WriteLine($"Стартовый узел шоссе: {startHighwayNode}");
            Console.WriteLine($"Целевой узел шоссе: {targetHighwayNode}");

            // 2. Если узлы совпадают, идем напрямую
            if (startHighwayNode.Equals(targetHighwayNode))
            {
                Console.WriteLine("Узлы совпадают, ищем прямой путь...");
                return AStarSearchStandard(start, target);
            }

            // 3. Ищем путь от старта до узла шоссе
            var pathToHighway = AStarSearchStandard(start, startHighwayNode);
            if (pathToHighway == null)
            {
                Console.WriteLine("Не найден путь от старта до узла шоссе.");
                return null;
            }

            // 4. Ищем путь по графу шоссе (только узлы)
            var highwayNodesPath = FindPathBetweenHighwayNodes(startHighwayNode, targetHighwayNode);
            if (highwayNodesPath == null || highwayNodesPath.Count == 0)
            {
                Console.WriteLine("Не найден путь по графу шоссе между узлами.");
                return null;
            }

            Console.WriteLine($"Найдено {highwayNodesPath.Count} узлов шоссе на пути");

            // 5. Ищем путь от последнего узла шоссе до цели
            var pathFromHighway = AStarSearchStandard(targetHighwayNode, target);
            if (pathFromHighway == null)
            {
                Console.WriteLine("Не найден путь от узла шоссе до цели.");
                return null;
            }

            // 6. Собираем полный путь
            var fullPath = new List<Cell>();
            fullPath.AddRange(pathToHighway);

            // Добавляем пути между узлами шоссе
            for (int i = 1; i < highwayNodesPath.Count; i++)
            {
                var segmentPath = AStarSearchStandard(highwayNodesPath[i - 1], highwayNodesPath[i]);
                if (segmentPath != null && segmentPath.Count > 1)
                {
                    // Пропускаем первую точку (она уже есть)
                    fullPath.AddRange(segmentPath.Skip(1));
                }
                else
                {
                    Console.WriteLine($"Ошибка: не найден путь между узлами {highwayNodesPath[i - 1]} и {highwayNodesPath[i]}");
                    return null;
                }
            }

            // Добавляем путь от шоссе к цели
            if (pathFromHighway.Count > 1)
            {
                fullPath.AddRange(pathFromHighway.Skip(1));
            }

            Console.WriteLine($"Полный путь через шоссе найден: {fullPath.Count} шагов");
            return fullPath;
        }

        // Поиск пути между узлами шоссе (только узлы, не полный путь)
        private List<Cell> FindPathBetweenHighwayNodes(Cell startNode, Cell targetNode)
        {
            Console.WriteLine($"Поиск пути между узлами шоссе: {startNode} -> {targetNode}");

            // Если узлы напрямую соединены
            if (highwayGraph.ContainsKey(startNode) && highwayGraph[startNode].Contains(targetNode))
            {
                Console.WriteLine("Узлы соединены напрямую");
                return new List<Cell> { startNode, targetNode };
            }

            // Используем BFS для поиска пути в графе
            var queue = new Queue<Cell>();
            var visited = new HashSet<Cell>();
            var parent = new Dictionary<Cell, Cell>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.Equals(targetNode))
                {
                    // Восстанавливаем путь
                    var path = new List<Cell>();
                    var node = current;

                    while (!node.Equals(startNode))
                    {
                        path.Insert(0, node);
                        node = parent[node];
                    }
                    path.Insert(0, startNode);

                    Console.WriteLine($"Найден путь из {path.Count} узлов");
                    return path;
                }

                if (highwayGraph.ContainsKey(current))
                {
                    foreach (var neighbor in highwayGraph[current])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            parent[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            Console.WriteLine("Путь не найден в графе шоссе");
            return null;
        }

        // Найти ближайшую доступную путевую точку
        private Cell FindNearestAccessibleHighwayNode(Cell start)
        {
            Cell nearest = null;
            double minDistance = double.MaxValue;

            // Ищем ближайшую путевую точку с проверкой доступности
            foreach (var highwayPoint in highwayPoints)
            {
                // Проверяем прямую видимость (для скорости)
                if (!HasObstaclesBetween(start, highwayPoint))
                {
                    double distance = Math.Sqrt(Math.Pow(start.X - highwayPoint.X, 2) +
                                               Math.Pow(start.Y - highwayPoint.Y, 2));
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = highwayPoint;
                    }
                }
            }

            // Если не нашли по прямой видимости, ищем через A*
            if (nearest == null)
            {
                foreach (var highwayPoint in highwayPoints)
                {
                    var path = AStarSearchStandard(start, highwayPoint);
                    if (path != null)
                    {
                        double pathLength = path.Count;
                        if (pathLength < minDistance)
                        {
                            minDistance = pathLength;
                            nearest = highwayPoint;
                        }
                    }
                }
            }

            return nearest;
        }

        // Стандартный A* поиск
        private List<Cell> AStarSearchStandard(Cell start, Cell target)
        {
            // Проверяем простой случай
            if (start.Equals(target))
                return new List<Cell> { start };

            var openSet = new PriorityQueue<Node>();
            var closedSet = new HashSet<Cell>();
            var cameFrom = new Dictionary<Cell, Cell>();
            var gScore = new Dictionary<Cell, double>();
            var fScore = new Dictionary<Cell, double>();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, target);
            openSet.Enqueue(new Node(start, fScore[start]));

            int nodesExplored = 0;
            int maxNodes = 10000; // Ограничение для предотвращения бесконечного цикла

            while (openSet.Count > 0 && nodesExplored < maxNodes)
            {
                nodesExplored++;
                var current = openSet.Dequeue().Position;

                if (current.Equals(target))
                {
                    Console.WriteLine($"A* нашел путь, исследовано {nodesExplored} узлов");
                    return ReconstructPath(cameFrom, current);
                }

                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    double tentativeGScore = gScore[current] + Distance(current, neighbor);

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, target);

                        // Проверяем, есть ли уже такая точка в очереди
                        openSet.Enqueue(new Node(neighbor, fScore[neighbor]));
                    }
                }
            }

            if (nodesExplored >= maxNodes)
                Console.WriteLine($"A* достиг предела в {maxNodes} узлов");

            return null;
        }

        private List<Cell> GetNeighbors(Cell point)
        {
            var neighbors = new List<Cell>();
            Cell[] directions = new Cell[]
            {
                new Cell(0, -1),  // вверх
                new Cell(1, 0),   // вправо
                new Cell(0, 1),   // вниз
                new Cell(-1, 0)   // влево
            };

            foreach (var dir in directions)
            {
                int newX = point.X + dir.X;
                int newY = point.Y + dir.Y;

                if (newX >= 0 && newX < WIDTH && newY >= 0 && newY < HEIGHT)
                {
                    if (grid[newY, newX] != '#')
                    {
                        neighbors.Add(new Cell(newX, newY));
                    }
                }
            }

            return neighbors;
        }

        // Проверка препятствий на линии
        private bool HasObstaclesBetween(Cell start, Cell end)
        {
            // Для близких точек проверяем быстро
            if (Math.Abs(start.X - end.X) <= 1 && Math.Abs(start.Y - end.Y) <= 1)
                return false;

            var linePoints = GetLinePoints(start, end);

            foreach (var point in linePoints)
            {
                if (point.Equals(start) || point.Equals(end))
                    continue;

                if (grid[point.Y, point.X] == '#')
                    return true;
            }

            return false;
        }

        // Алгоритм Брезенхэма
        private List<Cell> GetLinePoints(Cell start, Cell end)
        {
            var points = new List<Cell>();

            int x0 = start.X;
            int y0 = start.Y;
            int x1 = end.X;
            int y1 = end.Y;

            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (steep)
            {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }

            if (x0 > x1)
            {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }

            int dx = x1 - x0;
            int dy = Math.Abs(y1 - y0);
            int error = dx / 2;
            int ystep = (y0 < y1) ? 1 : -1;
            int y = y0;

            for (int x = x0; x <= x1; x++)
            {
                Cell point = steep ? new Cell(y, x) : new Cell(x, y);

                if (point.X >= 0 && point.X < WIDTH && point.Y >= 0 && point.Y < HEIGHT)
                {
                    points.Add(point);
                }

                error -= dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }

            return points;
        }

        private void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        private double Heuristic(Cell a, Cell b)
        {
            // Манхэттенское расстояние
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private double Distance(Cell a, Cell b)
        {
            // Евклидово расстояние
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        private List<Cell> ReconstructPath(Dictionary<Cell, Cell> cameFrom, Cell current)
        {
            var path = new List<Cell> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }

        public Cell GetStartPoint()
        {
            return startPoint;
        }

        public Bitmap RenderMap(
            int pictureWidth,
            int pictureHeight,
            int robotCellX,
            int robotCellY,
            double robotAngleDeg,
            List<Cell> path)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);

            // Квадратная клетка
            float cellSize = Math.Min(
                (float)pictureWidth / cols,
                (float)pictureHeight / rows);

            Bitmap bmp = new Bitmap(pictureWidth, pictureHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Black);

                // --- Рисуем карту ---
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < cols; x++)
                    {
                        char c = grid[y, x];
                        Brush brush = Brushes.Gray;

                        switch (c)
                        {
                            case '#': brush = new SolidBrush(Color.Brown); break;
                            case '0': brush = Brushes.Yellow; break;
                            case '1': brush = Brushes.Green; break;
                            case '2': brush = new SolidBrush(Color.YellowGreen); break;
                            case '3': brush = new SolidBrush(Color.Teal); break;
                            case '5': brush = Brushes.LightBlue; break;
                        }

                        float px = x * cellSize;
                        float py = y * cellSize;

                        g.FillRectangle(brush, px, py, cellSize, cellSize);
                        g.DrawRectangle(Pens.Black, px, py, cellSize, cellSize);
                    }
                }

                // --- Рисуем путь (жёлтая рамка) ---
                if (path != null)
                {
                    using (Pen pathPen = new Pen(Color.Yellow, 2))
                    {
                        foreach (var cell in path)
                        {
                            float px = cell.X * cellSize;
                            float py = cell.Y * cellSize;

                            g.DrawRectangle(pathPen, px, py, cellSize, cellSize);
                        }
                    }
                }

                // --- Рисуем робота только если координаты валидные ---
                if (robotCellX > 0 && robotCellY > 0)
                {
                    float centerX = robotCellX * cellSize + cellSize / 2f;
                    float centerY = robotCellY * cellSize + cellSize / 2f;

                    float robotRadius = 7.5f;

                    g.FillEllipse(
                        Brushes.Gold,
                        centerX - robotRadius,
                        centerY - robotRadius,
                        robotRadius * 2,
                        robotRadius * 2);

                    // --- Стрелка направления ---
                    float arrowLength = 7.5f;
                    double rad = (robotAngleDeg - 180) * Math.PI / 180.0;

                    float arrowX = centerX + arrowLength * (float)Math.Cos(rad);
                    float arrowY = centerY + arrowLength * (float)Math.Sin(rad);

                    g.DrawLine(new Pen(Color.Green, 2), centerX, centerY, arrowX, arrowY);
                }
            }

            return bmp;
        }


    }
}
