using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Data;
using System.Linq;
using SMGI_Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMGI_Plugin_EmergencyMap
{
    internal class HydlMaskProcessButton : Button
    {
        protected override async void OnClick()
        {

            #region
            //目前还未找到方法判断不是临时数据
            await QueuedTask.Run(() =>
            {
                // 创建TinDataset对象并建立关系
                TinDataset tinData = new TinDataset();

                // 模拟一个进度报告器
                IProgress<int> progress = new Progress<int>(percentage =>
                {
                    Debug.WriteLine($"Progress: {percentage}%");
                });

                // 建立关系并显示进度
                tinData.GetTinDatasetDefinition("CCC_TinTriangle", "CCC_TinEdge", "CCC_TinNode", progress);

                // 输出关系信息
                tinData.PrintTinDatasetDefinition();

                int nodeCount = tinData.GetNodeCount();
                GApplication.writeLog("一共有" + nodeCount + "个节点", GApplication.INFO, false);

                // 假设 node 是你要找相邻节点的特定节点对象
                TinNode node = tinData.GetNodeByIndex(145);
                GApplication.writeLog( "节点" + 145 + "一共有如下ID的相邻节点:", GApplication.INFO, false);

                if (node != null)
                {
                    List<TinNode> adjacentNodes = node.GetAdjacentNodes(tinData.Edges, tinData.Nodes);
                    // adjacentNodes 中包含了与特定节点相邻的其他节点
                    foreach (var adjacentNode in adjacentNodes)
                    {
                        GApplication.writeLog(adjacentNode.ID.ToString(), GApplication.INFO, false);
                    }
                }
                else
                {
                    // 处理未找到节点的情况
                }

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
