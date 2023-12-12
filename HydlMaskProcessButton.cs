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

                /*
                
                int triangleNum = TinTriangle.GetTriangleNum("CCC_TinTriangle");
                TinTriangle.TinTriangleTransition("CCC_TinTriangle", "CCC_TinNodesAll", "Node", true);
                TinTriangle.TinTriangleTransition("CCC_TinTriangle", "CCC_TinEdgesAll", "Edge", true);
                TinTriangle.TinTriangleTransition("CCC_TinTriangle", "CCC_TinNodesNotAll", "Node", false);
                TinTriangle.TinTriangleTransition("CCC_TinTriangle", "CCC_TinEdgesNotAll", "Edge", false);
                
                List<TinNode> nodes = new List<TinNode>();
                // 添加节点到 nodes 列表中

                List<TinEdge> edges = new List<TinEdge>();
                // 添加边到 edges 列表中

                List<TinTriangle> triangles = new List<TinTriangle>();
                // 添加三角形到 triangles 列表中

                TinDataset tinData = new TinDataset(nodes, edges, triangles);

                // 创建节点

                for (int i = 1; i <= 3 * triangleNum; i++)
                {
                    TinNode node = new TinNode(i);
                    nodes.Add(node);
                }

                // 创建边

                for (int i = 1; i <= 3 * triangleNum; i++)
                {
                    if (i % 3 == 1)
                    {
                        TinEdge edge = new TinEdge(i, i, i + 1, tinData);
                        edges.Add(edge);

                    }
                    else if (i % 3 == 2)
                    {
                        TinEdge edge = new TinEdge(i, i, i + 1, tinData);
                        edges.Add(edge);
                    }
                    else if (i % 3 == 0)
                    {
                        TinEdge edge = new TinEdge(i, i - 2, i, tinData);
                        edges.Add(edge);
                    }
                    else
                    {
                        break;
                    }
                }

                // 创建三角形

                for (int i = 1; i <= triangleNum; i++)
                {
                    TinTriangle triangle = new TinTriangle(i, 3 * i - 2, 3 * i - 1, 3 * i, tinData);
                    triangles.Add(triangle);
                }

                // 添加节点、边和三角形到数据结构中
                tinData.Nodes = nodes;
                tinData.Edges = edges;
                tinData.Triangles = triangles;

                // 输出节点、边和三角形的关联信息
                foreach (var node in tinData.Nodes)
                {
                    string connectedEdgesInfo = string.Join(", ", node.ConnectedEdgeIDs.Select(edgeID => edgeID.ToString()));
                    GApplication.writeLog(string.Format("Node ID: {0}, Connected Edge IDs: {1}", node.NodeID, connectedEdgesInfo), GApplication.DEBUG, false);
                }

                foreach (var edge in tinData.Edges)
                {
                    GApplication.writeLog(string.Format("Edge ID: {0}, Start Node ID: {1}, End Node ID: {2}", edge.EdgeID, edge.StartNodeID, edge.EndNodeID), GApplication.INFO, false);
                }

                foreach (var tri in tinData.Triangles)
                {
                    string edgesInfo = string.Join(", ", tri.EdgeIDs.Select(e => string.Format("Edge ID: {0}", e)));
                    GApplication.writeLog(string.Format("Triangle ID: {0}, Edges: {1}", tri.TriangleID, edgesInfo), GApplication.FATAL, false);
                }


                Tuple<int, int> sharededgeIDsTuple = tinData.GetSharedEdgeIDs(1, 2, tinData, "CCC_TinEdgesAll");
                GApplication.writeLog("Triangle 1与2 共享Edge" + sharededgeIDsTuple.Item1 + "与" + sharededgeIDsTuple.Item2, GApplication.FATAL, false);

*/

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
