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
        public Rectangle rect;
        public Path rectPath;
        public Rect initialTransform;
        public Label label;
        public Brush stroke;
        public AssetTypeValueField state;
        public FsmTransition[] transitions;
        public string name;
        private bool selected;

        private readonly Color[] _stateColors =
        {
            Color.FromRgb(128, 128, 128),
            Color.FromRgb(116, 143, 201),
            Color.FromRgb(58, 182, 166),
            Color.FromRgb(93, 164, 53),
            Color.FromRgb(225, 254, 50),
            Color.FromRgb(235, 131, 46),
            Color.FromRgb(187, 75, 75),
            Color.FromRgb(117, 53, 164)
        };

        private readonly Color[] _transitionColors =
        {
            Color.FromRgb(222, 222, 222),
            Color.FromRgb(197, 213, 248),
            Color.FromRgb(159, 225, 216),
            Color.FromRgb(183, 225, 159),
            Color.FromRgb(225, 254, 102),
            Color.FromRgb(255, 198, 152),
            Color.FromRgb(225, 159, 160),
            Color.FromRgb(197, 159, 225)
        };

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;

                rect.Stroke = selected
                    ? Brushes.LightBlue
                    : stroke;

                rect.StrokeThickness = selected
                    ? 8
                    : 2;

                //add border and fix offset
                if (selected)
                {
                    rect.Margin = new Thickness(0, -2, 0, 0);
                    Transform = new Rect(initialTransform.X - 1, initialTransform.Y, initialTransform.Width + 4, initialTransform.Height + 5);
                }
                else
                {
                    rect.Margin = new Thickness(0, -1, 0, 0);
                    Transform = new Rect(initialTransform.X, initialTransform.Y, initialTransform.Width + 2, initialTransform.Height + 3);
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
                rect.Width = value.Width;
                rect.Height = value.Height;
            }
        }

        public Node(AssetTypeValueField state, string name, int x, int y, int width, int height, FsmTransition[] transitions) :
                    this(state, name, new Rect(x, y, width, height), Brushes.LightGray, Brushes.Black, transitions)
        { }
        public Node(AssetTypeValueField state, string name, Rect transform, FsmTransition[] transitions) :
                    this(state, name, transform, Brushes.LightGray, Brushes.Black, transitions)
        { }

        public Node(AssetTypeValueField state, string name, Rect transform, Brush fill, Brush stroke, FsmTransition[] transitions)
        {
            this.state = state;
            this.transitions = transitions;
            this.name = name;

            this.stroke = stroke;

            bool isGlobal = state == null;

            initialTransform = transform;

            grid = new Grid();
            grid.SetValue(Canvas.LeftProperty, transform.X);
            grid.SetValue(Canvas.TopProperty, transform.Y);

            rectGeom = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, transform.Width, transform.Height),
                RadiusX = 1,
                RadiusY = 1
            };

            rect = new Rectangle()
            {
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = 2,
                Opacity = 0.75,
                Width = transform.Width + 2,
                Height = transform.Height + 3,
                RadiusX = 1,
                RadiusY = 1,
                Margin = new Thickness(0, -1, 0, 0)
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

            byte cIndex;
            if (state != null && !state.Get("colorIndex").IsDummy())
                cIndex = (byte)state.Get("colorIndex").GetValue().AsUInt();
            else
                cIndex = 0;

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

            if (isGlobal)
                label.Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));

            stack.Children.Add(label);

            if (!isGlobal)
            {
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

                    // stops lowercase descenders in the state titles
                    // from getting cut-off
                    i.MaxHeight = index == 0
                        ? (i.MinHeight = transform.Height / list.Count + 1.4)
                        : (i.MinHeight = (transform.Height - 1.4) / list.Count);
                }
            }

            grid.Children.Add(rect);
            grid.Children.Add(stack);
            //graphCanvas.Children.Add(grid);
        }
    }
}
