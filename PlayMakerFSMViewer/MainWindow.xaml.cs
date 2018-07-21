using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Win32;
using Petzold.Media2D;
using PlayMakerFSMViewer.FieldClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UABE.NET.Assets;

namespace PlayMakerFSMViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //graphCanvas = FindName("graphCanvas") as Canvas;
            

            //graphCanvas.Children.Add(aline1);
        }

        #region Node code
        
        #endregion

        #region Drag code
        private Point _last;
        private bool isDragged = false;
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            CaptureMouse();
            _last = e.GetPosition(this);
            Cursor = Cursors.Hand;
            isDragged = true;
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
            isDragged = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragged == false)
                return;
            base.OnMouseMove(e);
            if (e.RightButton == MouseButtonState.Pressed && IsMouseCaptured)
            {
                var pos = e.GetPosition(this);
                var matrix = mt.Matrix;
                matrix.Translate(pos.X - _last.X, pos.Y - _last.Y);
                mt.Matrix = matrix;
                _last = pos;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var pos = e.GetPosition(this);
            var matrix = mt.Matrix;
            Point point = Mouse.GetPosition(graphCanvas);
            if (e.Delta > 1)
            {
                matrix.ScaleAtPrepend(1.1, 1.1, point.X, point.Y);
            } else
            {
                matrix.ScaleAtPrepend(0.9, 0.9, point.X, point.Y);
            }
            mt.Matrix = matrix;
            base.OnMouseWheel(e);
        }
        #endregion

        #region Arrows
        private List<Node> nodes = new List<Node>();
        //from CircularDependencyTool
        private Point ComputeLocation(Node node1, Node node2)
        {
            Rect nodetfm1 = node1.Transform;
            Rect nodetfm2 = node2.Transform;
            
            Point loc = new Point
            {
                X = nodetfm1.X + (nodetfm1.Width / 2),
                Y = nodetfm1.Y + (nodetfm1.Height / 2)
            };

            bool overlapY = Math.Abs(nodetfm1.Y - nodetfm2.Y) < nodetfm1.Height / 2;
            if (!overlapY)
            {
                bool above = nodetfm1.Y < nodetfm2.Y;
                if (above)
                    loc.Y += nodetfm1.Height / 2;
                else
                    loc.Y -= nodetfm1.Height / 2;
            }

            bool overlapX = Math.Abs(nodetfm1.X - nodetfm2.X) < nodetfm1.Width / 2;
            if (!overlapX)
            {
                bool left = nodetfm1.X < nodetfm2.X;
                if (left)
                    loc.X += nodetfm1.Width / 2;
                else
                    loc.X -= nodetfm1.Width / 2;
                loc.Y = nodetfm1.Y + 6;
            }

            return loc;
        }
        private Point ComputeLocation(Node node1, Node node2, float yPos, out bool isLeft)
        {
            Rect nodetfm1 = node1.Transform;
            Rect nodetfm2 = node2.Transform;

            Point loc = new Point
            {
                X = nodetfm1.X + (nodetfm1.Width / 2),
                Y = nodetfm1.Y + yPos
            };

            bool left = nodetfm1.X < nodetfm2.X;
            if (left)
                loc.X += nodetfm1.Width / 2;
            else
                loc.X -= nodetfm1.Width / 2;
            isLeft = left;

            return loc;
        }
        #endregion

        #region
        private void LoadFSMs(string path)
        {
            using (FileStream assetStream = new FileStream(path, FileMode.Open))
            {
                string folderName = System.IO.Path.GetDirectoryName(path);
                AssetsManager am = new AssetsManager();
                am.LoadAssets(assetStream, folderName);
                am.LoadClassFile(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cldb.dat"));

                List<AssetInfo> assetInfos = new List<AssetInfo>();
                uint assetCount = am.initialTable.assetFileInfoCount;
                uint fsmTypeId = 0;
                foreach (AssetFileInfoEx info in am.initialTable.pAssetFileInfo)
                {
                    bool isMono = false;
                    if (fsmTypeId == 0)
                    {
                        ushort monoType = am.initialFile.typeTree.pTypes_Unity5[info.curFileTypeOrIndex].scriptIndex;
                        if (monoType != 0xFFFF)
                        {
                            isMono = true;
                        }
                    } else if (info.curFileType == fsmTypeId)
                    {
                        isMono = true;
                    }
                    if (isMono)
                    {
                        AssetTypeInstance monoAti = am.GetATI(assetStream, info);
                        AssetTypeInstance scriptAti = am.GetExtAsset(monoAti.GetBaseField().Get("m_Script")).instance;
                        AssetTypeInstance goAti = am.GetExtAsset(monoAti.GetBaseField().Get("m_GameObject")).instance;
                        if (goAti == null) //found a scriptable object, oops
                        {
                            fsmTypeId = 0;
                            continue;
                        }
                        string m_Name = goAti.GetBaseField().Get("m_Name").GetValue().AsString();
                        string m_ClassName = scriptAti.GetBaseField().Get("m_ClassName").GetValue().AsString();

                        if (m_ClassName == "PlayMakerFSM")
                        {
                            if (fsmTypeId == 0)
                                fsmTypeId = info.curFileType;
                            
                            BinaryReader reader = new BinaryReader(assetStream);

                            long oldPos = assetStream.Position;
                            reader.BaseStream.Position = (long)info.absoluteFilePos;
                            reader.BaseStream.Position += 28;
                            uint length = reader.ReadUInt32();
                            reader.ReadBytes((int)length);

                            long pad = 4 - (reader.BaseStream.Position % 4);
                            if (pad != 4) reader.BaseStream.Position += pad;

                            reader.BaseStream.Position += 16;
                            
                            uint length2 = reader.ReadUInt32();
                            string fsmName = Encoding.ASCII.GetString(reader.ReadBytes((int)length2));
                            reader.BaseStream.Position = oldPos;

                            assetInfos.Add(new AssetInfo()
                            {
                                id = info.index,
                                size = info.curFileSize,
                                name = m_Name + "-" + fsmName
                            });
                        }
                    }
                }
                assetInfos.Sort((x, y) => x.name.CompareTo(y.name));
                FSMSelector selector = new FSMSelector(assetInfos);
                selector.ShowDialog();

                if (selector.selectedID == -1)
                    return;

                graphCanvas.Children.Clear();//.RemoveRange(1, graphCanvas.Children.Count - 1);
                stateList.Children.Clear();
                eventList.Children.Clear();
                variableList.Children.Clear();
                nodes.Clear();

                AssetFileInfoEx afi = am.initialTable.getAssetInfo((ulong)selector.selectedID);

                //from uabe
                ClassDatabaseType cldt = AssetHelper.FindAssetClassByID(am.initialClassFile, afi.curFileType);
                AssetTypeTemplateField pBaseField = new AssetTypeTemplateField();
                pBaseField.FromClassDatabase(am.initialClassFile, cldt, 0);
                AssetTypeInstance mainAti = new AssetTypeInstance(1, new[] { pBaseField }, am.initialFile.reader, false, afi.absoluteFilePos);
                AssetTypeTemplateField[] desMonos;
                desMonos = TryDeserializeMono(mainAti, am, folderName);
                if (desMonos != null)
                {
                    AssetTypeTemplateField[] templateField = pBaseField.children.Concat(desMonos).ToArray();
                    pBaseField.children = templateField;
                    pBaseField.childrenCount = (uint)pBaseField.children.Length;

                    mainAti = new AssetTypeInstance(1, new[] { pBaseField }, am.initialFile.reader, false, afi.absoluteFilePos);
                }
                AssetTypeValueField baseField = mainAti.GetBaseField();
                
                AssetTypeValueField fsm = baseField.Get("fsm");
                AssetTypeValueField states = fsm.Get("states");
                for (int i = 0; i < states.GetValue().AsArray().size; i++)
                {
                    AssetTypeValueField state = states.Get((uint)i);
                    //move all of this into node
                    string name = state.Get("name").GetValue().AsString();
                    AssetTypeValueField rect = state.Get("position");
                    Rect dotNetRect = new Rect(rect.Get("x").GetValue().AsFloat(),
                                               rect.Get("y").GetValue().AsFloat(),
                                               rect.Get("width").GetValue().AsFloat(),
                                               rect.Get("height").GetValue().AsFloat());
                    AssetTypeValueField transitions = state.Get("transitions");
                    uint transitionCount = transitions.GetValue().AsArray().size;
                    FsmTransition[] dotNetTransitions = new FsmTransition[transitionCount];
                    for (int j = 0; j < transitionCount; j++)
                    {
                        dotNetTransitions[j] = new FsmTransition(transitions.Get((uint)j));
                    }
                    Node node = new Node(state, name, dotNetRect, dotNetTransitions);
                    nodes.Add(node);

                    node.grid.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
                    {
                        foreach (Node node2 in nodes)
                        {
                            node2.Selected = false;
                        }
                        node.Selected = true;
                        SidebarData(node);
                    };

                    graphCanvas.Children.Add(node.grid);
                }
                foreach (Node node in nodes)
                {
                    if (node.transitions.Length > 0)
                    {
                        float yPos = 24;
                        foreach (FsmTransition trans in node.transitions)
                        {
                            Node endNode = nodes.Where(n => n.name == trans.toState).FirstOrDefault();
                            if (endNode != null)
                            {
                                Point start = ComputeLocation(node, endNode, yPos, out bool isLeft);
                                Point end = ComputeLocation(endNode, node, 10, out bool dummy);

                                Point startMiddle, endMiddle;
                                double dist = 70;
                                if (!isLeft)
                                {
                                    startMiddle = new Point(start.X - dist, start.Y);
                                    endMiddle = new Point(end.X + dist, end.Y);
                                }
                                else
                                {
                                    startMiddle = new Point(start.X + dist, start.Y);
                                    endMiddle = new Point(end.X - dist, end.Y);
                                }

                                CurvedArrow arrow = new CurvedArrow()
                                {
                                    Points = new PointCollection(new List<Point>()
                                    {
                                        start,
                                        startMiddle,
                                        endMiddle,
                                        end
                                    }),
                                    StrokeThickness = 2,
                                    Stroke = Brushes.Black,
                                    Fill = Brushes.Black,
                                    IsHitTestVisible = true
                                };

                                arrow.MouseEnter += (object sender, MouseEventArgs e) =>
                                {
                                    arrow.Stroke = Brushes.LightGray;
                                    arrow.Fill = Brushes.LightGray;
                                };

                                arrow.MouseLeave += (object sender, MouseEventArgs e) =>
                                {
                                    arrow.Stroke = Brushes.Black;
                                    arrow.Fill = Brushes.Black;
                                };

                                Panel.SetZIndex(arrow, -1);

                                //ArrowLine arrowLine = new ArrowLine()
                                //{
                                //    IsArrowClosed = true,
                                //    Stroke = Brushes.Black,
                                //    StrokeThickness = 2,
                                //    ArrowLength = 8,
                                //    X1 = start.X,
                                //    Y1 = start.Y,
                                //    X2 = end.X,
                                //    Y2 = end.Y
                                //};

                                graphCanvas.Children.Add(arrow);
                            } else
                            {
                                System.Diagnostics.Debug.WriteLine(node.name + " failed to connect to " + trans.toState);
                            }
                            yPos += 16;
                        }
                    }
                }
                AssetTypeValueField events = fsm.Get("events");
                for (int i = 0; i < events.GetValue().AsArray().size; i++)
                {
                    AssetTypeValueField @event = events.Get((uint) i);
                    string name = @event.Get("name").GetValue().AsString();
                    bool isSystemEvent = @event.Get("isSystemEvent").GetValue().AsBool();
                    bool isGlobal = @event.Get("isGlobal").GetValue().AsBool();
                    
                    eventList.Children.Add(CreateSidebarRow(name, isSystemEvent, isGlobal));
                }
                AssetTypeValueField variables = fsm.Get("variables");
                AssetTypeValueField floatVariables = variables.Get("floatVariables");
                AssetTypeValueField intVariables = variables.Get("intVariables");
                AssetTypeValueField boolVariables = variables.Get("boolVariables");
                AssetTypeValueField stringVariables = variables.Get("stringVariables");
                AssetTypeValueField vector2Variables = variables.Get("vector2Variables");
                AssetTypeValueField vector3Variables = variables.Get("vector3Variables");
                AssetTypeValueField colorVariables = variables.Get("colorVariables");
                AssetTypeValueField rectVariables = variables.Get("rectVariables");
                AssetTypeValueField quaternionVariables = variables.Get("quaternionVariables");
                AssetTypeValueField gameObjectVariables = variables.Get("gameObjectVariables");
                AssetTypeValueField objectVariables = variables.Get("objectVariables");
                AssetTypeValueField materialVariables = variables.Get("materialVariables");
                AssetTypeValueField textureVariables = variables.Get("textureVariables");
                AssetTypeValueField arrayVariables = variables.Get("arrayVariables");
                AssetTypeValueField enumVariables = variables.Get("enumVariables");
                variableList.Children.Add(CreateSidebarHeader("Floats"));
                for (int i = 0; i < floatVariables.GetValue().AsArray().size; i++)
                {
                    string name = floatVariables.Get((uint)i).Get("name").GetValue().AsString();
                    string value = floatVariables.Get((uint)i).Get("value").GetValue().AsFloat().ToString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Ints"));
                for (int i = 0; i < intVariables.GetValue().AsArray().size; i++)
                {
                    string name = intVariables.Get((uint)i).Get("name").GetValue().AsString();
                    string value = intVariables.Get((uint)i).Get("value").GetValue().AsInt().ToString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Bools"));
                for (int i = 0; i < boolVariables.GetValue().AsArray().size; i++)
                {
                    string name = boolVariables.Get((uint)i).Get("name").GetValue().AsString();
                    string value = boolVariables.Get((uint)i).Get("value").GetValue().AsBool().ToString().ToLower();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Strings"));
                for (int i = 0; i < stringVariables.GetValue().AsArray().size; i++)
                {
                    string name = stringVariables.Get((uint)i).Get("name").GetValue().AsString();
                    string value = stringVariables.Get((uint)i).Get("value").GetValue().AsString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Vector2s"));
                for (int i = 0; i < vector2Variables.GetValue().AsArray().size; i++)
                {
                    string name = vector2Variables.Get((uint)i).Get("name").GetValue().AsString();
                    AssetTypeValueField vector2 = vector2Variables.Get((uint)i).Get("value");
                    string value =  vector2.Get("x").GetValue().AsFloat().ToString() + ", ";
                           value += vector2.Get("y").GetValue().AsFloat().ToString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Vector3s"));
                for (int i = 0; i < vector3Variables.GetValue().AsArray().size; i++)
                {
                    string name = vector3Variables.Get((uint)i).Get("name").GetValue().AsString();
                    AssetTypeValueField vector3 = vector3Variables.Get((uint)i).Get("value");
                    string value =  vector3.Get("x").GetValue().AsFloat().ToString() + ", ";
                           value += vector3.Get("x").GetValue().AsFloat().ToString() + ", ";
                           value += vector3.Get("z").GetValue().AsFloat().ToString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Colors"));
                for (int i = 0; i < colorVariables.GetValue().AsArray().size; i++)
                {
                    string name = colorVariables.Get((uint)i).Get("name").GetValue().AsString();
                    AssetTypeValueField color = colorVariables.Get((uint)i).Get("value");
                    string value =  color.Get("r").GetValue().AsFloat().ToString("X2");
                           value += color.Get("g").GetValue().AsFloat().ToString("X2");
                           value += color.Get("b").GetValue().AsFloat().ToString("X2");
                           value += color.Get("a").GetValue().AsFloat().ToString("X2");
                    Grid sidebarRow = CreateSidebarRow(name, value);
                    TextBox textBox = (TextBox)sidebarRow.Children[1];
                    textBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom(value));
                    variableList.Children.Add(sidebarRow);
                }
                variableList.Children.Add(CreateSidebarHeader("Rects"));
                for (int i = 0; i < rectVariables.GetValue().AsArray().size; i++)
                {
                    string name = rectVariables.Get((uint)i).Get("name").GetValue().AsString();
                    AssetTypeValueField rect = rectVariables.Get((uint)i).Get("value");
                    string value =  rect.Get("x").GetValue().AsFloat().ToString() + ", ";
                           value += rect.Get("y").GetValue().AsFloat().ToString() + ", ";
                           value += rect.Get("width").GetValue().AsFloat().ToString() + ", ";
                           value += rect.Get("height").GetValue().AsFloat().ToString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("Quaternions"));
                for (int i = 0; i < quaternionVariables.GetValue().AsArray().size; i++)
                {
                    string name = quaternionVariables.Get((uint)i).Get("name").GetValue().AsString();
                    AssetTypeValueField rect = quaternionVariables.Get((uint)i).Get("value");
                    string value =  rect.Get("x").GetValue().AsFloat().ToString() + ", ";
                           value += rect.Get("y").GetValue().AsFloat().ToString() + ", ";
                           value += rect.Get("z").GetValue().AsFloat().ToString() + ", ";
                           value += rect.Get("w").GetValue().AsFloat().ToString();
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
                variableList.Children.Add(CreateSidebarHeader("GameObjects"));
                for (int i = 0; i < gameObjectVariables.GetValue().AsArray().size; i++)
                {
                    string name = gameObjectVariables.Get((uint)i).Get("name").GetValue().AsString();
                    AssetTypeValueField gameObject = gameObjectVariables.Get((uint)i).Get("value");
                    int m_FileID = gameObject.Get("m_FileID").GetValue().AsInt();
                    long m_PathID = gameObject.Get("m_PathID").GetValue().AsInt64();

                    string value;
                    if (m_PathID != 0)
                    {
                        value = $"[{m_FileID},{m_PathID}]";
                    } else
                    {
                        value = "";
                    }
                    variableList.Children.Add(CreateSidebarRow(name, value));
                }
            }
        }

        private void SidebarData(Node node)
        {
            stateList.Children.Clear();

            AssetTypeValueField actionData = node.state.Get("actionData");
            uint actionCount = actionData.Get("actionNames").GetValue().AsArray().size;
            string[] actionValues = ActionReader.ActionValues(actionData);
            for (int i = 0; i < actionCount; i++)
            {
                string actionName = actionData.Get("actionNames").Get((uint)i).GetValue().AsString();
                if (actionName.Contains("."))
                    actionName = actionName.Substring(actionName.LastIndexOf(".")+1);
                stateList.Children.Add(CreateSidebarHeader(actionName));
                int startParam = actionData.Get("actionStartIndex").Get((uint)i).GetValue().AsInt();
                int endParam;
                if (i == actionCount - 1)
                {
                    endParam = (int)actionData.Get("paramDataType").GetValue().AsArray().size;
                }
                else
                {
                    endParam = actionData.Get("actionStartIndex").Get((uint)i + 1).GetValue().AsInt();
                }
                for (int j = startParam; j < endParam; j++)
                {
                    string paramName = actionData.Get("paramName").Get((uint)j).GetValue().AsString();
                    stateList.Children.Add(CreateSidebarRow(paramName, actionValues[j]));
                }
            }
        }

        private Label CreateSidebarHeader(string text)
        {
            Label header = new Label()
            {
                Content = text,
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 25,
                FontWeight = FontWeights.Bold
            };
            return header;
        }

        private Grid CreateSidebarRow(string key, string value)
        {
            Grid valueContainer = new Grid()
            {
                Height = 25,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.LightGray
            };
            Label valueLabel = new Label()
            {
                Content = key,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 120
            };
            TextBox valueBox = new TextBox()
            {
                Margin = new Thickness(125, 0, 0, 0),
                IsReadOnly = true,
                Text = value
            };
            valueContainer.Children.Add(valueLabel);
            valueContainer.Children.Add(valueBox);
            return valueContainer;
        }

        private Grid CreateSidebarRow(string key, bool system, bool global)
        {
            Grid valueContainer = new Grid()
            {
                Height = 25,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.LightGray
            };
            Label valueLabel = new Label()
            {
                Content = key,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 160
            };
            if (system)
            {
                valueLabel.FontWeight = FontWeights.Bold;
            }
            CheckBox checkBox = new CheckBox()
            {
                Margin = new Thickness(165, 0, 0, 0),
                IsEnabled = false,
                IsChecked = global,
                Content = "Global"
            };
            valueContainer.Children.Add(valueLabel);
            valueContainer.Children.Add(checkBox);
            return valueContainer;
        }

        private AssetTypeTemplateField[] TryDeserializeMono(AssetTypeInstance ati, AssetsManager am, string rootDir)
        {
            AssetTypeInstance scriptAti = am.GetExtAsset(ati.GetBaseField().Get("m_Script")).instance;
            string scriptName = scriptAti.GetBaseField().Get("m_Name").GetValue().AsString();
            string assemblyName = scriptAti.GetBaseField().Get("m_AssemblyName").GetValue().AsString();
            string assemblyPath = System.IO.Path.Combine(rootDir, "Managed", assemblyName);
            if (File.Exists(assemblyPath))
            {
                MonoClass mc = new MonoClass();
                mc.Read(scriptName, assemblyPath);
                return mc.children;
            }
            else
            {
                return null;
            }
        }
        #endregion

        private string lastFilename;
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                lastFilename = openFileDialog.FileName;
                openLast.IsEnabled = true;
                LoadFSMs(openFileDialog.FileName);
            }
        }

        private void OpenLast_Click(object sender, RoutedEventArgs e)
        {
            LoadFSMs(lastFilename);
        }
    }
}
