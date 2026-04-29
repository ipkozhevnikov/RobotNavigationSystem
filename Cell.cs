using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotLocalization
{

    public enum CellType
    {
        Obstacle = '#',
        Empty = '.',
        Highway = '5',
        Start = '0',
        Interest1 = '1',
        Interest2 = '2',
        Interest3 = '3',
        Interest4 = '4'
    }

    // Класс для представления клетки карты
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsObstacle { get; set; }

        public Cell(int x, int y, bool isObstacle)
        {
            X = x;
            Y = y;
            IsObstacle = isObstacle;
        }

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            return obj is Cell point &&
                   X == point.X &&
                   Y == point.Y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
