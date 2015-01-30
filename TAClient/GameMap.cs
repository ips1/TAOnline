using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TAGame;


namespace TAClient
{
    struct LineCoordinates
    {
        public double startX, startY, endX, endY;
    }

    /// <summary>
    /// Class representing visualisation of the game map
    /// </summary>
    /// <remarks>Is responsible for showing the map in specified Canvas UIComponent, updating the map and GUI actions asociated with the map as well</remarks>
    class GameMap
    {
        // Width of one field on the map (before transformation, in the grid layout)
        static readonly int fieldWidth = 60;
        // Height of one field on the map (before transformation, in the grid layout)
        static readonly int fieldHeight = 52;
        // Size of dot representing non-city vertex on the map
        static readonly int pointSize = 0;
        // Size of dot for base selection
        static readonly int baseSelectionPointSize = 15;
        // Size of dot representing city vertex on the map
        static readonly int citySize = 30;
        // Size of dot representing starting vertex on the map
        static readonly int baseSize = 15;
        // Shift of the whole graph from the upper border of the picture
        static readonly int topShift = 16;
        // Shift of the whole graph from the left border of the picture
        static readonly int leftShift = -118;
        // Scale in which the height of the graph is reduced after transformation
        static readonly double heightScale = 0.85;
        // Thickness of normal edges
        static readonly int normalThickness = 1;
        // Thickness of double edges
        static readonly int doubleThickness = 3;
        // Thickness of rails
        static readonly int railThickness = 4;
        // Thickness of selection highlight
        static readonly int highlightThickness = 5;

        private Canvas gameCanvas;

        private Game game;

        private Synchronizer synchronizer;

        private bool loaded;

        private bool active;

        private List<UIElement> highlights;

        /// <summary>
        /// Creates new GameMap with specified Canvas to draw on and Game as a source of game data
        /// </summary>
        /// <param name="gameCanvas">Canvas to draw on</param>
        /// <param name="game">Game to be showed</param>
        public GameMap(Canvas gameCanvas, Game game, Synchronizer synchronizer)
        {
            this.gameCanvas = gameCanvas;
            this.game = game;
            this.synchronizer = synchronizer;
            this.loaded = false;
            this.active = false;
            this.highlights = new List<UIElement>();
        }

        /// <summary>
        /// Removes all elements of the map from the canvas, hiding the map in process
        /// </summary>
        public void UnloadMap()
        {
            loaded = false;
            gameCanvas.Children.Clear();
            // We have to remove highlights as well
            highlights.Clear();
        }

        /// <summary>
        /// Reloads the map showing all the changes that occured
        /// </summary>
        public void ReloadMap()
        {
            UnloadMap();
            LoadMap();
        }

        /// <summary>
        /// Makes player unable to place rails on the map
        /// </summary>
        public void DeactivateMap()
        {
            active = false;
        }

        /// <summary>
        /// Enables player to place rails on the map
        /// </summary>
        /// <remarks>Map must be loaded first</remarks>
        public void ActivateMap()
        {
            if (!loaded) throw new IncorrectMoveException();
            active = true;
        }

        /// <summary>
        /// Loads all elements of the map on the canvas
        /// </summary>
        /// <remarks>Map must be unloaded before being loaded again!</remarks>
        public void LoadMap()
        {
            if (loaded) return;

            List<Vertex> vertices = game.Plan.GetAllVertices().ToList();

            // Drawing all edges
            vertices.ForEach(DrawEdgesForVertex);

            // Drawing all the vertices
            vertices.ForEach(DrawVertex);

            loaded = true;
        }

        /// <summary>
        /// Updates all edges on the map according to the game state
        /// </summary>
        public void RefreshMap()
        {

            foreach (UIElement uie in gameCanvas.Children)
            {
                if (!(uie is Line)) continue;
                Line l = (Line)uie;
                Edge e = (Edge)(l.Tag);
                int thickness = normalThickness;
                if (e.IsDouble) thickness = doubleThickness;
                Brush b = Brushes.Black;
                if (e.HasRail)
                {
                    b = Brushes.Red;
                    thickness = railThickness;
                }
                l.Stroke = b;
                l.StrokeThickness = thickness;
            }
        }

        /// <summary>
        /// Adds mark for the player base to the map
        /// </summary>
        /// <param name="v">Vertex to add the mark to</param>
        public void AddBase(Vertex v)
        {
            // Vertex is a starting base of a player - we add a mark
            Player p = v.BaseOf;
            if (p != null)
            {
                Brush b = synchronizer.GetClient(p.Id).Color;
                Ellipse baseEllipse = new Ellipse() { Fill = b, Width = baseSize, Height = baseSize, Stroke = Brushes.Black, StrokeThickness = 1 };

                int diff = baseSize / 2;
                Canvas.SetTop(baseEllipse, OrigY(v.X * fieldWidth, v.Y * fieldHeight) * heightScale + topShift - diff);
                Canvas.SetLeft(baseEllipse, OrigX(v.X * fieldWidth, v.Y * fieldHeight) + leftShift - diff);
                gameCanvas.Children.Add(baseEllipse);

                baseEllipse.Tag = v;
                //baseEllipse.MouseEnter += MouseOverVertex;
                //baseEllipse.MouseLeave += MouseOutVertex;

            }

        }

        private void DrawVertex(Vertex v)
        {
            City c = v.CityOn;
            Brush br = Brushes.Black;
            int size = game.IsPositionSelection() ? baseSelectionPointSize : pointSize;

            // If there is city on the vertex, we select a color according to the city
            if (c != null)
            {
                size = citySize;
                switch (c.Color)
                {
                    case CityColor.BLUE: br = Brushes.LightBlue; break;
                    case CityColor.GREEN: br = Brushes.LightGreen; break;
                    case CityColor.ORANGE: br = Brushes.Orange; break;
                    case CityColor.RED: br = Brushes.Salmon; break;
                    case CityColor.YELLOW: br = Brushes.LightYellow; break;
                }
            }

            Ellipse vertexEllipse = new Ellipse() { Fill = br, Width = size, Height = size, Stroke = Brushes.Black, StrokeThickness = 1 };
            vertexEllipse.Tag = v;
            // Associating event handlers for actions with vertices
            vertexEllipse.MouseEnter += MouseOverVertex;
            vertexEllipse.MouseLeave += MouseOutVertex;
            vertexEllipse.MouseUp += MouseClickVertex;
            int difference = (size - pointSize) / 2;
            Canvas.SetTop(vertexEllipse, TopFromVertex(v) * heightScale + topShift - difference);
            Canvas.SetLeft(vertexEllipse, LeftFromVertex(v) + leftShift - difference);
            gameCanvas.Children.Add(vertexEllipse);

            // Vertex is a starting base of a player - we add a mark
            AddBase(v);
        }

        private void DrawEdgesForVertex(Vertex v)
        {
            DrawEdge(v.East);
            DrawEdge(v.NorthWest);
            DrawEdge(v.NorthEast);
        }

        private void DrawEdge(Edge e)
        {
            if (e == null) return;

            int thickness = e.IsDouble ? doubleThickness : normalThickness;
            Brush b = Brushes.Black;
            if (e.HasRail)
            {
                b = Brushes.Red;
                thickness = railThickness;
            }

            LineCoordinates coords = CoordsFromEdge(e);

            double x1 = pointSize / 2 + coords.startX + leftShift;
            double x2 = pointSize / 2 + coords.endX + leftShift;
            double y1 = topShift + pointSize / 2 + coords.startY * heightScale;
            double y2 = topShift + pointSize / 2 + coords.endY * heightScale;
            Line edgeLine = new Line() { Stroke = b, StrokeThickness = thickness, X1 = x1, X2 = x2, Y1 = y1, Y2 = y2 };
            edgeLine.Tag = e;
            edgeLine.MouseEnter += MouseOverEdge;
            edgeLine.MouseLeave += MouseOutEdge;
            edgeLine.MouseUp += MouseClickEdge;
            gameCanvas.Children.Add(edgeLine);
        }

        private double LeftFromVertex(Vertex v)
        {
            return OrigX(v.X * fieldWidth, v.Y * fieldHeight);
        }

        private double TopFromVertex(Vertex v)
        {
            return OrigY(v.X * fieldWidth, v.Y * fieldHeight);
        }

        private LineCoordinates CoordsFromEdge(Edge e)
        {
            LineCoordinates coords = new LineCoordinates();
            coords.startX = LeftFromVertex(e.From);
            coords.endX = LeftFromVertex(e.To);
            coords.startY = TopFromVertex(e.From);
            coords.endY = TopFromVertex(e.To);

            return coords;
        }

        /// <summary>
        /// Event handler for MouseEnter event of each edge
        /// </summary>
        private void MouseOverEdge(object sender, RoutedEventArgs e)
        {
            if (!active) return;

            Line l = (Line)sender;

            Edge ed = (Edge)(l.Tag);

            if (!game.IsPlacement()) return;
            if (!synchronizer.IsActivePlayer()) return;

            if (!game.CanPlaceRail(game.GetActivePlayerId(), ed)) return;

            l.Stroke = Brushes.DarkCyan;
            l.StrokeThickness = highlightThickness;
        }

        /// <summary>
        /// Event handler for MouseLeave event of each vertex
        /// </summary
        private void MouseOutEdge(object sender, RoutedEventArgs e)
        {
            if (!active) return;

            Line l = (Line)sender;

            Edge ed = (Edge)(l.Tag);

            if (ed.HasRail)
            {
                l.StrokeThickness = railThickness;
                l.Stroke = Brushes.Red;
            }
            else if (ed.IsDouble)
            {
                l.Stroke = Brushes.Black;
                l.StrokeThickness = doubleThickness;
            }
            else 
            {
                l.Stroke = Brushes.Black;
                l.StrokeThickness = normalThickness;
            }
        }

        /// <summary>
        /// Event handler for MouseUp event of each vertex
        /// </summary>
        private void MouseClickEdge(object sender, RoutedEventArgs e)
        {
            if (!active) return;

            Line l = (Line)sender;
            Edge ed = (Edge)(l.Tag);

            if (!game.IsPlacement()) return;
            if (!synchronizer.IsActivePlayer()) return;

            if (!game.CanPlaceRail(game.GetActivePlayerId(), ed)) return;

            synchronizer.ClientPlaceRail(ed.From.X, ed.From.Y, ed.From.GetEdgeDirection(ed));
        }


        /// <summary>
        /// Event handler for MouseEnter event of each vertex, shows box with a city name
        /// </summary>
        private void MouseOverVertex(object sender, RoutedEventArgs e)
        {
            Ellipse el = (Ellipse)sender;

            Vertex v = (Vertex)(el.Tag);

            City c = v.CityOn;

            if (c != null)
            {
                RemoveHighlights();
                HighlightCity(c);
            }
            if (!active) return;

            if (!game.IsPositionSelection()) return;
            if (!synchronizer.IsActivePlayer()) return;
            if (v.BaseOf == null)
            {
                el.Fill = Brushes.DarkCyan;
            }
        }

        /// <summary>
        /// Event handler for MouseLeave event of each vertex, hides all boxes with city names
        /// </summary>
        private void MouseOutVertex(object sender, RoutedEventArgs e)
        {

            Ellipse el = (Ellipse)sender;
            Vertex v = (Vertex)el.Tag;
            City c = v.CityOn;
            Brush br = Brushes.Black;
            if (c != null)
            {
                switch (c.Color)
                {
                    case CityColor.BLUE: br = Brushes.LightBlue; break;
                    case CityColor.GREEN: br = Brushes.LightGreen; break;
                    case CityColor.ORANGE: br = Brushes.Orange; break;
                    case CityColor.RED: br = Brushes.Salmon; break;
                    case CityColor.YELLOW: br = Brushes.LightYellow; break;
                }
            }
            el.Fill = br;
            RemoveHighlights();
        }

        /// <summary>
        /// Event handler for MouseUp event of each vertex
        /// </summary>
        private void MouseClickVertex(object sender, RoutedEventArgs e)
        {
            if (!active) return;

            Ellipse el = (Ellipse)sender;

            Vertex v = (Vertex)(el.Tag);

            if (!game.IsPositionSelection()) return;
            if (!synchronizer.IsActivePlayer()) return;

            if (v.BaseOf != null) return;

            synchronizer.ClientSetStart(v.X, v.Y);


        }

        /// <summary>
        /// Method for calculating the transformation from grid representation of the map to the actual representation
        /// </summary>
        /// <param name="x">X coordinate of the point in grid representation</param>
        /// <param name="y">Y coordinate of the point in grid representation</param>
        /// <returns>X coordinate of the point in actual representation</returns>
        private double OrigX(double x, double y)
        {
            double origx = (x - OrigY(x, y) * Math.Cos(2 * Math.PI / 3));
            return origx;
        }

        /// <summary>
        /// Method for calculating the transformation from grid representation of the map to the actual representation
        /// </summary>
        /// <param name="x">X coordinate of the point in grid representation</param>
        /// <param name="y">Y coordinate of the point in grid representation</param>
        /// <returns>Y coordinate of the point in actual representation</returns>
        private double OrigY(double x, double y)
        {
            double origy = (y / Math.Sin(2 * Math.PI / 3));
            return origy;
        }

        /// <summary>
        /// Highlights specified city on the map
        /// </summary>
        /// <param name="c">City to be highlighted</param>
        public void HighlightCity(City c)
        {
            Vertex v = c.Position;

            TextBlock label = new TextBlock();
            label.FontSize = 15;
            label.Text = " " + c.Name + " ";
            label.Background = Brushes.White;

            Border b = new Border() { BorderThickness = new Thickness(2), BorderBrush = Brushes.Black };
            b.Child = label;

            Canvas.SetTop(b, OrigY(v.X * fieldWidth, v.Y * fieldHeight) * heightScale + topShift - 25);
            Canvas.SetLeft(b, OrigX(v.X * fieldWidth, v.Y * fieldHeight) + leftShift + 15);

            gameCanvas.Children.Add(b);
            highlights.Add(b);
        }

        /// <summary>
        /// Removes all highlights from the map
        /// </summary>
        public void RemoveHighlights()
        {
            foreach (UIElement e in highlights)
            {
                gameCanvas.Children.Remove(e);
            }
            highlights.Clear();
        }

    }
}
