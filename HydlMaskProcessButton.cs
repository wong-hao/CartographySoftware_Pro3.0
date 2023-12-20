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
using System.Windows.Forms.VisualStyles;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;

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
                TinDataset tinDataset = TinDataset.GetCachedTinDataset();

                // 模拟一个进度报告器
                IProgress<int> progress = new Progress<int>(percentage =>
                {
                    Debug.WriteLine($"Progress: {percentage}%");
                });

                if (tinDataset == null || tinDataset.Nodes.Count == 0)
                {
                    // 建立关系并显示进度
                    tinDataset.GetTinDatasetDefinition("CCC_TinTriangle", "CCC_TinEdge", "CCC_TinNode", progress);

                }

                // 输出关系信息
                tinDataset.PrintTinDatasetDefinition();

                int nodeCount = tinDataset.GetNodeCount();
                GApplication.writeLog("一共有" + nodeCount + "个节点", GApplication.INFO, false);

                // 假设 node 是你要找相邻节点的特定节点对象
                TinNode node = tinDataset.GetNodeByIndex(145);
                GApplication.writeLog( "节点" + 145 + "一共有如下ID的相邻节点:", GApplication.INFO, false);

                if (node != null)
                {
                    List<TinNode> adjacentNodes = node.GetAdjacentNodes(tinDataset.Edges, tinDataset.Nodes);
                    // adjacentNodes 中包含了与特定节点相邻的其他节点
                    foreach (var adjacentNode in adjacentNodes)
                    {
                        GApplication.writeLog(adjacentNode.Index.ToString(), GApplication.INFO, false);
                    }

                    List<TinEdge> incidentEdges = node.GetIncidentEdges(tinDataset.Edges);

                    // 打印出连接到节点 的边的 Index
                    foreach (var edge1 in incidentEdges)
                    {
                        GApplication.writeLog($"Edge Index connected to Node: {edge1.Index}", GApplication.INFO, false);
                    }

                    List<TinTriangle> incidentTriangles = node.GetIncidentTriangles(tinDataset.Triangles);

                    // 打印出与节点 相关的三角形的 Index
                    foreach (var triangle1 in incidentTriangles)
                    {
                        GApplication.writeLog($"Triangle Index connected to Node: {triangle1.Index}", GApplication.INFO, false);
                    }

                    GApplication.writeLog($"Node的横纵坐标为: {node.ToMapPoint(tinDataset).X}, {node.ToMapPoint(tinDataset).Y}", GApplication.INFO, false);
                }

                TinEdge edge = tinDataset.GetEdgeByIndex(75);
                if (edge != null)
                {
                    var triangle1 = edge.GetTriangleByEdge(tinDataset.Triangles);

                    TinEdge nextEdge = edge.GetNextEdgeInTriangle(triangle1, tinDataset.Edges);
                    GApplication.writeLog($"nextEdgeID: {nextEdge.Index}", GApplication.INFO, false);

                    TinEdge previousEdge = edge.GetPreviousEdgeInTriangle(triangle1, tinDataset.Edges);
                    GApplication.writeLog($"previousEdgeID: {previousEdge.Index}", GApplication.INFO, false);

                    GApplication.writeLog($"Edge的横纵坐标为: {edge.ToPolyline(tinDataset).Points.First().X}, {edge.ToPolyline(tinDataset).Points.First().Y}, {edge.ToPolyline(tinDataset).Points.Last().X}, {edge.ToPolyline(tinDataset).Points.Last().Y}", GApplication.INFO, false);

                    ;
                }

                TinTriangle triangle = tinDataset.GetTriangleByIndex(33);
                if (triangle != null)
                {
                    List<TinTriangle> adjacentTriangles = triangle.GetAdjacentTriangles(tinDataset.Edges, tinDataset.Triangles);

                    // 打印与 triangle 相邻的其他三角形的 Index
                    foreach (var triangle2 in adjacentTriangles)
                    {
                        GApplication.writeLog($"Adjacent Triangle Index to Triangle 33: {triangle2.Index}", GApplication.INFO, false);
                    }
                }

                var polygon = triangle.ToPolygon(tinDataset);

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
