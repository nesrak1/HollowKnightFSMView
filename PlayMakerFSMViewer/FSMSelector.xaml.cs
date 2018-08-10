using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for FSMSelector.xaml
    /// </summary>
    public partial class FSMSelector : Window
    {
        private List<AssetInfo> validAssets;
        private bool dontAllowSelect = false;
        private ICollectionView view;
        public long selectedID;
        public FSMSelector(List<AssetInfo> validAssets)
        {
            this.validAssets = validAssets;
            selectedID = -1;
            InitializeComponent();
            SourceInitialized += (x, y) =>
            {
                this.HideMinimizeAndMaximizeButtons();
            };
            List<FSMListItem> fsms = new List<FSMListItem>();
            int index = 0;
            foreach (AssetInfo info in this.validAssets)
            {
                string name = $"{info.name} [{info.size}b]";
                fsms.Add(new FSMListItem(name, index));
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
            selectedID = (long)validAssets[((FSMListItem)listBox.SelectedItem).index].id;
            Close();
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            view.Refresh();
        }

        private bool Filter(object obj)
        {
            return searchBox.Text == "" || searchBox.Text.ToUpper().All(x => obj.ToString().ToUpper().Contains(x));
        }

        public struct FSMListItem
        {
            public string name;
            public int index;
            public FSMListItem(string name, int index)
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
