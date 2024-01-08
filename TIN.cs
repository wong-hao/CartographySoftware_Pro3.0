using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Mapping;
using Newtonsoft.Json;
using SMGI_Common;
using FieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using Formatting = Newtonsoft.Json.Formatting;

public class TinNode
{
    public TinNode(int Index)
    {
        if (Index <= 0) throw new ArgumentException("Node Index must be a positive integer.");

        this.Index = Index;
        ConnectedEdgeIndexes = new List<int>();
    }

    public int Index { get; }
    public List<int> ConnectedEdgeIndexes { get; }

    public MapPoint ToMapPoint(TinDataset tinDataset)
    {
        // 获取包含节点信息的图层
        var nodeLayer = MapView.Active.Map.GetLayersAsFlattenedList()
            .OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == tinDataset.nodeLyrName);

        var nodeIndex = Index; // 假设节点对象有一个属性叫做 Index，表示节点的 Index

        // 使用 GetFeatureByOID 方法获取节点要素
        var nodeFeature = tinDataset.GetFeatureByOID(nodeLayer, nodeIndex);

        if (nodeFeature != null)
        {
            // 提取节点要素中的点位置信息
            var nodePoint = nodeFeature.GetShape() as MapPoint;
            return nodePoint;
        }

        // 如果未找到对应 nodeIndex 的节点，返回 null 或者其他适当的值
        return null;
    }

    public bool IsInsideDataArea(double minX, double minY, double maxX, double maxY, TinDataset tinDataset)
    {
        var nodePoint = ToMapPoint(tinDataset); // 获取节点的地图点

        // 检查节点的 X 和 Y 坐标是否在数据区域内
        return nodePoint.X >= minX && nodePoint.X <= maxX && nodePoint.Y >= minY && nodePoint.Y <= maxY;
    }

    public List<TinNode> GetAdjacentNodes(List<TinEdge> allEdges, List<TinNode> allNodes)
    {
        var adjacentNodes = allEdges
            .Where(edge => edge.ConnectedNodeIndexes.Contains(this.Index))
            .SelectMany(edge => edge.ConnectedNodeIndexes.Where(Index => Index != this.Index))
            .Distinct()
            .Select(Index => allNodes.FirstOrDefault(node => node.Index == Index))
            .Where(node => node != null)
            .ToList();

        return adjacentNodes;
    }

    public List<TinEdge> GetIncidentEdges(List<TinEdge> allEdges)
    {
        // 根据连接的节点筛选边，其中该节点是“起始”节点或“结束”节点
        return allEdges.Where(edge => edge.ConnectedNodeIndexes.Contains(Index)).ToList();
    }

    public List<TinTriangle> GetIncidentTriangles(List<TinTriangle> allTriangles)
    {
        // 根据由连接边组成的三角形筛选三角形，其中该节点的连接边是三角形的一部分
        return allTriangles.Where(triangle =>
            triangle.ConsistedEdgeIndexes.Any(edgeIndex => ConnectedEdgeIndexes.Contains(edgeIndex))).ToList();
    }
}

public class TinEdge
{
    public TinEdge(int Index)
    {
        if (Index <= 0) throw new ArgumentException("Edge Index must be a positive integer.");

        this.Index = Index;
        ConnectedNodeIndexes = new List<int>();
    }

    public int Index { get; }
    public List<int> ConnectedNodeIndexes { get; }

    public Polyline ToPolyline(TinDataset tinDataset)
    {
        // 获取包含边信息的图层
        var edgeLayer = MapView.Active.Map.GetLayersAsFlattenedList()
            .OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == tinDataset.edgeLyrName);

        var edgeIndex = Index; // 假设边对象有一个属性叫做 Index，表示边的 Index

        // 使用 GetFeatureByOID 方法获取边要素
        var edgeFeature = tinDataset.GetFeatureByOID(edgeLayer, edgeIndex);

        if (edgeFeature != null)
        {
            // 提取边要素中的 Polyline 信息
            var edgePolyline = edgeFeature.GetShape() as Polyline;
            return edgePolyline;
        }

        // 如果未找到对应 edgeIndex 的边，返回 null 或者其他适当的值
        return null;
    }


    public TinEdge GetNextEdgeInTriangle(TinTriangle triangle, List<TinEdge> allEdges)
    {
        // 获取组成三角形的所有边
        var triangleEdges = allEdges.Where(edge => triangle.ConsistedEdgeIndexes.Contains(edge.Index)).ToList();

        // 找到当前边在三角形中的索引
        var currentIndex = triangleEdges.FindIndex(edge => edge.Index == Index);

        if (currentIndex != -1)
        {
            // 获取下一个边的索引（按顺时针方向）
            var nextIndex = (currentIndex + 1) % triangleEdges.Count;

            // 返回下一个边
            return triangleEdges[nextIndex];
        }

        // 如果未找到当前边在三角形中的索引，返回 null 或者采取其他适当的操作
        return null;
    }

    public TinEdge GetPreviousEdgeInTriangle(TinTriangle triangle, List<TinEdge> allEdges)
    {
        // 获取组成三角形的所有边
        var triangleEdges = allEdges.Where(edge => triangle.ConsistedEdgeIndexes.Contains(edge.Index)).ToList();

        // 找到当前边在三角形中的索引
        var currentIndex = triangleEdges.FindIndex(edge => edge.Index == Index);

        if (currentIndex != -1)
        {
            // 获取前一个边的索引（按逆时针方向）
            var previousIndex = (currentIndex - 1 + triangleEdges.Count) % triangleEdges.Count;

            // 返回前一个边
            return triangleEdges[previousIndex];
        }

        // 如果未找到当前边在三角形中的索引，返回 null 或者采取其他适当的操作
        return null;
    }

    public TinTriangle GetTriangleByEdge(List<TinTriangle> allTriangles)
    {
        // 查找包含当前边的三角形
        return allTriangles.FirstOrDefault(triangle => triangle.ConsistedEdgeIndexes.Contains(Index));
    }
}

public class TinTriangle
{
    public TinTriangle(int Index)
    {
        if (Index <= 0) throw new ArgumentException("Triangle Index must be a positive integer.");

        this.Index = Index;
        ConsistedEdgeIndexes = new List<int>();
    }

    public int Index { get; }
    public List<int> ConsistedEdgeIndexes { get; }

    public Polygon ToPolygon(TinDataset tinDataset)
    {
        // 获取包含三角形信息的图层
        var triangleLayer = MapView.Active.Map.GetLayersAsFlattenedList()
            .OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == tinDataset.triangleLyrName);

        var triangleIndex = Index; // 假设三角形对象有一个属性叫做 Index，表示三角形的 Index

        // 使用 GetFeatureByOID 方法获取三角形要素
        var triangleFeature = tinDataset.GetFeatureByOID(triangleLayer, triangleIndex);

        if (triangleFeature != null)
        {
            // 提取三角形要素中的 Polygon 信息
            var trianglePolygon = triangleFeature.GetShape() as Polygon;
            return trianglePolygon;
        }

        // 如果未找到对应 triangleIndex 的三角形，返回 null 或者其他适当的值
        return null;
    }


    public List<TinTriangle> GetAdjacentTriangles(List<TinTriangle> allTriangles)
    {
        var adjacentTriangles = allTriangles
            .Where(triangle =>
                triangle != this &&
                triangle.ConsistedEdgeIndexes.Any(edgeIndex => ConsistedEdgeIndexes.Contains(edgeIndex)))
            .ToList();

        return adjacentTriangles;
    }
}

public class TinDataset
{
    public TinDataset()
    {
        Nodes = new List<TinNode>();
        Edges = new List<TinEdge>();
        Triangles = new List<TinTriangle>();
    }

    public List<TinNode> Nodes { get; }
    public List<TinEdge> Edges { get; }
    public List<TinTriangle> Triangles { get; }

    public string triangleLyrName { get; set; }
    public string edgeLyrName { get; set; }
    public string nodeLyrName { get; set; }

    public void GetTinDatasetDefinition(string tempTriangleLyrName, string tempEdgeLyrName, string tempNodeLyrName)
    {
        // 假设您已按名称获取了图层
        triangleLyrName = tempTriangleLyrName;
        edgeLyrName = tempEdgeLyrName;
        nodeLyrName = tempNodeLyrName;

        var triangleLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == triangleLyrName);
        var edgeLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == edgeLyrName);
        var nodeLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == nodeLyrName);

        if (triangleLayer == null || edgeLayer == null || nodeLayer == null)
        {
            MessageBox.Show("未找到一个或多个指定图层！");
            return;
        }

        // 获取每个图层中的要素 ObjectIDs
        var triangleOIDs = GetObjectIDs(triangleLayer);
        var edgeOIDs = GetObjectIDs(edgeLayer);
        var nodeOIDs = GetObjectIDs(nodeLayer);


        // 假设根据其 ObjectIDs 对特征进行排序
        foreach (var edgeOID in edgeOIDs)
        {
            var edgeFeature = GetFeatureByOID(edgeLayer, edgeOID);
            var edgePolyline = edgeFeature.GetShape() as Polyline;

            foreach (var nodeOID in nodeOIDs)
            {
                var nodeFeature = GetFeatureByOID(nodeLayer, nodeOID);
                var nodePoint = nodeFeature.GetShape() as MapPoint;

                if (EdgeHasNodeAtEndpoints(edgePolyline, nodePoint))
                {
                    // 如果节点不存在，则创建 TinNode
                    if (!Nodes.Any(node => node.Index == nodeOID)) Nodes.Add(new TinNode(nodeOID));

                    // 如果边不存在，则创建 TinEdge
                    if (!Edges.Any(edge => edge.Index == edgeOID)) Edges.Add(new TinEdge(edgeOID));

                    // 建立节点和边之间的连接关系
                    var currentNode = Nodes.First(node => node.Index == nodeOID);
                    var currentEdge = Edges.First(edge => edge.Index == edgeOID);

                    currentNode.ConnectedEdgeIndexes.Add(edgeOID);
                    currentEdge.ConnectedNodeIndexes.Add(nodeOID);
                }
            }
        }

        // 可以在此处添加建立三角形和边之间关系的类似逻辑
        // 建立三角形和边之间的关系
        foreach (var triangleOID in triangleOIDs)
        {
            // 获取三角形要素
            var triangleFeature = GetFeatureByOID(triangleLayer, triangleOID);

            // 获取构成三角形的边要素
            var edgeFeatures = GetEdgesForTriangle(triangleFeature, edgeLayer);

            // 新建一个 TinTriangle 对象表示当前的三角形
            var currentTriangle = new TinTriangle(triangleOID);

            // 将每个边的 Index 添加到当前三角形的边列表中
            foreach (var edgeFeature in edgeFeatures)
            {
                var edgeOID = Convert.ToInt32(edgeFeature.GetObjectID());
                currentTriangle.ConsistedEdgeIndexes.Add(edgeOID);
            }

            // 将当前三角形添加到 Triangles 列表中
            Triangles.Add(currentTriangle);
        }
    }

    public List<int> GetObjectIDs(FeatureLayer layer)
    {
        var oids = new List<int>();

        using (var cursor = layer.Search())
        {
            while (cursor.MoveNext())
            {
                var feature = (Feature)cursor.Current;
                oids.Add(Convert.ToInt32(feature.GetObjectID()));
            }
        }

        return oids;
    }

    public Feature GetFeatureByOID(FeatureLayer layer, int oid)
    {
        var objectIdField = GetObjectIDField(layer);

        var queryFilter = new QueryFilter();

        // 根据存在的字段设置查询条件
        if (!string.IsNullOrEmpty(objectIdField))
            queryFilter.WhereClause = $"{objectIdField} = {oid}";
        else
            queryFilter.WhereClause = $"OID = {oid}"; // 或者您的特定字段

        using var rowCursor = layer.Search(queryFilter);
        if (rowCursor.MoveNext()) return rowCursor.Current as Feature;

        return null;
    }

    private string GetObjectIDField(FeatureLayer layer)
    {
        // 获取图层的字段集合
        var fields = layer.GetTable().GetDefinition().GetFields();

        // 检查字段是否包含 OID 或 OBJECTID
        foreach (var field in fields)
            if (field.FieldType == FieldType.OID || field.Name.ToUpper() == "OBJECTID")
                return field.Name;

        return string.Empty;
    }

    private bool EdgeHasNodeAtEndpoints(Polyline edgePolyline, MapPoint nodePoint)
    {
        var edgeStartPoint = edgePolyline.Points.First();
        var edgeEndPoint = edgePolyline.Points.Last();

        // 检查节点是否与边的起点或终点完全重合
        var isNodeAtStart = ArePointsEqual(edgeStartPoint, nodePoint);
        var isNodeAtEnd = ArePointsEqual(edgeEndPoint, nodePoint);

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
        var edgeFeatures = new List<Feature>();
        var edgeOIDs = GetObjectIDs(edgeLayer);

        // 获取三角形的顶点集合
        var trianglePolygon = triangleFeature.GetShape() as Polygon;
        var points = trianglePolygon.Points;
        var point1Shape = points[2];
        var point2Shape = points[1];
        var point3Shape = points[0];

        // 获取三角形的三条边
        var edgeLine1 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point2Shape, point1Shape }); // 第一条边
        var edgeLine2 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point3Shape, point2Shape }); // 第二条边
        var edgeLine3 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point1Shape, point3Shape }); // 第三条边

        foreach (var edgeOID in edgeOIDs)
        {
            var edgeFeature = GetFeatureByOID(edgeLayer, edgeOID);
            var edgeGeometry = edgeFeature.GetShape() as Polyline;

            // 检查三角形的每条边是否与当前图层中的要素相匹配
            if (GeometriesAreEqual(edgeLine1, edgeGeometry) || GeometriesAreEqual(edgeLine2, edgeGeometry) ||
                GeometriesAreEqual(edgeLine3, edgeGeometry)) edgeFeatures.Add(edgeFeature);
        }

        return edgeFeatures;
    }

    public static bool GeometriesAreEqual(Geometry geometry1, Geometry geometry2)
    {
        // Check if the geometries are Polyline
        if (geometry1 is Polyline && geometry2 is Polyline)
        {
            // Get identifiers for both geometries
            var identifier1 = GetPolylineIdentifier((Polyline)geometry1);
            var identifier2 = GetPolylineIdentifier((Polyline)geometry2);

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
        var startPoint = sortedPoints.First();
        var endPoint = sortedPoints.Last();

        // Get the hash value of the sorted points as a unique identifier
        return $"{startPoint.X},{startPoint.Y},{startPoint.Z},{endPoint.X},{endPoint.Y},{endPoint.Z}";
    }

    public void PrintTinDatasetDefinition()
    {
        // 输出节点和其连接的边
        foreach (var node in Nodes)
        {
            GApplication.WriteLog($"Node Index: {node.Index}", GApplication.Info, false);
            GApplication.WriteLog("Connected Edge IDs:", GApplication.Info, false);
            foreach (var edgeIndex in node.ConnectedEdgeIndexes)
                GApplication.WriteLog(edgeIndex.ToString(), GApplication.Info, false);
            GApplication.WriteLog("------", GApplication.Info, false);
        }

        // 输出边和其连接的节点
        foreach (var edge in Edges)
        {
            GApplication.WriteLog($"Edge Index: {edge.Index}", GApplication.Info, false);
            GApplication.WriteLog("Connected Node IDs:", GApplication.Info, false);
            foreach (var nodeIndex in edge.ConnectedNodeIndexes)
                GApplication.WriteLog(nodeIndex.ToString(), GApplication.Info, false);
            GApplication.WriteLog("------", GApplication.Info, false);
        }

        // 输出三角形和其组成的边
        foreach (var triangle in Triangles)
        {
            GApplication.WriteLog($"Triangle Index: {triangle.Index}", GApplication.Info, false);
            GApplication.WriteLog("Edge IDs:", GApplication.Info, false);
            foreach (var edgeIndex in triangle.ConsistedEdgeIndexes)
                GApplication.WriteLog(edgeIndex.ToString(), GApplication.Info, false);
            GApplication.WriteLog("------", GApplication.Info, false);
        }
    }

    public int GetNodeCount()
    {
        return Nodes.Count;
    }

    public TinNode GetNodeByIndex(int index)
    {
        if (index < 1 || index > Nodes.Count)
            // 处理索引超出范围的情况，这里你可以选择抛出异常或返回 null
            throw new IndexOutOfRangeException("Index is out of range for Nodes list.");
        // 或者返回 null 或者其他适当的处理方式
        // return null;
        return Nodes[index - 1];
    }

    public int GetEdgeCount()
    {
        return Edges.Count;
    }

    public TinEdge GetEdgeByIndex(int index)
    {
        if (index < 1 || index > Edges.Count)
            // 处理索引超出范围的情况，这里你可以选择抛出异常或返回 null
            throw new IndexOutOfRangeException("Index is out of range for Edges list.");
        // 或者返回 null 或者其他适当的处理方式
        // return null;
        return Edges[index - 1];
    }

    public int GetTriangleCount()
    {
        return Triangles.Count;
    }

    public TinTriangle GetTriangleByIndex(int index)
    {
        if (index < 1 || index > Nodes.Count)
            // 处理索引超出范围的情况，这里你可以选择抛出异常或返回 null
            throw new IndexOutOfRangeException("Index is out of range for Triangles list.");
        // 或者返回 null 或者其他适当的处理方式
        // return null;
        return Triangles[index - 1];
    }

    public TinNode GetNearestNode(MapPoint mapPoint)
    {
        TinNode nearestNode = null;
        var minDistance = double.MaxValue;

        foreach (var node in Nodes)
        {
            // 假设节点有 X 和 Y 坐标属性
            var nodeX = node.ToMapPoint(this).X; // 节点的 X 坐标
            var nodeY = node.ToMapPoint(this).Y; // 节点的 Y 坐标

            // 计算给定点与当前节点地图点之间的距离
            var distance = CalculateDistance(mapPoint.X, mapPoint.Y, nodeX, nodeY);

            // 更新最小距离和对应的最近节点
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestNode = node;
            }
        }

        return nearestNode;
    }

    private double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        // 计算欧几里得距离
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    public TinEdge GetNearestEdge(MapPoint mapPoint)
    {
        TinEdge nearestEdge = null;
        var minDistance = double.MaxValue;

        foreach (var edge in Edges)
        foreach (var nodeIndex in edge.ConnectedNodeIndexes)
        {
            // 使用节点 Index 获取地图点
            var nodePoint = GetNodeByIndex(nodeIndex).ToMapPoint(this);

            // 计算给定点与当前节点地图点之间的距离
            var distance = CalculateDistance(mapPoint.X, mapPoint.Y, nodePoint.X, nodePoint.Y);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEdge = edge;
            }
        }

        return nearestEdge;
    }

    public void SerializeToJSONFile(string jsonFilePath)
    {
        // 将 TinDataset 对象转换为 JSON 字符串
        var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);

        // 保存 JSON 字符串到文件
        File.WriteAllText(jsonFilePath, jsonString);
    }

    public void SerializeToXMLFile(string xmlFilePath)
    {
        // 将 TinDataset 对象转换为 JSON 字符串
        var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);

        // 将对象从 JSON 转换为 XML
        var xmlDocument = JsonConvert.DeserializeXmlNode(jsonString, "TinDataset");

        // 保存 XML 数据到文件
        xmlDocument.Save(xmlFilePath);
    }

    public static TinDataset DeserializeFromJSONFile(string filePath)
    {
        if (File.Exists(filePath))
            try
            {
                // 从文件中读取 JSON 字符串
                var jsonString = File.ReadAllText(filePath);

                // 尝试反序列化 JSON 字符串为 TinDataset 对象
                return JsonConvert.DeserializeObject<TinDataset>(jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"反序列化失败: {ex.Message}");
                throw;
            }

        // 如果文件不存在，则返回 null 或者进行其他适当的处理
        return null;
    }

    public static TinDataset DeserializeFromXMLFile(string xmlFilePath)
    {
        if (File.Exists(xmlFilePath))
            try
            {
                // 读取 XML 文件内容
                var xmlContent = File.ReadAllText(xmlFilePath);

                // 加载 XML 文档
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlContent);

                // 获取 TinDataset 节点的内部内容
                var tinDatasetNode = xmlDocument.SelectSingleNode("TinDataset");

                // 将内部内容转换为 JSON
                var jsonString = JsonConvert.SerializeXmlNode(tinDatasetNode, Formatting.None, true);

                // 将 JSON 反序列化为 TinDataset 对象
                return JsonConvert.DeserializeObject<TinDataset>(jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"反序列化失败: {ex.Message}");
                return null;
            }

        Console.WriteLine("文件不存在.");
        return null;
    }

    public Polygon GetDataArea()
    {
        var (minX, minY, maxX, maxY) = CalculateDataAreaBounds();

        // 创建多边形对象表示数据区域
        var dataAreaPolygon = PolygonBuilder.CreatePolygon(
            new List<MapPoint>
            {
                MapPointBuilder.CreateMapPoint(minX, minY),
                MapPointBuilder.CreateMapPoint(minX, maxY),
                MapPointBuilder.CreateMapPoint(maxX, maxY),
                MapPointBuilder.CreateMapPoint(maxX, minY)
            });

        return dataAreaPolygon;
    }

    private (double minX, double minY, double maxX, double maxY) CalculateDataAreaBounds()
    {
        if (Nodes.Count == 0)
            // 如果没有节点，返回默认的边界值
            return (0, 0, 0, 0);

        // 初始化边界值为第一个节点的坐标
        var minX = Nodes[0].ToMapPoint(this).X;
        var minY = Nodes[0].ToMapPoint(this).Y;
        var maxX = Nodes[0].ToMapPoint(this).X;
        var maxY = Nodes[0].ToMapPoint(this).Y;

        // 遍历所有节点，更新边界值
        foreach (var node in Nodes)
        {
            var nodePoint = node.ToMapPoint(this);

            minX = Math.Min(minX, nodePoint.X);
            minY = Math.Min(minY, nodePoint.Y);
            maxX = Math.Max(maxX, nodePoint.X);
            maxY = Math.Max(maxY, nodePoint.Y);
        }

        return (minX, minY, maxX, maxY);
    }
}

public class Functions
{
    public static Dictionary<int, MapPoint> GetFid2PointFromFeatureClass(string featureClassName)
    {
        var fid2Point = new Dictionary<int, MapPoint>();

        try
        {
            // 获取现有的要素类
            var featureClass = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                .Where(l => l.Name == featureClassName).FirstOrDefault();
            if (featureClass == null)
            {
                MessageBox.Show("未找到" + featureClassName + "图层!", "提示");
                return null;
            }

            using (var cursor = featureClass.Search())
            {
                while (cursor.MoveNext())
                    using (var feature = cursor.Current as Feature)
                    {
                        // 获取要素的 OID
                        var oid = Convert.ToInt32(feature.GetObjectID());

                        // 获取要素的形状
                        var shape = feature.GetShape() as MapPoint;

                        // 将 OID 和形状添加到字典中
                        fid2Point.Add(oid, shape);
                    }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"异常信息： {ex}");
        }

        return fid2Point;
    }

    public static FeatureClass CreateFeatureClassFromPoints(Dictionary<int, MapPoint> fid2Point,
        string outputFeatureClassName)
    {
        try
        {
            var message = string.Empty;
            var creationResult = false;

            using (var gdb = new Geodatabase(
                       new FileGeodatabaseConnectionPath(new Uri(Path.Combine(GApplication.Application.AppDataPath,
                           "MyWorkspace.gdb")))))
            {
                // 创建SchemaBuilder
                var schemaBuilder = new SchemaBuilder(gdb);

                // 获取空间参考：默认为 WGS 1984（EPSG：4326）
                var spatialReference = fid2Point.Count > 0
                    ? fid2Point.First().Value.SpatialReference
                    : SpatialReferenceBuilder.CreateSpatialReference(4326); // 默认WGS 1984

                // 创建点形状描述，包括 HasM = false 和 HasZ = true
                var shapeDescription = new ShapeDescription(GeometryType.Point, spatialReference)
                {
                    HasM = false,
                    HasZ = true
                };

                // 创建要素类的字段描述列表
                // 收集字段列表
                var fieldDescriptions = new List<FieldDescription>();

                // 创建要素类描述
                var fcDescription =
                    new FeatureClassDescription(outputFeatureClassName, fieldDescriptions, shapeDescription);

                // 将创建任务添加到 DDL 任务列表中
                schemaBuilder.Create(fcDescription);

                // 执行 DDL
                var success = schemaBuilder.Build();

                if (success)
                {
                    MessageBox.Show("成功创建要素类 " + outputFeatureClassName + "！");
                }
                else
                {
                    MessageBox.Show(outputFeatureClassName + " 已存在，创建失败。");
                    return null;
                }

                // 获取创建的要素类
                var outputFeatureClass = gdb.OpenDataset<FeatureClass>(outputFeatureClassName);

                using (outputFeatureClass)
                {
                    var editOperation = new EditOperation();
                    editOperation.Callback(context =>
                    {
                        var outputFeatureClassDefinition = outputFeatureClass.GetDefinition();

                        foreach (var kvp in fid2Point)
                            using (var rowBuffer = outputFeatureClass.CreateRowBuffer())
                            {
                                var mapPoint = kvp.Value;

                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = mapPoint;

                                using (var feature = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    context.Invalidate(feature);
                                }
                            }
                    }, outputFeatureClass);

                    try
                    {
                        if (!editOperation.IsEmpty)
                        {
                            MessageBox.Show("图层" + outputFeatureClassName + "插入要素");
                            creationResult = editOperation.Execute();
                            if (creationResult)
                                Project.Current.SaveEditsAsync();
                            else
                                message = editOperation.ErrorMessage;
                        }
                    }
                    catch (GeodatabaseException exObj)
                    {
                        message = exObj.Message;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        MessageBox.Show("异常信息片:" + message);
                        return null;
                    }

                    ;

                    return outputFeatureClass;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"异常信息： {ex}");
        }

        return null;
    }

    public static async Task<IGPResult> CreateTin(string inputFeatureClass, string outputTIN)
    {
        IGPResult tGeoResult = null;

        using (var gdb =
               new Geodatabase(
                   new FileGeodatabaseConnectionPath(new Uri(Path.Combine(GApplication.Application.AppDataPath,
                       "MyWorkspace.gdb")))))
        {
            var inputfc = gdb.OpenDataset<FeatureClass>(inputFeatureClass);
            var spatialReference = inputfc.GetDefinition().GetSpatialReference();

            // 建立TIN创建的参数
            var valueArray =
                Geoprocessing.MakeValueArray(Path.Combine(GApplication.Application.AppDataPath, outputTIN),
                    spatialReference,
                    Path.Combine(Path.Combine(GApplication.Application.AppDataPath, "MyWorkspace.gdb"),
                        inputFeatureClass));

            // 执行【Create TIN】操作
            var cts = new CancellationTokenSource();

            var executeFlags = GPExecuteToolFlags.RefreshProjectItems | GPExecuteToolFlags.GPThread |
                               GPExecuteToolFlags.AddToHistory;

            tGeoResult = await Geoprocessing.ExecuteToolAsync("CreateTIN_3d", valueArray, null, cts.Token,
                (eventName, o) => { Debug.WriteLine($@"GP event: {eventName}"); }, executeFlags);

            // 检查结果状态以确定操作是否成功
            if (tGeoResult.ErrorCode != 0)
                // 错误处理逻辑
                MessageBox.Show("Create TIN failed. " + tGeoResult.ErrorMessages);
            //Geoprocessing.ShowMessageBox(results.Result.Messages, "GP Messages", results.Result.IsFailed ? GPMessageBoxStyle.Error : GPMessageBoxStyle.Default);
            else
                MessageBox.Show("Create TIN Success! " + tGeoResult.ErrorMessages);
        }

        return tGeoResult;
    }

    public static async Task<IGPResult> TinNode(string inputTIN, string outputFeatureClass)
    {
        inputTIN = Path.Combine(GApplication.Application.AppDataPath, inputTIN);
        outputFeatureClass = Path.Combine(Path.Combine(GApplication.Application.AppDataPath, "MyWorkspace.gdb"),
            outputFeatureClass);
        MessageBox.Show("inputTIN: " + inputTIN + " outputFeatureClass: " + outputFeatureClass);

        IGPResult tGeoResult = null;

        // TINode创建的参数
        var valueArray =
            Geoprocessing.MakeValueArray(inputTIN, outputFeatureClass);

        // 执行【TINode】操作
        var cts = new CancellationTokenSource();

        var executeFlags = GPExecuteToolFlags.RefreshProjectItems | GPExecuteToolFlags.GPThread |
                           GPExecuteToolFlags.AddToHistory;

        tGeoResult = await Geoprocessing.ExecuteToolAsync("TINode_3d", valueArray, null, cts.Token,
            (eventName, o) => { Debug.WriteLine($@"GP event: {eventName}"); }, executeFlags);

        // 检查结果状态以确定操作是否成功
        if (tGeoResult.ErrorCode != 0)
            // 错误处理逻辑
            MessageBox.Show("TinNode failed. " + tGeoResult.ErrorMessages);
        //Geoprocessing.ShowMessageBox(results.Result.Messages, "GP Messages", results.Result.IsFailed ? GPMessageBoxStyle.Error : GPMessageBoxStyle.Default);
        else
            MessageBox.Show("TinNode Success! " + tGeoResult.ErrorMessages);

        return tGeoResult;
    }
}