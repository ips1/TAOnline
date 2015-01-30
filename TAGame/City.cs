using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAGame
{
    /// <summary>
    /// Enum for all the basic colors of cities in the game
    /// </summary>
    public enum CityColor
    {
        GREEN, YELLOW, BLUE, RED, ORANGE
    }

    /// <summary>
    /// Class representing a city on the game plan of Trans America
    /// </summary>
    public class City
    {
        private int id;
        private string name;
        public string Name { get { return name; } }
        private Vertex position;
        private CityColor color;

        private static int count;

        public CityColor Color { get { return color; } }
        public Vertex Position { get { return position; } }

        /// <summary>
        /// Constructor specifiing city name, vertex on which it lies and color of the city
        /// </summary>
        /// <param name="name">Name of the city</param>
        /// <param name="position">Vertex on which the city is</param>
        /// <param name="color">Color of the city</param>
        public City(string name, Vertex position, CityColor color)
        {
            id = count++;
            this.name = name;
            this.position = position;
            this.color = color;
        }
    }
}
