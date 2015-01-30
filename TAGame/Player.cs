using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAGame
{
    /// <summary>
    /// Class representing one player in the game
    /// </summary>
    public class Player
    {
        private long id;
        public string Nickname { get; private set; }
        private Vertex startingPosition;
        private List<City> cities;
        public int Points { get; private set; }

        public long Id
        {
            get { return id; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="id"></param>
        public Player(string nickname, long id)
        {
            this.id = id;
            this.Nickname = nickname;
            cities = new List<City>();
            Points = 0;
        }

        public void AssignStart(Vertex v)
        {
            startingPosition = v;
        }

        public void AssignCity(City c)
        {
            bool correct = true;
            foreach (City x in cities)
            {
                if (x.Color == c.Color)
                {
                    correct = false;
                    break;
                }
            }
            if (!correct) throw new IncorrectMoveException();
            cities.Add(c);
        }

        public List<City> GetCities()
        {
            return cities;

        }

        public bool HasCities()
        {
            if (cities.Count == 5) return true; 
            return false;
        }

        public void Victory()
        {
            Points++;
        }


    }
}
