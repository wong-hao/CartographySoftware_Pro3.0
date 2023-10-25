using SMGI_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace SMGI_Plugin_EmergencyMap
{
    /// <summary>
    /// FeatureSelectSuspensionWPF.xaml 的交互逻辑
    /// </summary>
    public partial class FeatureSelectSuspensionWPF : Window
    {
        public FeatureSelectSuspensionWPF()
        {
            InitializeComponent();
            GApplication app = new GApplication();
            app.loadDataLog(MethodBase.GetCurrentMethod().DeclaringType, Environment.CurrentDirectory);
            app.writeDataLog("要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取要素选取");
        }
    }
}
