using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Mapping;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace SMGI_Common
{
    public class TIN
    {


    }

    public class TinNode
    {
        public int NodeID { get; set; }
        public List<TinEdge> ConnectedEdges { get; set; }

        public TinNode(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Node ID must be a positive integer.");
            }

            NodeID = id;
            ConnectedEdges = new List<TinEdge>();
        }
    }

    public class TinEdge
    {
        public int EdgeID { get; set; }
        public TinNode StartNode { get; set; }
        public TinNode EndNode { get; set; }

        public TinEdge(int id, TinNode startNode, TinNode endNode)
        {
            if (startNode.NodeID == endNode.NodeID)
            {
                throw new ArgumentException("Start node and end node cannot be the same.");
            }

            if (startNode.ConnectedEdges.Any(edge => edge.EndNode.NodeID == endNode.NodeID) ||
                endNode.ConnectedEdges.Any(edge => edge.EndNode.NodeID == startNode.NodeID))
            {
                throw new ArgumentException("Edges cannot intersect with existing edges.");
            }

            EdgeID = id;
            StartNode = startNode;
            EndNode = endNode;
            startNode.ConnectedEdges.Add(this);
            endNode.ConnectedEdges.Add(this);
        }
    }


    public class TinTriangle
    {
        public int TriangleID { get; set; }
        public TinEdge[] Edges { get; set; }

        public TinTriangle(int id, TinEdge edge1, TinEdge edge2, TinEdge edge3)
        {
            // 检测边和节点是否完全相同
            if (edge1 == edge2 || edge1 == edge3 || edge2 == edge3)
            {
                throw new ArgumentException("Edges cannot be completely the same in a triangle.");
            }

            Edges = new TinEdge[] { edge1, edge2, edge3 };
            TriangleID = id;
        }

        static string GetPolylineIdentifier(Polyline polyline)
        {
            // 对 Polyline 的点按照坐标值进行排序
            var sortedPoints = polyline.Points.OrderBy(p => p.X).ThenBy(p => p.Y).ThenBy(p => p.Z);

            // 获取 Polyline 的起点和终点
            MapPoint startPoint = sortedPoints.First();
            MapPoint endPoint = sortedPoints.Last();

            // 获取排序后的点集合的哈希值作为唯一标识符
            return $"{startPoint.X},{startPoint.Y},{startPoint.Z},{endPoint.X},{endPoint.Y},{endPoint.Z}";
        }

        public static int GetTriangleNum(string triangleLyrName)
        {
            int triangleNum = 0;

            var Trianglelyr =
                MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
                    .Where(l => (l as FeatureLayer).Name == triangleLyrName).FirstOrDefault() as FeatureLayer;
            if (Trianglelyr == null)
            {
                MessageBox.Show("未找到" + triangleLyrName + "图层!", "提示");
                return 0;
            }

            // 获取Trianglelyr图层的要素类
            var featureClass = Trianglelyr.GetTable() as FeatureClass;

            using (var cursor = featureClass.Search(null, true))
            {
                while (cursor.MoveNext())
                {
                    triangleNum ++;
                }
            }

            return triangleNum;
        }

        public static void TinTriangleTransition(string triangleLyrName, string outputLyrName, String type, bool all)
        {
            var Trianglelyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(l => (l as FeatureLayer).Name == triangleLyrName).FirstOrDefault() as FeatureLayer;
            if (Trianglelyr == null)
            {
                MessageBox.Show("未找到" + triangleLyrName + "图层!", "提示");
                return;
            }
            SpatialReference triangleSpatialReference = Trianglelyr.GetSpatialReference();

            Geodatabase gdb =
                new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(Project.Current.DefaultGeodatabasePath)));

            // 创建SchemaBuilder
            SchemaBuilder schemaBuilder = new SchemaBuilder(gdb);

            FeatureClass outputFeatureClass = null;

            GeometryType geometryType = GeometryType.Unknown;

            using (gdb)
            {
                if (type == "Node")
                {
                    geometryType = GeometryType.Point;
                }else if (type == "Edge")
                {
                    geometryType = GeometryType.Polyline;
                }
                else
                {
                    return;
                }

                var shapeDescription = new ShapeDescription(geometryType, triangleSpatialReference)
                {
                    HasM = false,
                    HasZ = true
                };

                //var shapeFieldDescription = new ArcGIS.Core.Data.DDL.FieldDescription("aaa", FieldType.String); 

                var fcName = outputLyrName;
                try
                {
                    // 收集字段列表
                    var fieldDescriptions = new List<ArcGIS.Core.Data.DDL.FieldDescription>()
                    {
                        //shapeFieldDescription,
                    };

                    // 创建FeatureClassDescription
                    var fcDescription = new FeatureClassDescription(fcName, fieldDescriptions, shapeDescription);

                    // 将创建任务添加到DDL任务列表中
                    schemaBuilder.Create(fcDescription);

                    // 执行DDL
                    bool success = schemaBuilder.Build();

                    if (success)
                    {
                        MessageBox.Show("新建成功，目标图层" + fcName + "并不存在!");
                    }
                    else
                    {
                        // MessageBox.Show("新建失败，目标图层" + fcName + "已经存在!!");
                    }

                    outputFeatureClass = gdb.OpenDataset<FeatureClass>(fcName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($@"Exception: {ex}");
                }
            }

            // 获取要素类的Table对象
            Table table = outputFeatureClass as Table;

            // 检查要素类是否为空
            if (table != null && table.GetCount() == 0)
            {
                // 要素类为空
                MessageBox.Show("目标图层" + outputLyrName + "为空。");
            }
            else
            {
                // 要素类不为空
                MessageBox.Show("目标图层" + outputLyrName + "不为空，其中含有" + table.GetCount() + "个要素");
                return;
            }

            string message = String.Empty;
            bool creationResult = false;

            //declare the callback here. We are not executing it yet
            EditOperation editOperation = new EditOperation();
            editOperation.Callback(context =>
            {
                FeatureClassDefinition outputFeatureClassDefinition = outputFeatureClass.GetDefinition();

                using (RowBuffer rowBuffer = outputFeatureClass.CreateRowBuffer())
                {

                    // 获取Trianglelyr图层的要素类
                    var triangleFeatureClass = Trianglelyr.GetTable() as FeatureClass;

                    // 在方法开始前创建一个 HashSet 用于唯一标识符
                    HashSet<string> addedFeaturesHashSet = new HashSet<string>();

                    // 遍历三角形要素
                    using (var cursor = triangleFeatureClass.Search(null, true))
                    {
                        while (cursor.MoveNext())
                        {
                            var triangle = cursor.Current as Feature;

                            // 获取三角形的顶点集合
                            //var points = (feature.GetShape() as Polygon).Points;
                            var polygon = triangle.GetShape() as Polygon;
                            long objectId = triangle.GetObjectID();
                            string objectIdString = objectId.ToString();

                            var points = polygon.Points;
                            MapPoint point1Shape = points[2];
                            MapPoint point2Shape = points[1];
                            MapPoint point3Shape = points[0];
                            string point1Key = $"{point1Shape.X},{point1Shape.Y},{point1Shape.Z}";
                            string point2Key = $"{point2Shape.X},{point2Shape.Y},{point2Shape.Z}";
                            string point3Key = $"{point3Shape.X},{point3Shape.Y},{point3Shape.Z}";

                            // 获取三角形的三条边
                            Polyline edgeLine1 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point2Shape, point1Shape }); // 第一条边
                            Polyline edgeLine2 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point3Shape, point2Shape }); // 第二条边
                            Polyline edgeLine3 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point1Shape, point3Shape }); // 第三条边
                            string edge1Key = $"{GetPolylineIdentifier(edgeLine1)}"; // 边1的唯一标识符
                            string edge2Key = $"{GetPolylineIdentifier(edgeLine2)}"; // 边2的唯一标识符
                            string edge3Key = $"{GetPolylineIdentifier(edgeLine3)}"; // 边3的唯一标识符

                            Debug.WriteLine("对于三角形" + objectIdString + "," + "三条边的唯一标志符号分别是: " + "edge1Key: " + edge1Key +
                                            "," + "edge2Key: " + edge2Key + "," + "edge3Key: " + edge3Key);
                            if (type == "Node")
                            {
                                if (all || !addedFeaturesHashSet.Contains(point1Key))
                                {
                                    // 添加point1到outputFeatureClass
                                    rowBuffer[outputFeatureClassDefinition.GetShapeField()] = point1Shape;
                                    using (Feature feature1 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature1);
                                    }
                                    addedFeaturesHashSet.Add(point1Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(point2Key))
                                {
                                    // 添加point2到outputFeatureClass
                                    rowBuffer[outputFeatureClassDefinition.GetShapeField()] = point2Shape;
                                    using (Feature feature2 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature2);
                                    }
                                    addedFeaturesHashSet.Add(point2Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(point3Key))
                                {
                                    // 添加point3到outputFeatureClass
                                    rowBuffer[outputFeatureClassDefinition.GetShapeField()] = point3Shape;
                                    using (Feature feature3 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature3);
                                    }
                                    addedFeaturesHashSet.Add(point3Key);
                                }
                            }
                            else if(type == "Edge")
                            {
                                if (all || !addedFeaturesHashSet.Contains(edge1Key))
                                {
                                    // 添加edgeLine1到outputFeatureClass
                                    rowBuffer[outputFeatureClassDefinition.GetShapeField()] = edgeLine1;
                                    using (Feature feature1 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature1);
                                    }
                                    addedFeaturesHashSet.Add(edge1Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(edge2Key))
                                {
                                    // 添加edgeLine2到outputFeatureClass
                                    rowBuffer[outputFeatureClassDefinition.GetShapeField()] = edgeLine2;
                                    using (Feature feature2 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature2);
                                    }
                                    addedFeaturesHashSet.Add(edge2Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(edge3Key))
                                {
                                    // 添加edgeLine3到outputFeatureClass
                                    rowBuffer[outputFeatureClassDefinition.GetShapeField()] = edgeLine3;
                                    using (Feature feature3 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature3);
                                    }
                                    addedFeaturesHashSet.Add(edge3Key);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }

            }, outputFeatureClass);

            try
            {
                if (!editOperation.IsEmpty)
                {
                    MessageBox.Show("图层" + outputLyrName + "插入要素");
                    creationResult = editOperation.Execute();
                    if (creationResult)
                    {
                        // Save the changes
                        Project.Current.SaveEditsAsync();
                    }
                    else
                    {
                        message = editOperation.ErrorMessage;
                    }
                }
            }
            catch (GeodatabaseException exObj)
            {
                message = exObj.Message;
            }

            if (!string.IsNullOrEmpty(message))
                MessageBox.Show("插入图层" + outputLyrName + "发生错误: " + message);
        }
    }

    public class TinDataset
    {
        public List<TinNode> Nodes { get; set; }
        public List<TinEdge> Edges { get; set; }
        public List<TinTriangle> Triangles { get; set; }

        public TinDataset()
        {
            Nodes = new List<TinNode>();
            Edges = new List<TinEdge>();
            Triangles = new List<TinTriangle>();
        }

        public List<int> GetTrianglesSharingEdge(int triangleID, List<TinTriangle> triangles)
        {
            List<int> sharedTriangleIDs = new List<int>();

            // 找到目标三角形
            TinTriangle targetTriangle = triangles.FirstOrDefault(triangle => triangle.TriangleID == triangleID);
            if (targetTriangle != null)
            {
                // 遍历所有其他三角形，检查它们与目标三角形是否共享至多一条边
                foreach (var triangle in triangles)
                {
                    // 跳过目标三角形本身
                    if (triangle.TriangleID == triangleID)
                    {
                        continue;
                    }

                    // 检查目标三角形的每条边是否在当前三角形中存在
                    int sharedEdgeCount = targetTriangle.Edges.Count(edge => triangle.Edges.Any(tEdge => tEdge.EdgeID == edge.EdgeID));

                    // 如果共享边的数量等于1，表示两个三角形共享边
                    if (sharedEdgeCount == 1)
                    {
                        sharedTriangleIDs.Add(triangle.TriangleID);
                    }
                }
            }

            return sharedTriangleIDs;
        }

        public int GetSharedEdgeID(int triangle1ID, int triangle2ID, List<TinTriangle> triangles)
        {
            int sharedEdgeID = -1; // 初始化为-1，表示没有共享边

            // 找到包含给定ID的两个三角形
            TinTriangle triangle1 = triangles.FirstOrDefault(triangle => triangle.TriangleID == triangle1ID);
            TinTriangle triangle2 = triangles.FirstOrDefault(triangle => triangle.TriangleID == triangle2ID);

            if (triangle1 != null && triangle2 != null)
            {
                // 遍历两个三角形的边，找到共享边的ID
                foreach (var edge1 in triangle1.Edges)
                {
                    foreach (var edge2 in triangle2.Edges)
                    {
                        if (edge1.EdgeID == edge2.EdgeID)
                        {
                            sharedEdgeID = edge1.EdgeID;
                            break;
                        }
                    }
                    if (sharedEdgeID != -1)
                    {
                        break;
                    }
                }
            }

            return sharedEdgeID;
        }

        public static int[] GetNodeIDsFromEdgeID(int edgeID, List<TinEdge> edges)
        {
            HashSet<int> nodeIDs = new HashSet<int>();

            TinEdge targetEdge = edges.FirstOrDefault(edge => edge.EdgeID == edgeID);
            if (targetEdge != null)
            {
                nodeIDs.Add(targetEdge.StartNode.NodeID);
                nodeIDs.Add(targetEdge.EndNode.NodeID);
            }

            return nodeIDs.ToArray();
        }

        public int[] GetSharedNodeIDs(int triangle1ID, int triangle2ID, List<TinTriangle> triangles, List<TinEdge> edges)
        {
            int sharedEdgeID = GetSharedEdgeID(triangle1ID, triangle2ID, triangles);

            if (sharedEdgeID != -1)
            {
                return GetNodeIDsFromEdgeID(sharedEdgeID, edges);
            }

            return new int[0]; // Return an empty array if there are no shared nodes
        }

        public static Tuple<int[], int[]> GetNodeAndEdgeIDsFromTriangleID(int triangleID, List<TinTriangle> triangles)
        {
            HashSet<int> uniqueNodeIDs = new HashSet<int>();
            List<int> edgeIDs = new List<int>();

            TinTriangle targetTriangle = triangles.FirstOrDefault(triangle => triangle.TriangleID == triangleID);
            if (targetTriangle != null)
            {
                foreach (var edge in targetTriangle.Edges)
                {
                    uniqueNodeIDs.Add(edge.StartNode.NodeID);
                    uniqueNodeIDs.Add(edge.EndNode.NodeID);
                    edgeIDs.Add(edge.EdgeID);
                }
            }

            return new Tuple<int[], int[]>(uniqueNodeIDs.ToArray(), edgeIDs.ToArray());
        }

        public static TinTriangle GetTriangleFromEdgeIDs(int[] edgeIDs, List<TinTriangle> triangles)
        {
            TinTriangle targetTriangle = triangles.FirstOrDefault(triangle =>
            {
                int[] triangleEdgeIDs = triangle.Edges.Select(edge => edge.EdgeID).ToArray();

                return edgeIDs.Length == triangleEdgeIDs.Length &&
                       edgeIDs.All(edgeID => triangleEdgeIDs.Contains(edgeID));
            });

            return targetTriangle;
        }
    }

    /*
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Framework.Dialogs;

public class TinNode
{
    public int NodeID { get; set; }
    public List<int> ConnectedEdgeIDs { get; set; }

    public TinNode(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Node ID must be a positive integer.");
        }

        NodeID = id;
        ConnectedEdgeIDs = new List<int>();
    }

    public static MapPoint ToMapPointByID(string nodeLyrName, int nodeID)
    {
        // 获取包含节点信息的图层
        var nodeLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == nodeLyrName);

        if (nodeLayer == null)
        {
            MessageBox.Show("未找到名为 " + nodeLyrName + " 的图层！", "提示");
            return null;
        }

        // 使用 NodeID 查询节点要素
        QueryFilter queryFilter = new QueryFilter
        {
            WhereClause = $"OBJECTID = {nodeID}" // 根据 OID 进行查询
        };

        using (RowCursor rowCursor = nodeLayer.Search(queryFilter))
        {
            while (rowCursor.MoveNext())
            {
                var nodeFeature = (Feature)rowCursor.Current;

                // 提取节点要素中的点位置信息
                MapPoint nodePoint = nodeFeature.GetShape() as MapPoint;
                return nodePoint;
            }
        }

        // 如果未找到对应 NodeID 的节点，返回 null 或者其他适当的值
        return null;
    }

    public static List<int> GetAdjacentNodeIDsByID(int nodeID, TinDataset dataset)
    {
        // 创建用于存储相邻节点ID的集合
        HashSet<int> adjacentNodeIDs = new HashSet<int>();

        // 遍历数据集中的每条边
        foreach (TinEdge edge in dataset.Edges)
        {
            // 找到连接到给定节点的边
            if (edge.StartNodeID == nodeID)
            {
                adjacentNodeIDs.Add(edge.EndNodeID);
            }
            else if (edge.EndNodeID == nodeID)
            {
                adjacentNodeIDs.Add(edge.StartNodeID);
            }
        }

        // 将HashSet转换为List并返回
        return adjacentNodeIDs.ToList();
    }

}

public class TinEdge
{
    public int EdgeID { get; set; }
    public int StartNodeID { get; set; }
    public int EndNodeID { get; set; }

    public TinEdge(int id, int startNodeID, int endNodeID, TinDataset dataset)
    {
        if (startNodeID == endNodeID)
        {
            throw new ArgumentException("Start node and end node cannot be the same.");
        }

        TinNode startNode = dataset.Nodes.FirstOrDefault(node => node.NodeID == startNodeID);
        TinNode endNode = dataset.Nodes.FirstOrDefault(node => node.NodeID == endNodeID);

        if (startNode != null && endNode != null)
        {
            startNode.ConnectedEdgeIDs.Add(id);
            endNode.ConnectedEdgeIDs.Add(id);
        }

        EdgeID = id;
        StartNodeID = startNodeID;
        EndNodeID = endNodeID;
    }

    public static Polyline ToPolylineByID(string edgeLyrName, int edgeID)
    {
        // 获取包含边信息的图层
        var edgeLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == edgeLyrName);

        if (edgeLayer == null)
        {
            MessageBox.Show("未找到名为 " + edgeLyrName + " 的图层！", "提示");
            return null;
        }

        // 使用 EdgeID 查询边要素
        QueryFilter queryFilter = new QueryFilter
        {
            WhereClause = $"OBJECTID = {edgeID}" // 根据 OID 进行查询
        };

        using (RowCursor rowCursor = edgeLayer.Search(queryFilter))
        {
            while (rowCursor.MoveNext())
            {
                var edgeFeature = (Feature)rowCursor.Current;

                // 提取边要素中的 Polyline 信息
                Polyline edgePolyline = edgeFeature.GetShape() as Polyline;
                return edgePolyline;
            }
        }

        // 如果未找到对应 EdgeID 的边，返回 null 或者其他适当的值
        return null;
    }

    public static string GetPolylineIdentifier(Polyline polyline)
    {
        // 对 Polyline 的点按照坐标值进行排序
        var sortedPoints = polyline.Points.OrderBy(p => p.X).ThenBy(p => p.Y).ThenBy(p => p.Z);

        // 获取 Polyline 的起点和终点
        MapPoint startPoint = sortedPoints.First();
        MapPoint endPoint = sortedPoints.Last();

        // 获取排序后的点集合的哈希值作为唯一标识符
        return $"{startPoint.X},{startPoint.Y},{startPoint.Z},{endPoint.X},{endPoint.Y},{endPoint.Z}";
    }

    public static int GetNextEdgeIDInTriangleByID(int edgeID, TinDataset dataset)
    {
        // 获取给定边所在的三角形ID
        int triangleID = TinDataset.GetTriangleIDByEdgeID(edgeID, dataset);

        // 如果找不到边所在的三角形，返回默认值
        if (triangleID == -1)
        {
            return -1;
        }

        // 找到当前三角形
        TinTriangle triangle = dataset.Triangles.FirstOrDefault(t => t.TriangleID == triangleID);

        // 检查当前三角形是否存在
        if (triangle != null)
        {
            // 找到当前边在三角形中的索引位置
            int edgeIndex = Array.IndexOf(triangle.EdgeIDs, edgeID);

            // 如果找到了边在三角形中的位置
            if (edgeIndex != -1)
            {
                // 获取下一个边的索引（以顺时针方向）
                int nextEdgeIndex = (edgeIndex + 2) % 3;

                // 返回下一个边的ID
                return triangle.EdgeIDs[nextEdgeIndex];
            }
        }

        // 如果未找到或出现问题，返回默认值
        return -1;
    }
}

public class TinTriangle
{
    public int TriangleID { get; set; }
    public int[] EdgeIDs { get; set; }

    public TinTriangle(int id, int edge1ID, int edge2ID, int edge3ID)
    {
        // 检测边是否不相同
        if (edge1ID == edge2ID || edge1ID == edge3ID || edge2ID == edge3ID)
        {
            throw new ArgumentException("Edges cannot be completely the same in a triangle.");
        }

        EdgeIDs = new int[] { edge1ID, edge2ID, edge3ID };
        TriangleID = id;
    }

    public static Polygon ToPolygonByID(string triangleLyrName, int triangleID)
    {
        // 获取包含三角形信息的图层
        var triangleLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
            .FirstOrDefault(layer => layer.Name == triangleLyrName);

        if (triangleLayer == null)
        {
            MessageBox.Show("未找到名为 " + triangleLyrName + " 的图层！", "提示");
            return null;
        }

        // 使用 TriangleID 查询三角形要素
        QueryFilter queryFilter = new QueryFilter
        {
            WhereClause = $"OID = {triangleID}" // 根据 OID 进行查询
        };

        using (RowCursor rowCursor = triangleLayer.Search(queryFilter))
        {
            while (rowCursor.MoveNext())
            {
                var triangleFeature = (Feature)rowCursor.Current;

                // 提取三角形要素中的 Polygon 信息
                Polygon trianglePolygon = triangleFeature.GetShape() as Polygon;
                return trianglePolygon;
            }
        }

        // 如果未找到对应 TriangleID 的三角形，返回 null 或者其他适当的值
        return null;
    }

    public List<int> GetTriangleEdgeIDs()
    {
        return EdgeIDs.ToList();
    }

    public static int GetTriangleNum(string triangleLyrName)
    {
        int triangleNum = 0;

        var Trianglelyr =
        MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>()
        .Where(l => (l as FeatureLayer).Name == triangleLyrName).FirstOrDefault() as FeatureLayer;
        if (Trianglelyr == null)
        {
            MessageBox.Show("未找到" + triangleLyrName + "图层!", "提示");
            return 0;
        }

        // 获取Trianglelyr图层的要素类
        var featureClass = Trianglelyr.GetTable() as FeatureClass;

        using (var cursor = featureClass.Search(null, true))
        {
            while (cursor.MoveNext())
            {
                triangleNum++;
            }
        }

        return triangleNum;
    }

    public static void TinTriangleTransition(string triangleLyrName, string outputLyrName, String type, bool all)
    {
        var Trianglelyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(l => (l as FeatureLayer).Name == triangleLyrName).FirstOrDefault() as FeatureLayer;
        if (Trianglelyr == null)
        {
            MessageBox.Show("未找到" + triangleLyrName + "图层!", "提示");
            return;
        }
        SpatialReference triangleSpatialReference = Trianglelyr.GetSpatialReference();

        Geodatabase gdb =
        new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(Project.Current.DefaultGeodatabasePath)));

        // 创建SchemaBuilder
        SchemaBuilder schemaBuilder = new SchemaBuilder(gdb);

        FeatureClass outputFeatureClass = null;

        GeometryType geometryType = GeometryType.Unknown;

        using (gdb)
        {
            if (type == "Node")
            {
                geometryType = GeometryType.Point;
            }
            else if (type == "Edge")
            {
                geometryType = GeometryType.Polyline;
            }
            else
            {
                return;
            }

            var shapeDescription = new ShapeDescription(geometryType, triangleSpatialReference)
            {
                HasM = false,
                HasZ = true
            };

            //var shapeFieldDescription = new ArcGIS.Core.Data.DDL.FieldDescription("aaa", FieldType.String); 

            var fcName = outputLyrName;
            try
            {
                // 收集字段列表
                var fieldDescriptions = new List<ArcGIS.Core.Data.DDL.FieldDescription>()
                {
                    //shapeFieldDescription,
                };

                // 创建FeatureClassDescription
                var fcDescription = new FeatureClassDescription(fcName, fieldDescriptions, shapeDescription);

                // 将创建任务添加到DDL任务列表中
                schemaBuilder.Create(fcDescription);

                // 执行DDL
                bool success = schemaBuilder.Build();

                if (success)
                {
                    MessageBox.Show("新建成功，目标图层" + fcName + "并不存在!");
                }
                else
                {
                    // MessageBox.Show("新建失败，目标图层" + fcName + "已经存在!!");
                }

                outputFeatureClass = gdb.OpenDataset<FeatureClass>(fcName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"Exception: {ex}");
            }
        }

        // 获取要素类的Table对象
        Table table = outputFeatureClass as Table;

        // 检查要素类是否为空
        if (table != null && table.GetCount() == 0)
        {
            // 要素类为空
            MessageBox.Show("目标图层" + outputLyrName + "为空。");
        }
        else
        {
            // 要素类不为空
            MessageBox.Show("目标图层" + outputLyrName + "不为空，其中含有" + table.GetCount() + "个要素");
            return;
        }

        string message = String.Empty;
        bool creationResult = false;

        //declare the callback here. We are not executing it yet
        EditOperation editOperation = new EditOperation();
        editOperation.Callback(context =>
        {
            FeatureClassDefinition outputFeatureClassDefinition = outputFeatureClass.GetDefinition();

            using (RowBuffer rowBuffer = outputFeatureClass.CreateRowBuffer())
            {

                // 获取Trianglelyr图层的要素类
                var triangleFeatureClass = Trianglelyr.GetTable() as FeatureClass;

                // 在方法开始前创建一个 HashSet 用于唯一标识符
                HashSet<string> addedFeaturesHashSet = new HashSet<string>();

                // 遍历三角形要素
                using (var cursor = triangleFeatureClass.Search(null, true))
                {
                    while (cursor.MoveNext())
                    {
                        var triangle = cursor.Current as Feature;

                        // 获取三角形的顶点集合
                        //var points = (feature.GetShape() as Polygon).Points;
                        var polygon = ToPolygonByID(triangleLyrName, (int)triangle.GetObjectID());
                        long objectId = triangle.GetObjectID();
                        string objectIdString = objectId.ToString();

                        var points = polygon.Points;
                        MapPoint point1Shape = points[2];
                        MapPoint point2Shape = points[1];
                        MapPoint point3Shape = points[0];
                        string point1Key = $"{point1Shape.X},{point1Shape.Y},{point1Shape.Z}";
                        string point2Key = $"{point2Shape.X},{point2Shape.Y},{point2Shape.Z}";
                        string point3Key = $"{point3Shape.X},{point3Shape.Y},{point3Shape.Z}";

                        // 获取三角形的三条边
                        Polyline edgeLine1 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point2Shape, point1Shape }); // 第一条边
                        Polyline edgeLine2 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point3Shape, point2Shape }); // 第二条边
                        Polyline edgeLine3 = PolylineBuilder.CreatePolyline(new List<MapPoint> { point1Shape, point3Shape }); // 第三条边
                        string edge1Key = $"{TinEdge.GetPolylineIdentifier(edgeLine1)}"; // 边1的唯一标识符
                        string edge2Key = $"{TinEdge.GetPolylineIdentifier(edgeLine2)}"; // 边2的唯一标识符
                        string edge3Key = $"{TinEdge.GetPolylineIdentifier(edgeLine3)}"; // 边3的唯一标识符

                        Debug.WriteLine("对于三角形" + objectIdString + "," + "三条边的唯一标志符号分别是: " + "edge1Key: " + edge1Key +
                        "," + "edge2Key: " + edge2Key + "," + "edge3Key: " + edge3Key);
                        if (type == "Node")
                        {
                            if (all || !addedFeaturesHashSet.Contains(point1Key))
                            {
                                // 添加point1到outputFeatureClass
                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = point1Shape;
                                using (Feature feature1 = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    //To Indicate that the attribute table has to be updated
                                    context.Invalidate(feature1);
                                }
                                addedFeaturesHashSet.Add(point1Key);
                            }

                            if (all || !addedFeaturesHashSet.Contains(point2Key))
                            {
                                // 添加point2到outputFeatureClass
                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = point2Shape;
                                using (Feature feature2 = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    //To Indicate that the attribute table has to be updated
                                    context.Invalidate(feature2);
                                }
                                addedFeaturesHashSet.Add(point2Key);
                            }

                            if (all || !addedFeaturesHashSet.Contains(point3Key))
                            {
                                // 添加point3到outputFeatureClass
                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = point3Shape;
                                using (Feature feature3 = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    //To Indicate that the attribute table has to be updated
                                    context.Invalidate(feature3);
                                }
                                addedFeaturesHashSet.Add(point3Key);
                            }
                        }
                        else if (type == "Edge")
                        {
                            if (all || !addedFeaturesHashSet.Contains(edge1Key))
                            {
                                // 添加edgeLine1到outputFeatureClass
                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = edgeLine1;
                                using (Feature feature1 = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    //To Indicate that the attribute table has to be updated
                                    context.Invalidate(feature1);
                                }
                                addedFeaturesHashSet.Add(edge1Key);
                            }

                            if (all || !addedFeaturesHashSet.Contains(edge2Key))
                            {
                                // 添加edgeLine2到outputFeatureClass
                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = edgeLine2;
                                using (Feature feature2 = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    //To Indicate that the attribute table has to be updated
                                    context.Invalidate(feature2);
                                }
                                addedFeaturesHashSet.Add(edge2Key);
                            }

                            if (all || !addedFeaturesHashSet.Contains(edge3Key))
                            {
                                // 添加edgeLine3到outputFeatureClass
                                rowBuffer[outputFeatureClassDefinition.GetShapeField()] = edgeLine3;
                                using (Feature feature3 = outputFeatureClass.CreateRow(rowBuffer))
                                {
                                    //To Indicate that the attribute table has to be updated
                                    context.Invalidate(feature3);
                                }
                                addedFeaturesHashSet.Add(edge3Key);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

        }, outputFeatureClass);

        try
        {
            if (!editOperation.IsEmpty)
            {
                MessageBox.Show("图层" + outputLyrName + "插入要素");
                creationResult = editOperation.Execute();
                if (creationResult)
                {
                    // Save the changes
                    Project.Current.SaveEditsAsync();
                }
                else
                {
                    message = editOperation.ErrorMessage;
                }
            }
        }
        catch (GeodatabaseException exObj)
        {
            message = exObj.Message;
        }

        if (!string.IsNullOrEmpty(message))
            MessageBox.Show("插入图层" + outputLyrName + "发生错误: " + message);
    }
}

public class TinDataset
{
    public List<TinNode> Nodes { get; set; }
    public List<TinEdge> Edges { get; set; }
    public List<TinTriangle> Triangles { get; set; }

    public TinDataset(List<TinNode> nodes, List<TinEdge> edges, List<TinTriangle> triangles)
    {
        Nodes = nodes;
        Edges = edges;
        Triangles = triangles;
    }

    public static TinDataset GetTinDatasetDefinition(string triangleLyrName)
    {
        int triangleNum = TinTriangle.GetTriangleNum(triangleLyrName);
        TinTriangle.TinTriangleTransition(triangleLyrName, "CCC_TinNodesAll", "Node", true);
        TinTriangle.TinTriangleTransition(triangleLyrName, "CCC_TinEdgesAll", "Edge", true);
        TinTriangle.TinTriangleTransition(triangleLyrName, "CCC_TinNodesNotAll", "Node", false);
        TinTriangle.TinTriangleTransition(triangleLyrName, "CCC_TinEdgesNotAll", "Edge", false);

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
            TinTriangle triangle = new TinTriangle(i, 3 * i - 2, 3 * i - 1, 3 * i);
            triangles.Add(triangle);
        }

        // 添加节点、边和三角形到数据结构中
        tinData.Nodes = nodes;
        tinData.Edges = edges;
        tinData.Triangles = triangles;

        return tinData;
    }

    public static Tuple<List<int>, List<int>> GetTriangleEdgeIDs(int triangle1ID, int triangle2ID, TinDataset dataset)
    {
        // 检查三角形ID是否存在于数据集中
        if (dataset.Triangles.Any(triangle => triangle.TriangleID == triangle1ID) == false ||
            dataset.Triangles.Any(triangle => triangle.TriangleID == triangle2ID) == false)
        {
            MessageBox.Show("提供的三角形ID不存在于数据集中");
            return new Tuple<List<int>, List<int>>(new List<int>(), new List<int>());
        }

        // 找到具有给定ID的两个三角形
        TinTriangle triangle1 = dataset.Triangles.FirstOrDefault(triangle => triangle.TriangleID == triangle1ID);
        TinTriangle triangle2 = dataset.Triangles.FirstOrDefault(triangle => triangle.TriangleID == triangle2ID);

        // 如果其中一个三角形未找到，返回空列表
        if (triangle1 == null || triangle2 == null)
        {
            MessageBox.Show("未找到三角形");
            return new Tuple<List<int>, List<int>>(new List<int>(), new List<int>());
        }

        // 返回这两个三角形的所有边的ID作为一个 Tuple
        return new Tuple<List<int>, List<int>>(triangle1.GetTriangleEdgeIDs(), triangle2.GetTriangleEdgeIDs());
    }

    public static Tuple<int, int> GetSharedEdgeIDs(int triangle1ID, int triangle2ID, TinDataset dataset, string edgeLyrName)
    {
        Tuple<List<int>, List<int>> edgeIDsTuple = GetTriangleEdgeIDs(triangle1ID, triangle2ID, dataset);
        List<int> edgeIDsForTriangle1 = edgeIDsTuple.Item1;
        List<int> edgeIDsForTriangle2 = edgeIDsTuple.Item2;

        if (edgeIDsForTriangle1.Count != 0 && edgeIDsForTriangle2.Count != 0)
        {
            foreach (int edgeID1 in edgeIDsForTriangle1)
            {
                foreach (int edgeID2 in edgeIDsForTriangle2)
                {
                    TinEdge edge1 = dataset.Edges.FirstOrDefault(e => e.EdgeID == edgeID1);
                    TinEdge edge2 = dataset.Edges.FirstOrDefault(e => e.EdgeID == edgeID2);

                    // 假设 edge1 和 edge2 不为 null
                    if (edge1 != null && edge2 != null)
                    {
                        var polyline1 = TinEdge.ToPolylineByID(edgeLyrName, edgeID1);
                        var polyline2 = TinEdge.ToPolylineByID(edgeLyrName, edgeID2);

                        if (polyline1 != null && polyline2 != null)
                        {
                            var identifier1 = TinEdge.GetPolylineIdentifier(polyline1);
                            var identifier2 = TinEdge.GetPolylineIdentifier(polyline2);

                            if (!string.IsNullOrEmpty(identifier1) && !string.IsNullOrEmpty(identifier2) && string.Equals(identifier1, identifier2))
                            {
                                return new Tuple<int, int>(edgeID1, edgeID2);

                            }
                        }
                    }
                }
            }
        }

        return new Tuple<int, int>(0, 0);
    }

    public static int GetTriangleIDByEdgeID(int edgeID, TinDataset dataset)
    {
        // 遍历数据集中的每个三角形
        foreach (TinTriangle triangle in dataset.Triangles)
        {
            // 检查当前三角形是否包含指定的边
            if (triangle.EdgeIDs.Contains(edgeID))
            {
                // 如果包含，返回该三角形的ID
                return triangle.TriangleID;
            }
        }

        // 如果未找到包含指定边的三角形，返回一个默认值（例如：-1）
        return -1;
    }
}
 */


}
