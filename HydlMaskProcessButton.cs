using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Mapping;
using NPOI.SS.Formula.Functions;
using SMGI_Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            await QueuedTask.Run(async () =>
            {
                //是否存在附区
                //Dictionary<string, string> envString = app.Workspace.MapConfig["EMEnvironment"] as Dictionary<string, string>;
                Dictionary<string, string> envString = new Dictionary<string, string>();
                if (envString == null || !envString.ContainsKey("AttachMap"))
                {
                    envString = EnvironmentSettings.GetConfigVal();
                }

                foreach (var kv in envString)
                {
                    GApplication.WriteLog($"Key: {kv.Key}, Value: {kv.Value}", GApplication.Info, false);
                }

                bool attachMap = false;
                if (envString.ContainsKey("AttachMap"))
                {
                    attachMap = bool.Parse(envString["AttachMap"]);

                }
                var FID2Point = TinClass.GetFid2PointFromFeatureClass("CCC_TinNode");
                await TinClass.CreateFeatureClassFromPoints(FID2Point, "AAA_FeatureClass");

                await TinClass.CreateTin("AAA_FeatureClass", "AAA");

                await TinClass.TinNode("AAA", "AAA_TinNode");
                await TinClass.TinEdge("AAA", "AAA_TinEdge");
                await TinClass.TinTriangle("AAA", "AAA_TinTriangle");

                TinDataset tinDataset = null;
                var tinDatasetFolderPath = Path.Combine(GApplication.Application.AppDataPath, @"TinDataset\");

                // 检查并创建 TIN 文件夹
                if (!Directory.Exists(tinDatasetFolderPath)) Directory.CreateDirectory(tinDatasetFolderPath);

                string jsonFilePath = Path.Combine(tinDatasetFolderPath, "AAA.json"); ;
                string xmlFilePath = Path.Combine(tinDatasetFolderPath, "AAA.xml");

                if (File.Exists(jsonFilePath))
                {
                    // 清空TinDataset
                    File.Delete(jsonFilePath);
                    File.Delete(xmlFilePath);

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

                    // 建立关系并保存为 JSON
                    tinDataset.GetTinDatasetDefinition("AAA_TinTriangle", "AAA_TinEdge", "AAA_TinNode");

                    tinDataset.SerializeToJSONFile(jsonFilePath);
                    tinDataset.SerializeToXMLFile(xmlFilePath);
                }

                //tinDataset.PrintTinDatasetDefinition();

                int nodeCount = tinDataset.GetNodeCount();
                GApplication.WriteLog("一共有" + nodeCount + "个节点", GApplication.Info, false);

                // 假设 node 是你要找相邻节点的特定节点对象
                TinNode node = tinDataset.GetNodeByIndex(33);
                GApplication.WriteLog("节点" + 33 + "一共有如下ID的相邻节点:", GApplication.Info, false);

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

                TinEdge edge = tinDataset.GetEdgeByIndex(45);
                if (edge != null)
                {
                    var triangle1 = edge.GetTriangleByEdge(tinDataset.Triangles);

                    TinEdge nextEdge = edge.GetNextEdgeInTriangle(triangle1, tinDataset.Edges);
                    GApplication.WriteLog($"Edge 45 nextEdgeID: {nextEdge.Index}", GApplication.Info, false);

                    TinEdge previousEdge = edge.GetPreviousEdgeInTriangle(triangle1, tinDataset.Edges);
                    GApplication.WriteLog($"Edge 45 previousEdgeID: {previousEdge.Index}", GApplication.Info, false);

                    GApplication.WriteLog($"Edge 45的横纵坐标为: {edge.ToPolyline(tinDataset).Points.First().X}, {edge.ToPolyline(tinDataset).Points.First().Y}, {edge.ToPolyline(tinDataset).Points.Last().X}, {edge.ToPolyline(tinDataset).Points.Last().Y}", GApplication.Info, false);
                }

                TinTriangle triangle = tinDataset.GetTriangleByIndex(22);
                if (triangle != null)
                {
                    List<TinTriangle> adjacentTriangles = triangle.GetAdjacentTriangles(tinDataset.Triangles);

                    // 打印与 triangle 相邻的其他三角形的 Index
                    foreach (var triangle2 in adjacentTriangles)
                    {
                        GApplication.WriteLog($"Adjacent Triangle Index to Triangle 22: {triangle2.Index}", GApplication.Info, false);
                    }
                }

                var tinDataArea = tinDataset.GetDataArea();
                double area = GeometryEngine.Instance.Area(tinDataArea);
                GApplication.WriteLog($"TinDataset Area: {area}", GApplication.Info, false);

                Dictionary<int, int> fid2Level = new Dictionary<int, int>();
                List<int> Levels = FID2Point.Keys.ToList();
                foreach (var kv in FID2Point)
                {
                    MapPoint p = kv.Value;

                    fid2Level.Add(kv.Key, p.ID);
                }


                for (int i = 1; i <= Levels.Count; i++)
                {
                    tinDataset.GetNodeByIndex(i).TagValue = Levels[i];
                }

                tinDataset.PrintTinDatasetDefinition();

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
        
        /*
        /// <summary>
        /// 顾及道路关系的POI选取
        /// </summary>
        /// <param name="fid2Point"></param>
        /// <param name="rate"></param>
        /// <param name="weightScale">水路交叉口处POI的权重比例</param>
        /// <param name="wo"></param>
        /// <returns></returns>
        public async Task<Dictionary<int, bool>> SelPOIByTinAsync(Dictionary<int, MapPoint> fid2Point, double rate, int weightScale, WaitOperation wo)
        {
            Dictionary<int, bool> result = new Dictionary<int, bool>();

            foreach (var kv in fid2Point)
            {
                result.Add(kv.Key, true);//初始化时，全部保留
            }
            int totalUnCount = (int)(fid2Point.Count * (1 - rate));
            int unSelCount = 0;

            //一次构tin最多去除的POI点数
            int maxSubCount = (int)(fid2Point.Count * 0.1);
            while (maxSubCount < 5 && maxSubCount < totalUnCount)
            {
                if (maxSubCount < 1)
                    maxSubCount = 1;

                maxSubCount = maxSubCount * 2;
            }
            int step = 1;
            while (unSelCount < totalUnCount)
            {
                int stepUnCount = maxSubCount * step;//本次Tin选取的最大点数

                //初始化tin
                TinClass.CreateFeatureClassFromPoints(fid2Point, "TinFc");

                await TinClass.CreateTin("TinFc", "Tin");
                await TinClass.TinNode("Tin", "TinNode");
                await TinClass.TinEdge("Tin", "TinEdge");
                await TinClass.TinTriangle("Tin", "TinTriangle");

                TinDataset tinDataset = null;

                tinDataset.GetTinDatasetDefinition("TinTriangle", "TinEdge", "TinNode");

                Dictionary<int, int> fid2Level = new Dictionary<int, int>();
                List<int> Levels = fid2Point.Keys.ToList();
                foreach (var kv in fid2Point)
                {
                    if (wo != null)
                        wo.SetText(string.Format("正在构建三角网......"));

                    if (!result[kv.Key])
                        continue;//已舍去，不参与本次构tin

                    MapPoint p = kv.Value;
                    p.Z = 0;

                    fid2Level.Add(kv.Key, p.ID);
                }

                if (tinDataset.Nodes.Count == Levels.Count)
                {
                    for (int i = 1; i <= Levels.Count - 1; i++)
                    {
                        tinDataset.GetNodeByIndex(i).TagValue = Levels[i];
                    }
                }

                //计算每个节点影响面积
                Dictionary<int, double> fid2Area = new Dictionary<int, double>();
                foreach (var kv in fid2Level)
                {
                    fid2Area.Add(kv.Key, 0);//初始化时，面积为0
                }
                Polygon tinDataArea = tinDataset.GetDataArea();//TIN的数据范围
                IRelationalOperator ro = tinDataArea as IRelationalOperator;
                for (int j = 1; j <= tin.NodeCount; j++)
                {
                    if (wo != null)
                        wo.SetText(string.Format("正在计算节点密度......"));

                    TinNode node = tinDataset.GetNodeByIndex(j);
                    if (!node.IsInsideDataArea(tinDataset))
                        continue;

                    Polygon voronoiPolygon = node.GetVoronoiRegion(null, tinDataset.Nodes, tinDataset);
                    double nodeArea = 0;//添加权重后的节点影响面积
                    if (!ro.Contains(voronoiPolygon))//节点影响范围部分超出了tin的数据范围，取两者相交的公共部分的面积
                    {
                        IPolygon interGeo = (tinDataArea as ITopologicalOperator).Intersect(voronoiPolygon as IGeometry, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;
                        if (interGeo != null)
                        {
                            nodeArea = (interGeo as IArea).Area;
                            if (fid2Level[node.TagValue] > 0)
                            {
                                nodeArea = nodeArea * (fid2Level[node.TagValue] * weightScale);
                            }
                        }
                    }
                    else
                    {
                        nodeArea = (voronoiPolygon as IArea).Area;
                        if (fid2Level[node.TagValue] > 0)
                        {
                            nodeArea = nodeArea * (fid2Level[node.TagValue] * weightScale);
                        }
                    }
                    fid2Area[node.TagValue] = nodeArea;
                }

                //按面积排序(升序)
                fid2Area = fid2Area.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);

                //设置较小影响面积的POI为false
                foreach (var kv in fid2Area)
                {
                    if (unSelCount >= totalUnCount)
                        break;//已选取完毕

                    if (unSelCount >= stepUnCount && (result.Count - stepUnCount) > 3)
                        break;//本次tin内选取完毕

                    result[kv.Key] = false;
                    unSelCount++;
                }

                ++step;//循环次数
            }

            return result;

        }
        */
    }
}
