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
            foreach (AssetInfo info in this.validAssets)
            {
                string name = $"{info.name} [{info.size}b]";
                listBox.Items.Add(name);
            }
        }

        private void selectButton_Click(object sender, RoutedEventArgs e)
        {
            if (dontAllowSelect)
                return;
            dontAllowSelect = true;
            selectedID = (long)validAssets[listBox.SelectedIndex].id;
            Close();
        }
    }
}
