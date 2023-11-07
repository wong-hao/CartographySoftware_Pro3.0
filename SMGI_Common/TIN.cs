using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
