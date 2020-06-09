using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PlayMakerFSMViewer
{
    public class FSMInstance
    {
        public Matrix matrix;
        public List<UIElement> states;
        public List<UIElement> events;
        public List<UIElement> variables;
        public List<UIElement> graphElements;
        public List<Node> nodes;
        public int dataVersion;
    }
}
