using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using SMGI_Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

public class TinNode
{
    public int ID { get; }
    public List<int> ConnectedEdgeIDs { get; }

    public TinNode(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Node ID must be a positive integer.");
        }

        ID = id;
        ConnectedEdgeIDs = new List<int>();
    }

    public List<TinNode> GetAdjacentNodes(List<TinEdge> edges, List<TinNode> allNodes)
    {
        List<TinNode> adjacentNodes = new List<TinNode>();

        foreach (TinEdge edge in edges)
        {
            if (edge.ConnectedNodeIDs.Contains(this.ID))
            {
                foreach (int connectedNodeId in edge.ConnectedNodeIDs)
                {
                    if (connectedNodeId != this.ID)
                    {
                        TinNode adjacentNode = allNodes.FirstOrDefault(n => n.ID == connectedNodeId);
                        if (adjacentNode != null && !adjacentNodes.Contains(adjacentNode))
                        {
                            adjacentNodes.Add(adjacentNode);
                        }
                    }
                }
            }
        }

        return adjacentNodes;
    }
}

public class TinEdge
{
    public int ID { get; }
    public List<int> ConnectedNodeIDs { get; }

    public TinEdge(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Edge ID must be a positive integer.");
        }

        ID = id;
        ConnectedNodeIDs = new List<int>();
    }
}

public class TinTriangle
{
    public int ID { get; }
    public List<int> ConsistedEdgeIDs { get; }

    public TinTriangle(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Triangle ID must be a positive integer.");
        }

        ID = id;
        ConsistedEdgeIDs = new List<int>();
    }
}

public class TinDataset
{
    public List<TinNode> Nodes { get; }
    public List<TinEdge> Edges { get; }
    public List<TinTriangle> Triangles { get; }

    public TinDataset()
    {
        Nodes = new List<TinNode>();
        Edges = new List<TinEdge>();
        Triangles = new List<TinTriangle>();
    }

    public void GetTinDatasetDefinition(string triangleLyrName, string edgeLyrName, string nodeLyrName, IProgress<int> progress = null)
    {
        // 假设您已按名称获取了图层
        FeatureLayer triangleLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(layer => layer.Name == triangleLyrName);
        FeatureLayer edgeLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(layer => layer.Name == edgeLyrName);
        FeatureLayer nodeLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(layer => layer.Name == nodeLyrName);

        if (triangleLayer == null || edgeLayer == null || nodeLayer == null)
        {
            MessageBox.Show("未找到一个或多个指定图层！");
            return;
        }

        // 获取每个图层中的要素 ObjectIDs
        List<int> triangleOIDs = GetObjectIDs(triangleLayer);
        List<int> edgeOIDs = GetObjectIDs(edgeLayer);
        List<int> nodeOIDs = GetObjectIDs(nodeLayer);


        int totalProgress = edgeOIDs.Count * nodeOIDs.Count; // 计算总进度步数

        int currentProgress = 0;

        // 假设根据其 ObjectIDs 对特征进行排序
        foreach (int edgeOID in edgeOIDs)
        {
            Feature edgeFeature = GetFeatureByOID(edgeLayer, edgeOID);
            Polyline edgePolyline = edgeFeature.GetShape() as Polyline;

            foreach (int nodeOID in nodeOIDs)
            {
                Feature nodeFeature = GetFeatureByOID(nodeLayer, nodeOID);
                MapPoint nodePoint = nodeFeature.GetShape() as MapPoint;

                if (EdgeHasNodeAtEndpoints(edgePolyline, nodePoint))
                {
                    // 如果节点不存在，则创建 TinNode
                    if (!Nodes.Any(node => node.ID == nodeOID))
                    {
                        Nodes.Add(new TinNode(nodeOID));
                    }

                    // 如果边不存在，则创建 TinEdge
                    if (!Edges.Any(edge => edge.ID == edgeOID))
                    {
                        Edges.Add(new TinEdge(edgeOID));
                    }

                    // 建立节点和边之间的连接关系
                    TinNode currentNode = Nodes.First(node => node.ID == nodeOID);
                    TinEdge currentEdge = Edges.First(edge => edge.ID == edgeOID);

                    currentNode.ConnectedEdgeIDs.Add(edgeOID);
                    currentEdge.ConnectedNodeIDs.Add(nodeOID);
                }

                // 这里模拟进度
                currentProgress++;
                int percentage = (int)((double)currentProgress / totalProgress * 100);
                progress?.Report(percentage);
            }
        }

        // 可以在此处添加建立三角形和边之间关系的类似逻辑
        // 建立三角形和边之间的关系
        totalProgress = triangleOIDs.Count(); // 计算总进度步数

        currentProgress = 0;

        foreach (int triangleOID in triangleOIDs)
        {
            // 获取三角形要素
            Feature triangleFeature = GetFeatureByOID(triangleLayer, triangleOID);

            // 获取构成三角形的边要素
            List<Feature> edgeFeatures = GetEdgesForTriangle(triangleFeature, edgeLayer);

            // 新建一个 TinTriangle 对象表示当前的三角形
            TinTriangle currentTriangle = new TinTriangle(triangleOID);

            // 将每个边的 ID 添加到当前三角形的边列表中
            foreach (Feature edgeFeature in edgeFeatures)
            {
                int edgeOID = Convert.ToInt32(edgeFeature.GetObjectID());
                currentTriangle.ConsistedEdgeIDs.Add(edgeOID);
            }

            // 将当前三角形添加到 Triangles 列表中
            Triangles.Add(currentTriangle);

            // 更新进度
            currentProgress++;

            int percentage = (int)((double)currentProgress / totalProgress * 100);
            progress?.Report(percentage);
        }
    }

    private List<int> GetObjectIDs(FeatureLayer layer)
    {
        List<int> oids = new List<int>();

        using (RowCursor cursor = layer.Search())
        {
            while (cursor.MoveNext())
            {
                Feature feature = (Feature)cursor.Current;
                oids.Add(Convert.ToInt32(feature.GetObjectID()));
            }
        }

        return oids;
    }

    private Feature GetFeatureByOID(FeatureLayer layer, int oid)
    {
        string objectIdField = GetObjectIDField(layer);

        QueryFilter queryFilter = new QueryFilter();

        // 根据存在的字段设置查询条件
        if (!string.IsNullOrEmpty(objectIdField))
        {
            queryFilter.WhereClause = $"{objectIdField} = {oid}";
        }
        else
        {
            queryFilter.WhereClause = $"OID = {oid}"; // 或者您的特定字段
        }

        using (RowCursor rowCursor = layer.Search(queryFilter))
        {
            if (rowCursor.MoveNext())
            {
                return rowCursor.Current as Feature;
            }
        }

        return null;
    }

    private string GetObjectIDField(FeatureLayer layer)
    {
        // 获取图层的字段集合
        var fields = layer.GetTable().GetDefinition().GetFields();

        // 检查字段是否包含 OID 或 OBJECTID
        foreach (var field in fields)
        {
            if (field.FieldType == FieldType.OID || field.Name.ToUpper() == "OBJECTID")
            {
                return field.Name;
            }
        }

        return string.Empty;
    }


    private bool EdgeHasNodeAtEndpoints(Polyline edgePolyline, MapPoint nodePoint)
    {
        MapPoint edgeStartPoint = edgePolyline.Points.First();
        MapPoint edgeEndPoint = edgePolyline.Points.Last();

        // 检查节点是否与边的起点或终点完全重合
        bool isNodeAtStart = ArePointsEqual(edgeStartPoint, nodePoint);
        bool isNodeAtEnd = ArePointsEqual(edgeEndPoint, nodePoint);

        return isNodeAtStart || isNodeAtEnd;
    }

    private bool ArePointsEqual(MapPoint point1, MapPoint point2)
    {
        // 检查两点的 X 和 Y 坐标是否完全相等
        return Math.Abs(point1.X - point2.X) < double.Epsilon
               && Math.Abs(point1.Y - point2.Y) < double.Epsilon && Math.Abs(point1.Z - point2.Z) < double.Epsilon;
    }

    private List<Feature> GetEdgesForTriangle(Feature triangleFeature, FeatureLayer edgeLayer)
    {
        List<Feature> edgeFeatures = new List<Feature>();
        List<int> edgeOIDs = GetObjectIDs(edgeLayer);

        // 获取三角形的顶点集合
        Polygon trianglePolygon = triangleFeature.GetShape() as Polygon;
        var points = trianglePolygon.Points;
        MapPoint point1Shape = points[2];
        MapPoint point2Shape = points[1];
        MapPoint point3Shape = points[0];

        // 获取三角形的三条边
        Polyline edgeLine1 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point2Shape, point1Shape }); // 第一条边
        Polyline edgeLine2 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point3Shape, point2Shape }); // 第二条边
        Polyline edgeLine3 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point1Shape, point3Shape }); // 第三条边

        foreach (int edgeOID in edgeOIDs)
        {
            Feature edgeFeature = GetFeatureByOID(edgeLayer, edgeOID);
            Polyline edgeGeometry = edgeFeature.GetShape() as Polyline;

            // 检查三角形的每条边是否与当前图层中的要素相匹配
            if (GeometriesAreEqual(edgeLine1, edgeGeometry) || GeometriesAreEqual(edgeLine2, edgeGeometry) || GeometriesAreEqual(edgeLine3, edgeGeometry))
            {
                edgeFeatures.Add(edgeFeature);
            }
        }

        return edgeFeatures;
    }


    public static bool GeometriesAreEqual(Geometry geometry1, Geometry geometry2)
    {
        // Check if the geometries are Polyline
        if (geometry1 is Polyline && geometry2 is Polyline)
        {
            // Get identifiers for both geometries
            string identifier1 = GetPolylineIdentifier((Polyline)geometry1);
            string identifier2 = GetPolylineIdentifier((Polyline)geometry2);

            // Compare identifiers to check if the polylines have the same shape
            return identifier1 == identifier2;
        }

        return false;
    }

    public static string GetPolylineIdentifier(Polyline polyline)
    {
        // Sort points of the Polyline by their coordinates
        var sortedPoints = polyline.Points.OrderBy(p => p.X).ThenBy(p => p.Y).ThenBy(p => p.Z);

        // Get start and end points of the Polyline
        MapPoint startPoint = sortedPoints.First();
        MapPoint endPoint = sortedPoints.Last();

        // Get the hash value of the sorted points as a unique identifier
        return $"{startPoint.X},{startPoint.Y},{startPoint.Z},{endPoint.X},{endPoint.Y},{endPoint.Z}";
    }

    public void PrintTinDatasetDefinition()
    {        

        // 输出节点和其连接的边
        foreach (var node in Nodes)
        {
            GApplication.writeLog($"Node ID: {node.ID}", GApplication.INFO, false);
            GApplication.writeLog("Connected Edge IDs:", GApplication.INFO, false);
            foreach (var edgeID in node.ConnectedEdgeIDs)
            {
                GApplication.writeLog(edgeID.ToString(), GApplication.INFO, false);
            }
            GApplication.writeLog("------", GApplication.INFO, false);
        }

        // 输出边和其连接的节点
        foreach (var edge in Edges)
        {
            GApplication.writeLog($"Edge ID: {edge.ID}", GApplication.INFO, false);
            GApplication.writeLog("Connected Node IDs:", GApplication.INFO, false);
            foreach (var nodeID in edge.ConnectedNodeIDs)
            {
                GApplication.writeLog(nodeID.ToString(), GApplication.INFO, false);
            }
            GApplication.writeLog("------", GApplication.INFO, false);
        }

        // 输出三角形和其组成的边
        foreach (var triangle in Triangles)
        {
            GApplication.writeLog($"Triangle ID: {triangle.ID}", GApplication.INFO, false);
            GApplication.writeLog("Edge IDs:", GApplication.INFO, false);
            foreach (var edgeID in triangle.ConsistedEdgeIDs)
            {
                GApplication.writeLog(edgeID.ToString(), GApplication.INFO, false);
            }
            GApplication.writeLog("------", GApplication.INFO, false);
        }
    }

    public int GetNodeCount()
    {
        return Nodes.Count;
    }

    public TinNode GetNodeByIndex(int index)
    {
        if (index < 1 || index > Nodes.Count)
        {
            // 处理索引超出范围的情况，这里你可以选择抛出异常或返回 null
            throw new IndexOutOfRangeException("Index is out of range for Nodes list.");
            // 或者返回 null 或者其他适当的处理方式
            // return null;
        }

        return Nodes[index - 1];
    }



}


