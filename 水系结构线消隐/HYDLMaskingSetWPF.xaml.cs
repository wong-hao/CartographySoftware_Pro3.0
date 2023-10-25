using ArcGIS.Desktop.Internal.GeoProcessing;
using ArcGIS.Desktop.Mapping;
using log4net.Config;
using log4net;
using SMGI_Common;
using System;
using System.Collections.Generic;
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
using System.Xml.Linq;
using System.Reflection;

namespace SMGI_Plugin_EmergencyMap
{
    /// <summary>
    /// HYDLMaskingSetWPF.xaml 的交互逻辑
    /// </summary>
    public partial class HYDLMaskingSetWPF : Window
    {
        public string MaskingLyr = "";
        public string MaskedLyr = "";
        public bool UsingMask = true;
        public HYDLMaskingSetWPF()
        {
            InitializeComponent();
        }

        private void SureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!UsingMask)
            {
                DialogResult = DialogResult.Value;
            }
            if (MaskLyrsListBox.Items.Count == 0 || MaskedLyrsListBox.Items.Count == 0)
            {
                MessageBox.Show("请选择图层", "提示");
                return;
            }

            MaskedLyr = MaskLyrsListBox.Items[0].ToString();
            MaskingLyr = MaskedLyrsListBox.Items[0].ToString();
            DialogResult = DialogResult.Value;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void HYDLMaskingSetWPF_Loaded(object sender, RoutedEventArgs e)
        {
            GApplication app = new GApplication();
            app.loadSysLog();
            app.writeSyslog("系统启动");

            /*
        var fileName = EnvironmentSettings.GetCaptionPath() + @"\专家库\消隐\水系消隐.xml";
        string hyda = "HYDA";
        string hydl = "HYDL";
        if (File.Exists(fileName))
        {
            XDocument doc = XDocument.Load(fileName);
            var content = doc.Element("Template").Element("Content");
            var mask = content.Element("MakedLayer");
            if (mask != null)
                hyda = mask.Value;
            var masked = content.Element("MakingLayer");
            if (masked != null)
                hydl = masked.Value;
        }
        // 未作该判断(x as IFeatureLayer).FeatureClass as IDataset).Workspace.PathName == m_Application.Workspace.EsriWorkspace.PathName
        FeatureLayer[] lyrs = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(f => f.Name == GApplication.LayersMappingENtoCH[hyda]).ToArray();
        for (int i = 0; i < lyrs.Length; i++)
        {
            var lyr = lyrs[i];
            var MaskLyrsBox = new CheckBox
            {
                Content = lyr.Name,
            };
            MaskLyrsListBox.Items.Add(MaskLyrsBox);
            if (lyr.Name == CommonMethods.MaskLayer)
            {
                var objectMask = MaskLyrsListBox.Items[i];
                (objectMask as CheckBox).IsChecked = true;
            }
        }

        lyrs = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(f => f.Name == GApplication.LayersMappingENtoCH[hydl]).ToArray();
        for (int i = 0; i < lyrs.Length; i++)
        {
            var lyr = lyrs[i];
            var MaskedLyrsBox = new CheckBox
            {
                Content = lyr.Name,
            };
            MaskedLyrsListBox.Items.Add(MaskedLyrsBox);
            if (lyr.Name == CommonMethods.MaskedLayer)
            {
                var objectMask = MaskedLyrsListBox.Items[i];
                (objectMask as CheckBox).IsChecked = true;
            }
        }
        MaskCheckBox.IsChecked = CommonMethods.UsingMask;
        */
        }

        private void MaskCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MaskLyrsListBox.IsEnabled = MaskCheckBox.IsChecked.Value;
            MaskedLyrsListBox.IsEnabled = MaskCheckBox.IsChecked.Value;
            UsingMask = MaskCheckBox.IsChecked.Value;
        }
    }
}
