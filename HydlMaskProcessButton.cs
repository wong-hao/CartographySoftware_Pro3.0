using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Mapping;
using SMGI_Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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
                    ruleDt = Helper.ReadGDBToDataTable(GApplication.Application.AppDataPath + @"\质检内容配置.gdb", "点线套合拓扑检查");//通用RuleID

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
                        GApplication.WriteLog(item + "\t", GApplication.Info, true);
                    }
                }

                TinDataset tinDataset = null;

                string jsonFilePath = GApplication.Application.AppDataPath + "//" + "tinDataset.json";
                string xmlFilePath = GApplication.Application.AppDataPath + "//" + "tinDataset.xml";

                if (File.Exists(jsonFilePath))
                {
                    TinDataset deserializedDataset = TinDataset.DeserializeFromJSONFile(jsonFilePath);

                    // 如果反序列化的结果不为空，则将其赋值给 tinDataset
                    if (deserializedDataset != null)
                    {
                        tinDataset = deserializedDataset;
                    }
                }

                // 如果 tinDataset 仍为空，执行建立关系和序列化的操作
                if (tinDataset == null || tinDataset.Nodes.Count == 0)
                {
                    tinDataset = new TinDataset();

                    // 模拟进度报告器
                    IProgress<int> progress = new Progress<int>(percentage =>
                    {
                        Debug.WriteLine($"Progress: {percentage}%");
                    });

                    // 建立关系并保存为 JSON
                    tinDataset.GetTinDatasetDefinition("CCC_TinTriangle", "CCC_TinEdge", "CCC_TinNode");

                    tinDataset.SerializeToJSONFile(jsonFilePath);
                    tinDataset.SerializeToXMLFile(xmlFilePath);
                }

                tinDataset.PrintTinDatasetDefinition();

                int nodeCount = tinDataset.GetNodeCount();
                GApplication.WriteLog("一共有" + nodeCount + "个节点", GApplication.Info, false);

                // 假设 node 是你要找相邻节点的特定节点对象
                TinNode node = tinDataset.GetNodeByIndex(145);
                GApplication.WriteLog("节点" + 145 + "一共有如下ID的相邻节点:", GApplication.Info, false);

                if (node != null)
                {
                    List<TinNode> adjacentNodes = node.GetAdjacentNodes(tinDataset.Edges, tinDataset.Nodes);
                    // adjacentNodes 中包含了与特定节点相邻的其他节点
                    foreach (var adjacentNode in adjacentNodes)
                    {
                        GApplication.WriteLog(adjacentNode.Index.ToString(), GApplication.Info, false);
                    }

                    List<TinEdge> incidentEdges = node.GetIncidentEdges(tinDataset.Edges);

                    // 打印出连接到节点 的边的 Index
                    foreach (var edge1 in incidentEdges)
                    {
                        GApplication.WriteLog($"Edge Index connected to Node: {edge1.Index}", GApplication.Info, false);
                    }

                    List<TinTriangle> incidentTriangles = node.GetIncidentTriangles(tinDataset.Triangles);

                    // 打印出与节点 相关的三角形的 Index
                    foreach (var triangle1 in incidentTriangles)
                    {
                        GApplication.WriteLog($"Triangle Index connected to Node: {triangle1.Index}", GApplication.Info, false);
                    }

                    GApplication.WriteLog($"Node的横纵坐标为: {node.ToMapPoint(tinDataset).X}, {node.ToMapPoint(tinDataset).Y}", GApplication.Info, false);
                }

                TinEdge edge = tinDataset.GetEdgeByIndex(75);
                if (edge != null)
                {
                    var triangle1 = edge.GetTriangleByEdge(tinDataset.Triangles);

                    TinEdge nextEdge = edge.GetNextEdgeInTriangle(triangle1, tinDataset.Edges);
                    GApplication.WriteLog($"nextEdgeID: {nextEdge.Index}", GApplication.Info, false);

                    TinEdge previousEdge = edge.GetPreviousEdgeInTriangle(triangle1, tinDataset.Edges);
                    GApplication.WriteLog($"previousEdgeID: {previousEdge.Index}", GApplication.Info, false);

                    GApplication.WriteLog($"Edge的横纵坐标为: {edge.ToPolyline(tinDataset).Points.First().X}, {edge.ToPolyline(tinDataset).Points.First().Y}, {edge.ToPolyline(tinDataset).Points.Last().X}, {edge.ToPolyline(tinDataset).Points.Last().Y}", GApplication.Info, false);

                    ;
                }

                TinTriangle triangle = tinDataset.GetTriangleByIndex(33);
                if (triangle != null)
                {
                    List<TinTriangle> adjacentTriangles = triangle.GetAdjacentTriangles(tinDataset.Triangles);

                    // 打印与 triangle 相邻的其他三角形的 Index
                    foreach (var triangle2 in adjacentTriangles)
                    {
                        GApplication.WriteLog($"Adjacent Triangle Index to Triangle 33: {triangle2.Index}", GApplication.Info, false);
                    }
                }

                var tinDataArea = tinDataset.GetDataArea();
                double area = GeometryEngine.Instance.Area(tinDataArea);
                GApplication.WriteLog($"TinDataset Area: {area}", GApplication.Info, true);

                EnvironmentSettings.UpdateEnvironmentToConfig();
                var envConfig = EnvironmentSettings.GetConfigValProject();
                foreach (var kv in envConfig)
                {
                    GApplication.WriteLog($"Key: {kv.Key}, Value: {kv.Value}", GApplication.Debug, true);
                }

                GApplication.WriteLog("\n", GApplication.Debug, true);

                //是否存在附区
                //Dictionary<string, string> envString = app.Workspace.MapConfig["EMEnvironment"] as Dictionary<string, string>;
                Dictionary<string, string> envString = new Dictionary<string, string>();
                if (envString == null || !envString.ContainsKey("AttachMap"))
                {
                    envString = EnvironmentSettings.GetConfigVal();
                }

                foreach (var kv in envString)
                {
                    GApplication.WriteLog($"Key: {kv.Key}, Value: {kv.Value}", GApplication.Info, true);
                }

                bool attachMap = false;
                if (envString.ContainsKey("AttachMap"))
                {
                    attachMap = bool.Parse(envString["AttachMap"]);

                }

                var FID2Point = Functions.GetFid2PointFromFeatureClass("CCC_TinNode");
                var cccFeatureClass = Functions.CreateFeatureClassFromPoints(FID2Point, "CCC_FeatureClass"); 

                Functions.CreateTin("CCC_FeatureClass", "CCC");

                Functions.TinNode("CCC", "CCC_TinNode");

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
