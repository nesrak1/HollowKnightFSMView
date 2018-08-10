using AssetsTools.NET;
using PlayMakerFSMViewer.FieldClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        
        private readonly Color[] _stateColors = 
        {
            // Color.FromScRgb(1, 0.5f, 0.5f, 0.5f), 
            // Color.FromScRgb(1, 0.545098066f, 0.670588255f, 0.9411765f),
            // Color.FromScRgb(1, 0.243137255f, 0.7607843f, 0.6901961f),
            // Color.FromScRgb(1, 0.431372553f, 0.7607843f, 0.243137255f),
            // Color.FromScRgb(1, 1f, 0.8745098f, 0.1882353f),
            // Color.FromScRgb(1, 1f, 0.5529412f, 0.1882353f),
            // Color.FromScRgb(1, 0.7607843f, 0.243137255f, 0.2509804f),
            // Color.FromScRgb(1, 0.545098066f, 0.243137255f, 0.7607843f)
            Color.FromRgb(128, 128, 128), // grey
            Color.FromRgb(116, 143, 201), // blue
            Color.FromRgb(58, 182, 166), // cyan
            Color.FromRgb(93, 164, 53), // green
            Color.FromRgb(225, 254, 50), // yellow
            Color.FromRgb(235, 131, 46), // orange
            Color.FromRgb(187, 75, 75), // red
            Color.FromRgb(117, 53, 164) // purple
        };
        
        private readonly Color[] _transitionColors = 
        {
            Color.FromRgb(222, 222, 222), // grey
            Color.FromRgb(197, 213, 248), // blue
            Color.FromRgb(159, 225, 216), // cyan
            Color.FromRgb(183, 225, 159), // green
            Color.FromRgb(225, 254, 102), // yellow
            Color.FromRgb(255, 198, 152), // orange
            Color.FromRgb(225, 159, 160), // red
            Color.FromRgb(197, 159, 225) // purple
        };

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                rectPath.StrokeThickness = selected 
                    ? 2 
                    : 1;
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
            
            grid = new Grid();
            grid.SetValue(Canvas.LeftProperty, transform.X);
            grid.SetValue(Canvas.TopProperty, transform.Y);

            rectGeom = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, transform.Width, transform.Height),
                RadiusX = 1,
                RadiusY = 1
            };

            rectPath = new Path
            {
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = 1,
                Opacity = 0.75,
                Data = rectGeom
            };

            FontFamily font = new FontFamily("Segoe UI Bold");

            StackPanel stack = new StackPanel();

            byte cIndex = (byte) state.Get("colorIndex").GetValue().AsUInt();

            label = new Label
            {
                Foreground = Brushes.White,
                Content = name,
                Padding = new Thickness(1),
                FontFamily = font,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1, 1, 1, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(_stateColors[cIndex]),
                MaxWidth = transform.Width,
                MinWidth = transform.Width
            };

            stack.Children.Add(label);
            
            foreach (FsmTransition transition in transitions)
            {
                stack.Children.Add(new Label
                {
                    Background = new SolidColorBrush(_transitionColors[cIndex]),
                    Foreground = Brushes.DimGray,
                    Content = transition.fsmEvent.name,
                    Padding = new Thickness(1),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1, .5, 1, .25),
                    FontFamily = font,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    MaxWidth = transform.Width,
                    MinWidth = transform.Width
                });
            }

            List<Label> list = stack.Children.OfType<Label>().ToList();
            for (int index = 0; index < list.Count; index++)
            {
                Label i = list[index];
                Grid.SetRow(i, index);
                i.MaxHeight = i.MinHeight = transform.Height / list.Count;
            }

            grid.Children.Add(rectPath);
            grid.Children.Add(stack);
            //graphCanvas.Children.Add(grid);
        }
    }
}
