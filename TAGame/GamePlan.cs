using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAGame
{
    /// <summary>
    /// Enum representing the direction of an edge
    /// </summary>
    public enum Direction
    {
        NORTHWEST, NORTHEAST, WEST, EAST, SOUTHWEST, SOUTHEAST
    }

    /// <summary>
    /// Class representing a vertex (one field / railroad intersection on a game plan)
    /// </summary>
    public class Vertex
    {
        public Edge NorthWest { get; set; }
        public Edge NorthEast { get; set; }
        public Edge West { get; set; }
        public Edge East { get; set; }
        public Edge SouthWest { get; set; }
        public Edge SouthEast { get; set; }
        public City CityOn { get; set; }
        public Player BaseOf { get; set; }

        private int x;
        private int y;
        public int X { get { return x; } }
        public int Y { get { return y; } }
        private List<Player> reachableBy;
        public List<Player> ReachableBy { get { return reachableBy; } }

        /// <summary>
        /// Default constructor for vertex
        /// </summary>
        /// <param name="x">X coordinate of vertex in a grid</param>
        /// <param name="y">Y coordinate of vertex in a grid</param>
        public Vertex(int x, int y)
        {
            this.x = x;
            this.y = y;
            BaseOf = null;
            reachableBy = new List<Player>();
            CityOn = null;
        }

        /// <summary>
        /// Finds out whether the vertex is reachable by selected player (connected to his railroad network)
        /// </summary>
        /// <param name="pid">ID of the player</param>
        /// <returns>True if the vertex is connected to player's railroad network</returns>
        public bool IsReachableBy(long pid)
        {
            bool result = false;
            foreach (Player p in reachableBy)
            {
                if (p.Id == pid)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Gets direction for a specific edge leading from this vertex
        /// </summary>
        /// <param name="e">Edge leading from this vertex</param>
        /// <returns>Direction of the edge</returns>
        public Direction GetEdgeDirection(Edge e)
        {
            if (NorthWest == e) return Direction.NORTHWEST;
            if (NorthEast == e) return Direction.NORTHEAST;
            if (East == e) return Direction.EAST;
            if (West == e) return Direction.WEST;
            if (SouthEast == e) return Direction.SOUTHEAST;
            if (SouthWest == e) return Direction.SOUTHWEST;

            throw new IncorrectMoveException();
        }

    }

    /// <summary>
    /// Class representing an edge (one place for a rail) in a game plan
    /// </summary>
    public class Edge
    {
        private Vertex from;
        private Vertex to;
        public Vertex From { get { return from; } }
        public Vertex To { get { return to; } }

        private bool isDouble;
        public bool IsDouble { get { return isDouble; } }
        private bool hasRail;
        public bool HasRail { get { return hasRail; } }

        /// <summary>
        /// Default constructor, specifies the position of the edge by its ends
        /// </summary>
        /// <param name="from">One end of the edge</param>
        /// <param name="to">The other end of the edge</param>
        public Edge(Vertex from, Vertex to)
        {
            this.from = from;
            this.to = to;
        }

        /// <summary>
        /// Sets the edge as a double edge
        /// </summary>
        public void SetDouble()
        {
            if (!isDouble)
            {
                isDouble = true;
            }
        }

        /// <summary>
        /// Gets the other end of the edge for a specified end vertex
        /// </summary>
        /// <param name="v">One end of the edge</param>
        /// <returns>The other end of the edge</returns>
        public Vertex GetOtherEnd(Vertex v)
        {
            if (v == from) return to;
            else return from;
        }

        /// <summary>
        /// Places rail on the edge
        /// </summary>
        public void PlaceRail()
        {
            if (hasRail) throw new IncorrectMoveException();
            else hasRail = true;
        }
    }

    /// <summary>
    /// Helper structure representing 2D point used for initialization of double edge lists
    /// </summary>
    struct Point
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// Class for initializing the game plan with default values
    /// </summary>
    class PlanInitializer
    {
        // Coordinates of sections of lines with vertices
        public static int[][] PlanLines = { 
            new int[] { 4, 13 }, 
            new int[] { 3, 14, 18, 19 },
            new int[] { 2, 14, 17, 19 },
            new int[] { 2, 18 },
            new int[] { 1, 17 },
            new int[] { 1, 16 },
            new int[] { 0, 15 },
            new int[] { 0, 15 },
            new int[] { 0, 15 },
            new int[] { 0, 14 },
            new int[] { 0, 13 },
            new int[] { 1, 12 },
            new int[] { 3, 11 } };

        // Coordinates of vertices whose north-western edge is doubled
        public static Point[] NWDoubleEdges = {
            new Point { x = 1, y = 4},
            new Point { x = 1, y = 8},
            new Point { x = 1, y = 9},
            new Point { x = 2, y = 3},
            new Point { x = 4, y = 2},
            new Point { x = 4, y = 12},
            new Point { x = 4, y = 17},
            new Point { x = 5, y = 16},
            new Point { x = 6, y = 1},
            new Point { x = 6, y = 2},
            new Point { x = 6, y = 4},
            new Point { x = 6, y = 10},
            new Point { x = 6, y = 11},
            new Point { x = 6, y = 13},
            new Point { x = 6, y = 14},
            new Point { x = 6, y = 15},
            new Point { x = 7, y = 3},
            new Point { x = 7, y = 5},
            new Point { x = 7, y = 12},
            new Point { x = 7, y = 14},
            new Point { x = 8, y = 2},
            new Point { x = 8, y = 11},
            new Point { x = 8, y = 13},
            new Point { x = 9, y = 10},
            new Point { x = 9, y = 12},
            new Point { x = 10, y = 1},
            new Point { x = 10, y = 3},
            new Point { x = 10, y = 9},
            new Point { x = 12, y = 8} };

        // Coordinates of vertices whose north-eastern edge is doubled
        public static Point[] NEDoubleEdges = { 
            new Point { x = 1, y = 5},
            new Point { x = 1, y = 7},
            new Point { x = 1, y = 8},
            new Point { x = 1, y = 9},
            new Point { x = 2, y = 5},
            new Point { x = 2, y = 6},
            new Point { x = 2, y = 9},
            new Point { x = 2, y = 12},
            new Point { x = 3, y = 2},
            new Point { x = 3, y = 6},
            new Point { x = 3, y = 9},
            new Point { x = 3, y = 12},
            new Point { x = 4, y = 5},
            new Point { x = 4, y = 9},
            new Point { x = 5, y = 1},
            new Point { x = 5, y = 2},
            new Point { x = 5, y = 4},
            new Point { x = 5, y = 9},
            new Point { x = 5, y = 11},
            new Point { x = 6, y = 4},
            new Point { x = 6, y = 5},
            new Point { x = 6, y = 9},
            new Point { x = 6, y = 10},
            new Point { x = 6, y = 11},
            new Point { x = 6, y = 13},
            new Point { x = 7, y = 0},
            new Point { x = 7, y = 11},
            new Point { x = 9, y = 1},
            new Point { x = 9, y = 2},
            new Point { x = 10, y = 2},
            new Point { x = 11, y = 8} };

        // Coordinates of vertices whose eastern edge is doubled
        public static Point[] EDoubleEdges = { 
            new Point { x = 0, y = 4},
            new Point { x = 1, y = 3},
            new Point { x = 1, y = 5},
            new Point { x = 1, y = 6},
            new Point { x = 1, y = 9},
            new Point { x = 1, y = 12},
            new Point { x = 2, y = 2},
            new Point { x = 2, y = 5},
            new Point { x = 2, y = 6},
            new Point { x = 2, y = 9},
            new Point { x = 2, y = 12},
            new Point { x = 3, y = 2},
            new Point { x = 3, y = 6},
            new Point { x = 3, y = 9},
            new Point { x = 3, y = 12},
            new Point { x = 4, y = 1},
            new Point { x = 4, y = 9},
            new Point { x = 4, y = 11},
            new Point { x = 4, y = 16},
            new Point { x = 5, y = 1},
            new Point { x = 5, y = 2},
            new Point { x = 5, y = 5},
            new Point { x = 5, y = 9},
            new Point { x = 5, y = 11},
            new Point { x = 5, y = 15},
            new Point { x = 6, y = 1},
            new Point { x = 6, y = 3},
            new Point { x = 6, y = 5},
            new Point { x = 6, y = 11},
            new Point { x = 6, y = 12},
            new Point { x = 6, y = 14},
            new Point { x = 7, y = 0},
            new Point { x = 7, y = 2},
            new Point { x = 7, y = 4},
            new Point { x = 7, y = 11},
            new Point { x = 7, y = 13},
            new Point { x = 8, y = 0},
            new Point { x = 8, y = 1},
            new Point { x = 8, y = 2},
            new Point { x = 8, y = 10},
            new Point { x = 8, y = 12},
            new Point { x = 9, y = 0},
            new Point { x = 9, y = 1},
            new Point { x = 9, y = 2},
            new Point { x = 9, y = 9},
            new Point { x = 9, y = 11},
            new Point { x = 10, y = 0},
            new Point { x = 10, y = 8},
            new Point { x = 11, y = 8} };

    }

    /// <summary>
    /// Class representing a game plan for a game
    /// </summary>
    public class GamePlan
    {

        // Two dimensional field of vertices
        private Vertex[][] plan;

        /// <summary>
        /// Default constructor for new game plan
        /// </summary>
        public GamePlan()
        {
            // Initialize the matrix holding vertices of game plan
            plan = new Vertex[13][];
            for (int i = 0; i < plan.Length; i++)
            {
                plan[i] = new Vertex[20];
            }

            InitVertices();
            InitEdges();
        }

        /// <summary>
        /// Returns vertex of the plan specified by coordinates
        /// </summary>
        /// <param name="x">X coordinate of the vertex</param>
        /// <param name="y">Y coordinate of the vertex</param>
        /// <returns>Vertex on specified position</returns>
        public Vertex GetVertex(int x, int y)
        {
            return plan[y][x];
        }

        /// <summary>
        /// Gets all vertices (not in fixed order)
        /// </summary>
        /// <returns>IEnumerable containing all vertices of the map</returns>
        public IEnumerable<Vertex> GetAllVertices()
        {
            foreach (Vertex[] line in plan)
            {
                foreach (Vertex v in line)
                {
                    if (v != null) yield return v;
                }
            }
        }


        /// <summary>
        /// Gets edge of the plan specified by coordinates ant its direction
        /// </summary>
        /// <param name="x">X coordinate of the edge</param>
        /// <param name="y">Y coordinate of the edge</param>
        /// <param name="d">Direction of the edge</param>
        /// <returns>Edge specified by the arguments</returns>
        public Edge GetEdge(int x, int y, Direction d)
        {

            Vertex v = plan[y][x];

            Edge e = null;

            switch (d)
            {
                case Direction.EAST: e = v.East; break;
                case Direction.WEST: e = v.West; break;
                case Direction.NORTHEAST: e = v.NorthEast; break;
                case Direction.NORTHWEST: e = v.NorthWest; break;
                case Direction.SOUTHEAST: e = v.SouthEast; break;
                case Direction.SOUTHWEST: e = v.SouthWest; break;
            }

            return e;
        }

        /// <summary>
        /// Initializes all the vertices of the plan (according to specifications in PlanInitializer class)
        /// </summary>
        /// <remarks>Should be called only once from the constructor</remarks>
        private void InitVertices()
        {
            for (int i = 0; i < PlanInitializer.PlanLines.Length; i++)
            {
                int[] line = PlanInitializer.PlanLines[i];
                int j = 0;
                // Going through tuples of begin / end coordinates for each line of game plan and initializing the vertices
                while (j < line.Length - 1)
                {
                    int beg = line[j++];
                    int end = line[j++];
                    // Initialize vertices between begind and end coordinates for the line
                    for (int k = beg; k <= end; k++)
                    {
                        plan[i][k] = new Vertex(k, i);
                    }
                }
            }
        }


        /// <summary>
        /// Initializes edges of the game plan (according to specifications in PlanInitializer class)
        /// </summary>
        /// <remarks>Should be called only once from the constructor</remarks>
        private void InitEdges()
        {
            // For each vertex, we try to initialize edges to northern vertices and eastern vetex
            foreach (Vertex[] line in plan)
            {
                foreach (Vertex field in line)
                {
                    if (field == null) continue;
                    int x = field.X;
                    int y = field.Y;
                    // Look in all three directions and find out whether there is vertex to be connected to
                    if (y > 0)
                    {
                        Vertex newfield = plan[y-1][x];
                        if (newfield != null)
                        {
                            Edge newedge = new Edge(field, newfield);
                            field.NorthWest = newedge;
                            newfield.SouthEast = newedge;
                        }
                    }
                    if (x < plan[y].Length - 1)
                    {
                        Vertex newfield = plan[y][x+1];
                        if (newfield != null)
                        {
                            Edge newedge = new Edge(field, newfield);
                            field.East = newedge;
                            newfield.West = newedge;
                        }
                    }
                    if ((y > 0) && (x < plan[y].Length - 1))
                    {
                        Vertex newfield = plan[y - 1][x + 1];
                        if (newfield != null)
                        {
                            Edge newedge = new Edge(field, newfield);
                            field.NorthEast = newedge;
                            newfield.SouthWest = newedge;
                        }
                    }

                }
            }

            // Initialization of double-edges
            foreach (Point coords in PlanInitializer.NWDoubleEdges)
            {
                Vertex v = plan[coords.x][coords.y];
                Edge e = v.NorthWest;
                if (e != null)
                {
                    e.SetDouble();
                }
            }
            foreach (Point coords in PlanInitializer.NEDoubleEdges)
            {
                Vertex v = plan[coords.x][coords.y];
                Edge e = v.NorthEast;
                if (e != null)
                {
                    e.SetDouble();
                }
            }
            foreach (Point coords in PlanInitializer.EDoubleEdges)
            {
                Vertex v = plan[coords.x][coords.y];
                Edge e = v.East;
                if (e != null)
                {
                    e.SetDouble();
                }
            }

        }

        /// <summary>
        /// Updates reachability by all the players of neighbours of a specified vertex (transitively)
        /// </summary>
        /// <param name="start">Vertex to start updating from</param>
        public void RefreshReachability(Vertex start)
        {
            Queue<Vertex> queue = new Queue<Vertex>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                // Check all the neighbours whether they have corresponding reachability
                Vertex v = queue.Dequeue();
                foreach (Player p in v.ReachableBy)
                {
                    if ((v.NorthEast != null) && (v.NorthEast.HasRail))
                    {
                        Vertex otherEnd = v.NorthEast.GetOtherEnd(v);
                        if (!otherEnd.ReachableBy.Contains(p))
                        {
                            otherEnd.ReachableBy.Add(p);
                            queue.Enqueue(otherEnd);
                        }
                    }
                    if ((v.NorthWest != null) && (v.NorthWest.HasRail))
                    {
                        Vertex otherEnd = v.NorthWest.GetOtherEnd(v);
                        if (!otherEnd.ReachableBy.Contains(p))
                        {
                            otherEnd.ReachableBy.Add(p);
                            queue.Enqueue(otherEnd);
                        }
                    }
                    if ((v.East != null) && (v.East.HasRail))
                    {
                        Vertex otherEnd = v.East.GetOtherEnd(v);
                        if (!otherEnd.ReachableBy.Contains(p))
                        {
                            otherEnd.ReachableBy.Add(p);
                            queue.Enqueue(otherEnd);
                        }
                    }
                    if ((v.West != null) && (v.West.HasRail))
                    {
                        Vertex otherEnd = v.West.GetOtherEnd(v);
                        if (!otherEnd.ReachableBy.Contains(p))
                        {
                            otherEnd.ReachableBy.Add(p);
                            queue.Enqueue(otherEnd);
                        }
                    }
                    if ((v.SouthEast != null) && (v.SouthEast.HasRail))
                    {
                        Vertex otherEnd = v.SouthEast.GetOtherEnd(v);
                        if (!otherEnd.ReachableBy.Contains(p))
                        {
                            otherEnd.ReachableBy.Add(p);
                            queue.Enqueue(otherEnd);
                        }
                    }
                    if ((v.SouthWest != null) && (v.SouthWest.HasRail))
                    {
                        Vertex otherEnd = v.SouthWest.GetOtherEnd(v);
                        if (!otherEnd.ReachableBy.Contains(p))
                        {
                            otherEnd.ReachableBy.Add(p);
                            queue.Enqueue(otherEnd);
                        }
                    }
                }
            }
        }

    }
}
