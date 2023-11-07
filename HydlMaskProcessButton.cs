using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping.TOC;
using ArcGIS.Desktop.Mapping;
using SMGI_Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms.VisualStyles;

namespace SMGI_Plugin_EmergencyMap
{
    internal class HydlMaskProcessButton : Button
    {
        protected override async void OnClick()
        {
            HYDLMaskingSetForm frmMask = new HYDLMaskingSetForm();
            frmMask.ShowDialog();
             
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

                DataTable ruleDt = new DataTable();
                try
                {
                    ruleDt = Helper.ReadGDBToDataTable(GApplication.GetAppDataPath() + @"\质检内容配置.gdb", "线线套合拓扑检查");//通用RuleID

                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                    throw;
                }

                if (ruleDt.Rows.Count == 0)
                {
                    MessageBox.Show("质检内容配置表不存在或内容为空！");
                    return;
                }

                foreach (DataRow dataRow in ruleDt.Rows)
                {
                    foreach (var item in dataRow.ItemArray)
                    {
                        GApplication.writeLog(item + "\t", GApplication.INFO, true);
                    }
                }

                /*
                foreach (DataColumn column in ruleDt.Columns)
                {
                    MessageBox.Show(column.ColumnName + "\t");
                    foreach (DataRow row in ruleDt.Rows)
                    {
                        MessageBox.Show(row[column] + "\t");
                    }
                    Console.WriteLine(); // 换行
                }*/

                // 创建TinDataset对象
                TinDataset tinData = new TinDataset();

                // 创建节点
                TinNode node1 = new TinNode(1);
                TinNode node2 = new TinNode(2);
                TinNode node3 = new TinNode(3);
                TinNode node4 = new TinNode(4);
                TinNode node5 = new TinNode(5);

                // 创建边
                TinEdge edge1 = new TinEdge(1, node1, node2);
                TinEdge edge2 = new TinEdge(2, node2, node3);
                TinEdge edge3 = new TinEdge(3, node1, node3);
                TinEdge edge4 = new TinEdge(4, node1, node4);
                TinEdge edge5 = new TinEdge(5, node2, node4);
                TinEdge edge6 = new TinEdge(6, node4, node5);

                // 创建三角形
                TinTriangle triangle1 = new TinTriangle(1, edge1, edge2, edge3);
                TinTriangle triangle2 = new TinTriangle(2, edge1, edge4, edge5);
                TinTriangle triangle3 = new TinTriangle(3, edge3, edge4, edge6);


                // 添加节点、边和三角形到数据结构中
                tinData.Nodes.Add(node1);
                tinData.Nodes.Add(node2);
                tinData.Nodes.Add(node3);
                tinData.Nodes.Add(node4);
                tinData.Nodes.Add(node5);

                tinData.Edges.Add(edge1);
                tinData.Edges.Add(edge2);
                tinData.Edges.Add(edge3);
                tinData.Edges.Add(edge4);
                tinData.Edges.Add(edge5);
                tinData.Edges.Add(edge6);
                
                tinData.Triangles.Add(triangle1);
                tinData.Triangles.Add(triangle2);
                tinData.Triangles.Add(triangle3);
                
                MessageBox.Show(tinData.GetSharedEdgeID(2, 3, tinData.Triangles).ToString());

                foreach (var nodeID in tinData.GetSharedNodeIDs(2, 3, tinData.Triangles, tinData.Edges))
                {
                    MessageBox.Show(nodeID.ToString());
                }

                List<int> sharedTriangleIDs = tinData.GetTrianglesSharingEdge(2, tinData.Triangles);
                MessageBox.Show($"三角形{2}与以下三角形共享边:");
                foreach (int sharedTriangleID in sharedTriangleIDs)
                {
                    MessageBox.Show(sharedTriangleID.ToString());

                }

                // 输出节点、边和三角形的关联信息
                foreach (var node in tinData.Nodes)
                {
                    GApplication.writeLog(string.Format("Node ID: {0}, Connected Edges: {1}", node.NodeID, string.Join(", ", node.ConnectedEdges.Select(e => e.EdgeID))), GApplication.DEBUG, false);
                }

                foreach (var edge in tinData.Edges)
                {
                    GApplication.writeLog(string.Format("Edge ID: {0}, Start Node: {1}, End Node: {2}", edge.EdgeID, edge.StartNode.NodeID, edge.EndNode.NodeID), GApplication.INFO, false);
                }

                foreach (var tri in tinData.Triangles)
                {
                    string edgesInfo = string.Join(", ", tri.Edges.Select(e => string.Format("Edge ID: {0}, Start Node: {1}, End Node: {2}", e.EdgeID, e.StartNode.NodeID, e.EndNode.NodeID)));
                    GApplication.writeLog(string.Format("Triangle ID: {0}, Edges: {1}", tri.TriangleID, edgesInfo), GApplication.FATAL, false);
                }

                int triangleID = 2; // 你要查询的三角形ID

                int targetEdgeID = 6;
                int[] nodeIDs = TinDataset.GetNodeIDsFromEdgeID(targetEdgeID, tinData.Edges);

                // 打印节点ID数组中的值
                foreach (int nodeID in nodeIDs)
                {
                    MessageBox.Show("Node ID: " + nodeID);
                }

                // 假设你有一个包含目标节点 IDs 和边 IDs 的数组
                int[] targetEdgeIDs = { 4, 1, 5 }; // 目标边 IDs

                // 使用 GetTriangleFromNodeAndEdgeIDs 方法获取匹配的三角形
                TinTriangle targetTriangle = TinDataset.GetTriangleFromEdgeIDs(targetEdgeIDs, tinData.Triangles);

                if (targetTriangle != null)
                {
                    MessageBox.Show($"找到了三角形，三角形 ID 为: {targetTriangle.TriangleID}");

                    // 如果你需要访问三角形的边，可以通过 targetTriangle.Edges 进行访问
                    foreach (var edge in targetTriangle.Edges)
                    {
                        MessageBox.Show($"边 ID: {edge.EdgeID}, 起始节点 ID: {edge.StartNode.NodeID}, 结束节点 ID: {edge.EndNode.NodeID}");
                    }
                }
                else
                {
                    MessageBox.Show("未找到匹配的三角形。");
                }

                var Trianglelyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(l => (l as FeatureLayer).Name == "CCC_TinTriangle").FirstOrDefault() as FeatureLayer;
                if (Trianglelyr == null)
                {
                    MessageBox.Show("未找到CCC_TinTriangle图层!", "提示");
                }

                if (Trianglelyr != null)
                {
                    // 获取Trianglelyr图层的要素类
                    var featureClass = Trianglelyr.GetTable() as FeatureClass;

                    // 遍历三角形要素
                    using (var cursor = featureClass.Search(null, true))
                    {
                        while (cursor.MoveNext())
                        {
                            var feature = cursor.Current as Feature;

                            // 获取三角形的几何形状（多边形）
                            var polygon = feature.GetShape() as Polygon;

                            // 获取多边形的边
                            var rings = polygon.Parts;
                            foreach (var ring in rings)
                            {
                                // 遍历多边形的每一条边
                                for (int i = 0; i < ring.Count - 1; i++)
                                {
                                    var startPoint = ring[i];
                                    var endPoint = ring[i + 1];

                                    // 在这里处理每一条边，可以将起点和终点坐标保存下来，或者进行其他操作
                                    // startPoint 和 endPoint 是 IPoint 接口的实例，可以获取坐标信息
                                }
                            }

                            // 获取多边形的顶点
                            var points = polygon.Points;
                            foreach (var point in points)
                            {
                                // 在这里处理每一个顶点，可以获取其坐标信息
                                // point 是 IPoint 接口的实例，可以获取坐标信息
                                Console.WriteLine($"Point: X={point.X}, Y={point.Y}");

                            }
                        }
                    }
                }


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
