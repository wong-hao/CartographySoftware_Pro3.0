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

        // Must be called within QueuedTask.Run()
        public bool FeatureClassExists(Geodatabase geodatabase, string featureClassName)
        {
            try
            {
                FeatureClassDefinition featureClassDefinition = geodatabase.GetDefinition<FeatureClassDefinition>(featureClassName);
                featureClassDefinition.Dispose();
                return true;
            }
            catch
            {
                // GetDefinition throws an exception if the definition doesn't exist
                return false;
            }
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
                FeatureClassDefinition facilitySiteDefinition = outputFeatureClass.GetDefinition();

                using (RowBuffer rowBuffer = outputFeatureClass.CreateRowBuffer())
                {

                    // 获取Trianglelyr图层的要素类
                    var featureClass = Trianglelyr.GetTable() as FeatureClass;

                    // 在方法开始前创建一个 HashSet 用于唯一标识符
                    HashSet<string> addedFeaturesHashSet = new HashSet<string>();

                    // 遍历三角形要素
                    using (var cursor = featureClass.Search(null, true))
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
                            string point1Key = $"{points[2].X},{points[2].Y}";
                            string point2Key = $"{points[1].X},{points[1].Y}";
                            string point3Key = $"{points[0].X},{points[0].Y}";

                            // 获取三角形的三条边
                            Polyline edgeLine1 = PolylineBuilder.CreatePolyline(new List<MapPoint> { points[1], points[2] }); // 第一条边
                            Polyline edgeLine2 = PolylineBuilder.CreatePolyline(new List<MapPoint> { points[0], points[1] }); // 第二条边
                            Polyline edgeLine3 = PolylineBuilder.CreatePolyline(new List<MapPoint> { points[2], points[0] }); // 第三条边
                            string edge1Key = $"{edgeLine1.Length3D}"; // 边1的唯一标识符
                            string edge2Key = $"{edgeLine2.Length3D}"; // 边2的唯一标识符
                            string edge3Key = $"{edgeLine3.Length3D}"; // 边3的唯一标识符

                            Debug.WriteLine("对于三角形" + objectIdString + "," + "三条边的唯一标志符号分别是: " + "edge1Key: " + edge1Key +
                                            "," + "edge2Key: " + edge2Key + "," + "edge3Key: " + edge3Key);
                            if (type == "Node")
                            {
                                if (all || !addedFeaturesHashSet.Contains(point1Key))
                                {
                                    // 添加point1到TinEdgeFeatureClass
                                    rowBuffer[facilitySiteDefinition.GetShapeField()] = points[2];
                                    using (Feature feature1 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature1);
                                    }
                                    addedFeaturesHashSet.Add(point1Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(point2Key))
                                {
                                    // 添加point2到TinEdgeFeatureClass
                                    rowBuffer[facilitySiteDefinition.GetShapeField()] = points[1];
                                    using (Feature feature2 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature2);
                                    }
                                    addedFeaturesHashSet.Add(point2Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(point3Key))
                                {
                                    // 添加point3到TinEdgeFeatureClass
                                    rowBuffer[facilitySiteDefinition.GetShapeField()] = points[0];
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
                                    // 添加edgeLine1到TinEdgeFeatureClass
                                    rowBuffer[facilitySiteDefinition.GetShapeField()] = edgeLine1;
                                    using (Feature feature1 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature1);
                                    }
                                    addedFeaturesHashSet.Add(edge1Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(edge2Key))
                                {
                                    // 添加edgeLine2到TinEdgeFeatureClass
                                    rowBuffer[facilitySiteDefinition.GetShapeField()] = edgeLine2;
                                    using (Feature feature2 = outputFeatureClass.CreateRow(rowBuffer))
                                    {
                                        //To Indicate that the attribute table has to be updated
                                        context.Invalidate(feature2);
                                    }
                                    addedFeaturesHashSet.Add(edge2Key);
                                }

                                if (all || !addedFeaturesHashSet.Contains(edge3Key))
                                {
                                    // 添加edgeLine3到TinEdgeFeatureClass
                                    rowBuffer[facilitySiteDefinition.GetShapeField()] = edgeLine3;
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


}
