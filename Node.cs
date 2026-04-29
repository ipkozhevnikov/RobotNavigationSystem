using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotLocalization
{
    public class Node : IComparable<Node>
    {
        public Cell Position { get; }
        public double Priority { get; }

        public Node(Cell position, double priority)
        {
            Position = position;
            Priority = priority;
        }

        public int CompareTo(Node other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
