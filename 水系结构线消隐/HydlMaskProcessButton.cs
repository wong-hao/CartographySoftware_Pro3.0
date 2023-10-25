using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Linq;

namespace SMGI_Plugin_EmergencyMap
{
    internal class HydlMaskProcessButton : Button
    {
        protected override async void OnClick()
        {
            var frmMask = new HYDLMaskingSetWPF();
            frmMask.ShowDialog();
                return;
            bool usingMask = frmMask.UsingMask;
            string maskingLyr = frmMask.MaskingLyr;
            string maskedLyr = frmMask.MaskedLyr;
            CommonMethods.UsingMask = usingMask;
            CommonMethods.MaskLayer = frmMask.MaskedLyr;
            CommonMethods.MaskedLayer = frmMask.MaskingLyr;
            FeatureClass HYDAfcl = null;
            FeatureClass HYDLfcl = null;
            GroupLayer groupLyr = null;

            #region
            //目前还未找到方法判断不是临时数据
            await QueuedTask.Run(() =>
            {
                var HYDLlyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(l => (l as FeatureLayer).Name == maskingLyr).FirstOrDefault() as FeatureLayer;

                var HYDAlyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(l => (l as FeatureLayer).Name == maskedLyr).FirstOrDefault() as FeatureLayer;

                var groups = MapView.Active.Map.GetLayersAsFlattenedList().OfType<GroupLayer>().Where(l => l is GroupLayer);

                if (HYDLlyr != null)
                {
                    HYDLfcl = HYDLlyr.GetFeatureClass();
                }
                else
                {
                    MessageBox.Show("未找到水系线图层!", "提示");
                    return;
                }
                if (HYDAlyr != null)
                {
                    HYDAfcl = HYDAlyr.GetFeatureClass();
                }
                else
                {
                    MessageBox.Show("未找到水系面图层!", "提示");
                    return;
                }
                #endregion
                #region
                GroupLayer groupLyr1 = null;
                GroupLayer groupLyr2 = null;
                foreach (var group in groups)
                {
                    CompositeLayer g = group as CompositeLayer;
                    for (int i = 0; i < g.Layers.Count; i++)
                    {
                        var l = g.Layers[i];
                        if (l is FeatureLayer)
                        {
                            if ((l as FeatureLayer).Name == maskingLyr)
                            {
                                groupLyr1 = g as GroupLayer;
                            }
                            if ((l as FeatureLayer).Name == maskedLyr)
                            {
                                groupLyr2 = g as GroupLayer;
                            }
                        }
                    }
                }
                if (groupLyr1.Equals(groupLyr2))
                {
                    groupLyr = groupLyr1;
                }
                else
                {
                    MessageBox.Show("不在同一个图层组!", "提示");
                    return;
                }
                #endregion

                //增加定义查询：不显示要素
                CIMFeatureTable fd = (HYDAlyr.GetDefinition() as CIMFeatureLayer).FeatureTable;
                string finitionExpression = fd.DefinitionExpression;
                if (!finitionExpression.ToLower().Contains(string.Format("ruleid <> {0}", 1)))
                {
                    if (finitionExpression != "")
                    {
                        fd.DefinitionExpression = string.Format("({0}) and (ruleid <> {1})", finitionExpression, 1);
                    }
                    else
                    {
                        fd.DefinitionExpression = string.Format("ruleid <> {0}", 1);
                    }
                }

                //图层掩膜方法 ArcGIS.Core.CIM.CIMBaselayer.LayerMasks
                #region Mask feature
                //Get the layer's definition
                var lyrDefn = HYDLlyr.GetDefinition();
                //Create an array of Masking layers (polygon only)
                //Set the LayerMasks property of the Masked layer
                lyrDefn.LayerMasks = new string[] { HYDAlyr.URI };
                //Re-set the Masked layer's defintion
                HYDLlyr.SetDefinition(lyrDefn);
                #endregion
            });
            MessageBox.Show("水系结构线消隐完成！");
        }

        /// <summary>
        /// 获取要素类
        /// </summary>
        /// <param name="pws"></param>
        /// <param name="fclName"></param>
        /// <returns></returns>
        public static FeatureClass GetFclViaWs(Geodatabase geodatabase, string fclName)
        {
            try
            {
                FeatureClass fcl = null;
                fcl = geodatabase.OpenDataset<FeatureClass>(fclName);
                return fcl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                System.Diagnostics.Trace.WriteLine(ex.Source);
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return null;
            }
        }
    }
}
