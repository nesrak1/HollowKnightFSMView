using AssetsTools.NET;
using PlayMakerFSMViewer.FieldClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PlayMakerFSMViewer
{
    public class Node
    {
        public Grid grid;
        public RectangleGeometry rectGeom;
        public Path rectPath;
        public Label label;
        public AssetTypeValueField state;
        public FsmTransition[] transitions;
        public string name;
        private bool selected;
        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                if (selected)
                {
                    rectPath.StrokeThickness = 2;
                }
                else
                {
                    rectPath.StrokeThickness = 1;
                }
            }
        }

        public Rect Transform
        {
            get
            {
                return new Rect((double)grid.GetValue(Canvas.LeftProperty),
                                (double)grid.GetValue(Canvas.TopProperty),
                                        rectGeom.Rect.Width,
                                        rectGeom.Rect.Height);
            }
            set
            {
                grid.SetValue(Canvas.LeftProperty, value.X);
                grid.SetValue(Canvas.TopProperty, value.Y);
                rectGeom.Rect = new Rect(0, 0, value.Width, value.Height);
            }
        }

        public Node(AssetTypeValueField state, string name, int x, int y, int width, int height, FsmTransition[] transitions) :
                    this(state, name, new Rect(x, y, width, height), Brushes.LightGray, Brushes.Black, transitions) {}
        public Node(AssetTypeValueField state, string name, Rect transform, FsmTransition[] transitions) :
                    this(state, name, transform, Brushes.LightGray, Brushes.Black, transitions) {}

        public Node(AssetTypeValueField state, string name, Rect transform, Brush fill, Brush stroke, FsmTransition[] transitions)
        {
            this.state = state;
            this.transitions = transitions;

            this.name = name;
            foreach (FsmTransition transition in transitions)
            {
                name += "\n" + transition.fsmEvent.name;
            }

            grid = new Grid();
            grid.SetValue(Canvas.LeftProperty, transform.X);
            grid.SetValue(Canvas.TopProperty, transform.Y);

            rectGeom = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, transform.Width, transform.Height),
                RadiusX = 1,
                RadiusY = 1
            };

            rectPath = new Path()
            {
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = 1,
                Opacity = 0.75,
                Data = rectGeom
            };

            label = new Label()
            {
                Foreground = Brushes.Gray,
                Content = name,
                Padding = new Thickness(1),
                FontFamily = new FontFamily("Segoe UI Bold"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            grid.Children.Add(rectPath);
            grid.Children.Add(label);
            //graphCanvas.Children.Add(grid);
        }
    }
}
