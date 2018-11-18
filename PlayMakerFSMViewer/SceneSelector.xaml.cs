using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;

namespace PlayMakerFSMViewer
{
    /// <summary>
    /// Interaction logic for SceneSelector.xaml
    /// </summary>
    public partial class SceneSelector : Window
    {
        private List<string> scenes;
        private bool dontAllowSelect;
        public string selectedFile;
        private ICollectionView view;
        
        public SceneSelector(List<string> scenes)
        {
            this.scenes = scenes;
            selectedFile = "";
            InitializeComponent();
            SourceInitialized += (x, y) =>
            {
                this.HideMinimizeAndMaximizeButtons();
            };
            List<SceneListItem> fsms = new List<SceneListItem>();
            int index = 0;
            foreach (string info in this.scenes)
            {
                string name = info;
                fsms.Add(new SceneListItem(name, index));
                index++;
            }
            listBox.ItemsSource = fsms;
            view = CollectionViewSource.GetDefaultView(listBox.ItemsSource);
            view.Filter = Filter;
        }

        private void selectButton_Click(object sender, RoutedEventArgs e)
        {
            if (dontAllowSelect)
                return;
            dontAllowSelect = true;
            selectedFile = scenes[((SceneListItem)listBox.SelectedItem).index];
            Close();
        }
        
        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            view.Refresh();
        }

        private bool Filter(object obj)
        {
            return searchBox.Text == "" || searchBox.Text.ToUpper().Split(' ', '_').All(x => obj.ToString().ToUpper().Contains(x));
        }

        public struct SceneListItem
        {
            public string name;
            public int index;
            public SceneListItem(string name, int index)
            {
                this.name = name;
                this.index = index;
            }
            public override string ToString()
            {
                return name;
            }
        }
    }
}
