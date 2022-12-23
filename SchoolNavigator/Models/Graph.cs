using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SchoolNavigator.Models
{
    public class Graph
    {
        public Vertice[]? Vertices;
        public Path[]? Paths;
        public Location[]? Locations;
        public double[][]? PathWeights;

        public void InitializePathWeights()
        {
            // 二维数组最大值为所有节点数量（包括无名节点 vertice 和景点 location）
            int size = Locations.Length + Vertices.Length; // 91
            PathWeights = new double[size][];

            // 将整个二维数组初始化为0
            for (int i = 0; i < size; ++i)
            {
                PathWeights[i] = new double[size];
            }

            // 通过 path 找两端节点，给节点对应的数组 index 赋值。
            // 通过 path 遍历，比通过节点遍历更快，从 O(N3) 降低到 O(N)
            foreach (var path in Paths)
            {
                int start = path.StartVerticeId;
                int end = path.EndVerticeId;

                // Debug 时断言：保证路径两端节点在数组中初始为0，这样就不会出现覆盖。
                Debug.Assert(PathWeights[start][end] == 0, $"i={start}, j={end}, value={PathWeights[start][end]}");

                // 如果路径被禁用，则不会写入权重值。
                if (path.IsEnabled == false) continue;

                // 将对应的两个节点都赋值。
                PathWeights[start][end] = path.Distance;
                PathWeights[end][start] = path.Distance;
            }
        }

        public Tuple<double[], int[]> Dijkstra(int startIndex)
        {
            if (PathWeights == null || PathWeights.Length == 0)
                throw new ArgumentNullException($"没有足够的节点。");

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException($"起始位置 {startIndex} 不合法。");

            // 定义接受最小距离的数组 distance 和前驱节点数组 preVertices。它们的 index 对应 PathWeights 一维或二维所代表的元素。
            int n = PathWeights.Length;
            double[] distance = new double[n];
            int[] preVertices = new int[n];

            // 初始化
            for (int i = 0; i < n; ++i)
            {
                // 将所有 distance 都置为 int 最大值
                distance[i] = double.MaxValue;
                // 将所有前驱节点 preVertices 都置为 -1，即 startIndex。
                preVertices[i] = -1;
            }

            // 定义用于求最小值的优先队列 PriorityQueue。减少时间复杂度。注意：第二个 int 才是权值（与C++的pair<int, int>相反）。
            var queue = new PriorityQueue<int, double>();
            queue.Enqueue(startIndex, 0);
            distance[startIndex] = 0;

            // 不断循环，直至优先队列中元素清空
            while (queue.Count != 0)
            {
                // 取出优先队列的首元素，这是当前的最短路径
                int u = queue.Dequeue();
                for (int v = 0; v < n; ++v)
                {
                    // 如果节点是自己，或者节点与 startIndex 节点没有边连接在一起，则把对应的 PathWeights[u][v] 和 PathWeights[v][u] 置为 0
                    if (PathWeights[u][v] != 0 && distance[v] > distance[u] + PathWeights[u][v])
                    {
                        // 进入 if 语句则代表找到了更短的路径，故更新 distance。
                        distance[v] = distance[u] + PathWeights[u][v];

                        // 同理，更新前驱节点 preVertices。
                        preVertices[v] = u;

                        // 将当前最短路径插入优先队列
                        queue.Enqueue(v, distance[v]);
                    }
                }
            }

            // 两个数组的 index 对应传入 PathWeights 的一行或一列。可以通过前驱节点 preVertices 逐渐获得最短路径经过的所有节点。
            // 当起点 startIndex 相同而终点不同时，无需重复调用 Dijkstra 方法进行计算。
            return new Tuple<double[], int[]>(distance, preVertices);
        }
    }


    public class Vertice
    {
        public int Id;
        public string? Name;
        public double X;
        public double Y;
    }

    public class Location : Vertice
    {
        public string? Info;
        public int[]? LocatedVerticeIds;
        public int[]? LocatedPathIds;
    }


    public class Path
    {
        public int Id;
        public string? Name;
        public double Distance;
        public int StartVerticeId;
        public int EndVerticeId;
        public string? Data;
        public bool IsEnabled;
    }
}