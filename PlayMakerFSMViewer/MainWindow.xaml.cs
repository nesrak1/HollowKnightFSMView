using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Win32;
using PlayMakerFSMViewer.FieldClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;

namespace PlayMakerFSMViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AssetsManager am;
        AssetsFileInstance curFile;
        public MainWindow()
        {
            InitializeComponent();
        }

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
            //for laptops and such
            double scale = 1 + (double)e.Delta / 1200;
            matrix.ScaleAtPrepend(scale, scale, point.X, point.Y);
            mt.Matrix = matrix;
            base.OnMouseWheel(e);
        }
        #endregion

        #region Arrows
        private Point ComputeLocation(Node node1, Node node2, float yPos, out bool isLeft)
        {
            Rect nodetfm1 = node1.Transform;
            Rect nodetfm2 = node2.Transform;
            double midx1 = nodetfm1.X + nodetfm1.Width / 2;
            double midx2 = nodetfm2.X + nodetfm2.Width / 2;
            double midy1 = nodetfm1.Y + nodetfm1.Height / 2;
            double midy2 = nodetfm2.Y + nodetfm2.Height / 2;

            Point loc = new Point
            {
                X = nodetfm1.X + nodetfm1.Width / 2,
                Y = nodetfm1.Y + yPos
            };

            if (midx1 == midx2)
            {
                isLeft = true;
            }
            else
            {
                if (Math.Abs(midx1 - midx2) * 2 < nodetfm1.Width + nodetfm2.Width)
                {
                    if (midy2 > midy1)
                        isLeft = midx1 < midx2;
                    else
                        isLeft = midx1 > midx2;
                }
                else
                {
                    isLeft = midx1 < midx2;
                }
            }

            if (isLeft)
                loc.X += nodetfm1.Width / 2;
            else
                loc.X -= nodetfm1.Width / 2;

            return loc;
        }
        #endregion

        private int currentTab = -1;
        private List<FSMInstance> tabs = new List<FSMInstance>();
        private List<Node> nodes = new List<Node>();
        private int dataVersion = -1;
        private bool ignoreChangeEvent;

        private void LoadFSMs(string path)
        {
            string folderName = Path.GetDirectoryName(path);

            curFile = am.LoadAssetsFile(path, true);
            am.UpdateDependencies();

            AssetsFile file = curFile.file;
            AssetsFileTable table = curFile.table;

            List<AssetInfo> assetInfos = new List<AssetInfo>();
            uint assetCount = table.assetFileInfoCount;
            uint fsmTypeId = 0;
            foreach (AssetFileInfoEx info in table.assetFileInfo)
            {
                bool isMono = false;
                if (fsmTypeId == 0)
                {
                    ushort monoType = file.typeTree.unity5Types[info.curFileTypeOrIndex].scriptIndex;
                    if (monoType != 0xFFFF)
                    {
                        isMono = true;
                    }
                }
                else if (info.curFileType == fsmTypeId)
                {
                    isMono = true;
                }
                if (isMono)
                {
                    AssetTypeInstance monoAti = am.GetATI(file, info);
                    AssetTypeInstance scriptAti = am.GetExtAsset(curFile, monoAti.GetBaseField().Get("m_Script")).instance;
                    AssetTypeInstance goAti = am.GetExtAsset(curFile, monoAti.GetBaseField().Get("m_GameObject")).instance;
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

                        BinaryReader reader = file.reader;

                        long oldPos = reader.BaseStream.Position;
                        reader.BaseStream.Position = info.absoluteFilePos;
                        reader.BaseStream.Position += 28;
                        uint length = reader.ReadUInt32();
                        reader.ReadBytes((int)length);

                        long pad = 4 - (reader.BaseStream.Position % 4);
                        if (pad != 4) reader.BaseStream.Position += pad;

                        reader.BaseStream.Position += 16;

                        uint length2 = reader.ReadUInt32();
                        string fsmName = Encoding.UTF8.GetString(reader.ReadBytes((int)length2));
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

            //todo separate into separate method(s)
            if (selector.selectedID == -1)
                return;
            
            AssetFileInfoEx afi = table.GetAssetInfo(selector.selectedID);

            string tabName = assetInfos.FirstOrDefault(i => i.id == selector.selectedID).name;
            TabItem tab = new TabItem
            {
                Header = tabName
            };
            ignoreChangeEvent = true;
            fsmTabControl.Items.Add(tab);
            fsmTabControl.SelectedItem = tab;
            ignoreChangeEvent = false;

            SaveAndClearNodes();
            mt.Matrix = Matrix.Identity;

            AssetTypeValueField baseField = am.GetMonoBaseFieldCached(curFile, afi, Path.Combine(Path.GetDirectoryName(curFile.path), "Managed"));

            AssetTypeValueField fsm = baseField.Get("fsm");
            AssetTypeValueField states = fsm.Get("states");
            AssetTypeValueField globalTransitions = fsm.Get("globalTransitions");
            dataVersion = fsm.Get("dataVersion").GetValue().AsInt();
            for (int i = 0; i < states.GetValue().AsArray().size; i++)
            {
                AssetTypeValueField state = states.Get(i);
                //move all of this into node
                string name = state.Get("name").GetValue().AsString();
                AssetTypeValueField rect = state.Get("position");
                Rect dotNetRect = new Rect(rect.Get("x").GetValue().AsFloat(),
                                            rect.Get("y").GetValue().AsFloat(),
                                            rect.Get("width").GetValue().AsFloat(),
                                            rect.Get("height").GetValue().AsFloat());
                AssetTypeValueField transitions = state.Get("transitions");
                int transitionCount = transitions.GetValue().AsArray().size;
                FsmTransition[] dotNetTransitions = new FsmTransition[transitionCount];
                for (int j = 0; j < transitionCount; j++)
                {
                    dotNetTransitions[j] = new FsmTransition(transitions.Get(j));
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
                    SidebarData(node, curFile);
                };

                graphCanvas.Children.Add(node.grid);
            }
            for (int i = 0; i < globalTransitions.GetValue().AsArray().size; i++)
            {
                AssetTypeValueField transition = globalTransitions.Get(i);

                FsmTransition dotNetTransition = new FsmTransition(transition);
                Node toNode = nodes.FirstOrDefault(n => n.name == dotNetTransition.toState);

                if (toNode == null)
                {
                    Debug.WriteLine("transition " + dotNetTransition.fsmEvent.name + " going to non-existant node " + dotNetTransition.toState);
                }
                else
                {
                    Rect rect = new Rect(
                        toNode.Transform.X,
                        toNode.Transform.Y - 50,
                        toNode.Transform.Width,
                        18);

                    if (toNode != null)
                    {
                        Node node = new Node(null, dotNetTransition.fsmEvent.name, rect, new[] { dotNetTransition });
                        nodes.Add(node);

                        graphCanvas.Children.Add(node.grid);
                    }
                }
            }
            foreach (Node node in nodes)
            {
                if (node.transitions.Length <= 0) continue;
                    
                float yPos = 24;
                foreach (FsmTransition trans in node.transitions)
                {
                    Node endNode = nodes.FirstOrDefault(n => n.name == trans.toState);
                    if (endNode != null)
                    {

                        Point start, end, startMiddle, endMiddle;

                        if (node.state != null)
                        {
                            start = ComputeLocation(node, endNode, yPos, out bool isLeftStart);
                            end = ComputeLocation(endNode, node, 10, out bool isLeftEnd);

                            double dist = 70;

                            if (isLeftStart == isLeftEnd)
                                dist *= 0.5;

                            if (!isLeftStart)
                                startMiddle = new Point(start.X - dist, start.Y);
                            else
                                startMiddle = new Point(start.X + dist, start.Y);

                            if (!isLeftEnd)
                                endMiddle = new Point(end.X - dist, end.Y);
                            else
                                endMiddle = new Point(end.X + dist, end.Y);
                        }
                        else
                        {
                            start = new Point(node.Transform.X + node.Transform.Width / 2,
                                              node.Transform.Y + node.Transform.Height);
                            end = new Point(endNode.Transform.X + endNode.Transform.Width / 2,
                                              endNode.Transform.Y);
                            startMiddle = new Point(start.X, start.Y + 1);
                            endMiddle = new Point(end.X, end.Y - 1);
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

                        graphCanvas.Children.Add(arrow);
                    }
                    else
                    {
                        Debug.WriteLine(node.name + " failed to connect to " + trans.toState);
                    }
                    yPos += 16;
                }
            }
            AssetTypeValueField events = fsm.Get("events");
            for (int i = 0; i < events.GetValue().AsArray().size; i++)
            {
                AssetTypeValueField @event = events.Get(i);
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
                string name = floatVariables.Get(i).Get("name").GetValue().AsString();
                string value = floatVariables.Get(i).Get("value").GetValue().AsFloat().ToString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Ints"));
            for (int i = 0; i < intVariables.GetValue().AsArray().size; i++)
            {
                string name = intVariables.Get(i).Get("name").GetValue().AsString();
                string value = intVariables.Get(i).Get("value").GetValue().AsInt().ToString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Bools"));
            for (int i = 0; i < boolVariables.GetValue().AsArray().size; i++)
            {
                string name = boolVariables.Get(i).Get("name").GetValue().AsString();
                string value = boolVariables.Get(i).Get("value").GetValue().AsBool().ToString().ToLower();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Strings"));
            for (int i = 0; i < stringVariables.GetValue().AsArray().size; i++)
            {
                string name = stringVariables.Get(i).Get("name").GetValue().AsString();
                string value = stringVariables.Get(i).Get("value").GetValue().AsString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Vector2s"));
            for (int i = 0; i < vector2Variables.GetValue().AsArray().size; i++)
            {
                string name = vector2Variables.Get(i).Get("name").GetValue().AsString();
                AssetTypeValueField vector2 = vector2Variables.Get(i).Get("value");
                string value =  vector2.Get("x").GetValue().AsFloat().ToString() + ", ";
                        value += vector2.Get("y").GetValue().AsFloat().ToString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Vector3s"));
            for (int i = 0; i < vector3Variables.GetValue().AsArray().size; i++)
            {
                string name = vector3Variables.Get(i).Get("name").GetValue().AsString();
                AssetTypeValueField vector3 = vector3Variables.Get(i).Get("value");
                string value =  vector3.Get("x").GetValue().AsFloat().ToString() + ", ";
                        value += vector3.Get("x").GetValue().AsFloat().ToString() + ", ";
                        value += vector3.Get("z").GetValue().AsFloat().ToString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Colors"));
            for (int i = 0; i < colorVariables.GetValue().AsArray().size; i++)
            {
                string name = colorVariables.Get(i).Get("name").GetValue().AsString();
                AssetTypeValueField color = colorVariables.Get(i).Get("value");
                string value =  ((int)color.Get("r").GetValue().AsFloat() * 255).ToString("X2");
                        value += ((int)color.Get("g").GetValue().AsFloat() * 255).ToString("X2");
                        value += ((int)color.Get("b").GetValue().AsFloat() * 255).ToString("X2");
                        value += ((int)color.Get("a").GetValue().AsFloat() * 255).ToString("X2");
                Grid sidebarRow = CreateSidebarRow(name, value);
                TextBox textBox = sidebarRow.Children.OfType<TextBox>().FirstOrDefault();
                textBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#" + value));
                variableList.Children.Add(sidebarRow);
            }
            variableList.Children.Add(CreateSidebarHeader("Rects"));
            for (int i = 0; i < rectVariables.GetValue().AsArray().size; i++)
            {
                string name = rectVariables.Get(i).Get("name").GetValue().AsString();
                AssetTypeValueField rect = rectVariables.Get(i).Get("value");
                string value =  rect.Get("x").GetValue().AsFloat().ToString() + ", ";
                        value += rect.Get("y").GetValue().AsFloat().ToString() + ", ";
                        value += rect.Get("width").GetValue().AsFloat().ToString() + ", ";
                        value += rect.Get("height").GetValue().AsFloat().ToString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("Quaternions"));
            for (int i = 0; i < quaternionVariables.GetValue().AsArray().size; i++)
            {
                string name = quaternionVariables.Get(i).Get("name").GetValue().AsString();
                AssetTypeValueField rect = quaternionVariables.Get(i).Get("value");
                string value =  rect.Get("x").GetValue().AsFloat().ToString() + ", ";
                        value += rect.Get("y").GetValue().AsFloat().ToString() + ", ";
                        value += rect.Get("z").GetValue().AsFloat().ToString() + ", ";
                        value += rect.Get("w").GetValue().AsFloat().ToString();
                variableList.Children.Add(CreateSidebarRow(name, value));
            }
            variableList.Children.Add(CreateSidebarHeader("GameObjects"));
            for (int i = 0; i < gameObjectVariables.GetValue().AsArray().size; i++)
            {
                string name = gameObjectVariables.Get(i).Get("name").GetValue().AsString();
                AssetTypeValueField gameObject = gameObjectVariables.Get(i).Get("value");
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

            currentTab++;
            tabs.Add(new FSMInstance()
            {
                matrix = mt.Matrix,
                nodes = nodes,
                dataVersion = dataVersion,
                graphElements = CopyChildrenToList(graphCanvas),
                states = CopyChildrenToList(stateList),
                events = CopyChildrenToList(eventList),
                variables = CopyChildrenToList(variableList)
            });
        }

        private void SaveAndClearNodes()
        {
            if (currentTab != -1)
            {
                FSMInstance fsmInstance = tabs[currentTab];
                fsmInstance.matrix = mt.Matrix;
            }

            graphCanvas.Children.Clear();
            stateList.Children.Clear();
            eventList.Children.Clear();
            variableList.Children.Clear();
            nodes = new List<Node>();
        }

        public void LoadSavedNodes(int tab)
        {
            currentTab = tab;
            FSMInstance curInstance = tabs[currentTab];

            mt.Matrix = curInstance.matrix;

            nodes = curInstance.nodes;
            CopyListToChildren(stateList, curInstance.states);
            CopyListToChildren(eventList, curInstance.events);
            CopyListToChildren(variableList, curInstance.variables);
            CopyListToChildren(graphCanvas, curInstance.graphElements);
            dataVersion = curInstance.dataVersion;
        }

        private void SidebarData(Node node, AssetsFileInstance inst)
        {
            stateList.Children.Clear();

            AssetTypeValueField actionData = node.state.Get("actionData");
            int actionCount = actionData.Get("actionNames").GetValue().AsArray().size;
            string[] actionValues = ActionReader.ActionValues(actionData, inst, dataVersion);
            for (int i = 0; i < actionCount; i++)
            {
                string actionName = actionData.Get("actionNames").Get(i).GetValue().AsString();
                if (actionName.Contains("."))
                    actionName = actionName.Substring(actionName.LastIndexOf(".")+1);
                stateList.Children.Add(CreateSidebarHeader(actionName));
                int startParam = actionData.Get("actionStartIndex").Get(i).GetValue().AsInt();
                int endParam;
                if (i == actionCount - 1)
                {
                    endParam = actionData.Get("paramDataType").GetValue().AsArray().size;
                }
                else
                {
                    endParam = actionData.Get("actionStartIndex").Get(i + 1).GetValue().AsInt();
                }
                for (int j = startParam; j < endParam; j++)
                {
                    string paramName = actionData.Get("paramName").Get(j).GetValue().AsString();
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

        private List<UIElement> CopyChildrenToList(Panel ele)
        {
            UIElement[] tempEle;
            tempEle = new UIElement[ele.Children.Count];
            ele.Children.CopyTo(tempEle, 0);
            return tempEle.ToList();
        }

        private void CopyListToChildren(Panel ele, List<UIElement> children)
        {
            foreach (UIElement child in children)
            {
                ele.Children.Add(child);
            }
        }

        private string lastFilename;
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (am == null)
                CreateAssetManager();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                lastFilename = openFileDialog.FileName;
                openLast.IsEnabled = true;
                closeTab.IsEnabled = true;
                LoadFSMs(openFileDialog.FileName);
            }
        }

        private void OpenScene_Click(object sender, RoutedEventArgs e)
        {
            if (am == null)
                CreateAssetManager();

            string gameDataPath = GetGamePath();

            if (string.IsNullOrEmpty(gameDataPath))
                return;
            
            AssetsFileInstance inst = am.LoadAssetsFile(Path.Combine(gameDataPath, "globalgamemanagers"), false);
            AssetFileInfoEx buildSettings = inst.table.GetAssetInfo(11);

            List<string> scenes = new List<string>();
            AssetTypeValueField baseField = am.GetATI(inst.file, buildSettings).GetBaseField();
            AssetTypeValueField sceneArray = baseField.Get("scenes").Get("Array");
            for (int i = 0; i < sceneArray.GetValue().AsArray().size; i++)
            {
                scenes.Add(sceneArray[i].GetValue().AsString());
            }
            SceneSelector sel = new SceneSelector(scenes);
            sel.ShowDialog();
            if (sel.selectedFile != "")
            {
                int levelId = scenes.IndexOf(sel.selectedFile);
                string filePath = Path.Combine(gameDataPath, "level" + levelId);
                lastFilename = filePath;
                openLast.IsEnabled = true;
                closeTab.IsEnabled = true;
                LoadFSMs(filePath);
            }
        }
        
        private void OpenResources_Click(object sender, RoutedEventArgs e)
        {
            if (am == null)
                CreateAssetManager();

            string gameDataPath = GetGamePath();

            if (string.IsNullOrEmpty(gameDataPath)) return;

            string filePath = Path.Combine(gameDataPath, "resources.assets");
            lastFilename = filePath;
            openLast.IsEnabled = true;
            closeTab.IsEnabled = true;

            LoadFSMs(filePath);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            ignoreChangeEvent = true;
            SaveAndClearNodes();
            if (tabs.Count > 1)
            {
                int oldTab = currentTab;
                if (currentTab > 0)
                    currentTab--;
                tabs.RemoveAt(oldTab);
                LoadSavedNodes(currentTab);
                fsmTabControl.Items.Remove(fsmTabControl.SelectedItem);
                fsmTabControl.SelectedIndex = currentTab;
            }
            else
            {
                fsmTabControl.Items.Remove(fsmTabControl.SelectedItem);
                tabs.RemoveAt(currentTab);
                currentTab = -1;
                closeTab.IsEnabled = false;
            }
            ignoreChangeEvent = false;
        }

        private string GetGamePath()
        {
            string gamePath = "";
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    gamePath = fbd.SelectedPath;
                }
            }

            if (gamePath == "" || !Directory.Exists(gamePath))
            {
                MessageBox.Show("Could not find game path. If you've moved your game directory this could be why.");
                return null;
            }

            string gameDataPath = Path.Combine(gamePath, "hollow_knight_Data");

            return gameDataPath;
        }

        private void OpenLast_Click(object sender, RoutedEventArgs e)
        {
            LoadFSMs(lastFilename);
            closeTab.IsEnabled = true;
        }

        private void fsmTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreChangeEvent)
                return;
            SaveAndClearNodes();
            LoadSavedNodes(fsmTabControl.SelectedIndex);
        }

        private void CreateAssetManager()
        {
            am = new AssetsManager();
            am.updateAfterLoad = false;
            am.useTemplateFieldCache = true;
            am.LoadClassDatabase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cldb.dat"));
        }
    }
}
