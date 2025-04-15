using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace Algoritm4Colors
{
    internal class Program
    {
        static int MAX_VERTICES = 10000000;
        static double REMOVAL_CHANCE;
        static Random random = new Random();

        public static void Main(string[] args)
        {

            Stopwatch stopwatch = new Stopwatch();
            List<List<int>> loadedGraph = LoadGraphFromFileJson("graph.json");
            if (loadedGraph==null)
            {
                loadedGraph = LoadGraphFromFileTxt("graph.txt");
                if (loadedGraph == null)
                {


                    stopwatch.Start();
                    List<List<int>> graph = GenerateGraph();
                    stopwatch.Stop();
                    Console.WriteLine("Граф сгенерирован");
                    Console.WriteLine("Количество вершин = " + graph.Count);
                    Console.WriteLine("Количество рёбер = " + CountEdges(graph));
                    Console.WriteLine("Затраченное время = " + stopwatch.Elapsed.TotalSeconds + " секунд");
                    SaveGraphToFileTxt(graph, "graph.txt");
                    SaveGraphToFileJson(graph, "graph.json");
                    stopwatch.Restart();
                    loadedGraph = graph;
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            double averageTime = 0;
            int test_count = 1;
            for (int i = 0;i!= test_count;i++)
            {
                REMOVAL_CHANCE = random.NextDouble();
                List<List<int>> test_graph = loadedGraph.Select(neighbors => new List<int>(neighbors)).ToList();
                int removed = RemoveRandomEdges(test_graph);
                Console.Write("Удалено " + removed + " ребер с шансом " + REMOVAL_CHANCE);
                stopwatch.Start();
                var colors = ColorGraph(test_graph);
                stopwatch.Stop();
                Console.WriteLine(" Время работы алгоритма = " + stopwatch.Elapsed.TotalSeconds + " секунд");
                averageTime += stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
            }
            Console.WriteLine("Среднее время работы алгоритма = " + averageTime / test_count);


            List<int> output = ColorGraph(loadedGraph);

            File.WriteAllLines("output.txt", output.Select((color, index) => $"{index} - {color}").ToArray());

            Console.WriteLine($"Цвета сохранены в файл: output.txt");
        }

        static List<List<int>> GenerateGraph()
        {
            List<List<int>> graph = CreateBaseTriangle();
            var seenTriangles = new HashSet<string>();

            while (graph.Count <= MAX_VERTICES)
            {
                List<List<int>> triangles = FindTriangles(graph, seenTriangles);
                foreach (List<int> triangle in triangles)
                {
                    int newNode = graph.Count;
                    graph[triangle[0]].Add(newNode);
                    graph[triangle[1]].Add(newNode);
                    graph[triangle[2]].Add(newNode);
                    graph.Add(new List<int> { triangle[0], triangle[1], triangle[2] });

                    if (graph.Count >= MAX_VERTICES)
                        return graph;
                }
            }

            return graph;
        }

        static List<List<int>> CreateBaseTriangle()
        {
            return new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { 0, 2 },
                new List<int> { 0, 1 }
            };
        }

        static List<List<int>> FindTriangles(List<List<int>> graph, HashSet<string> seenTriangles)
        {
            var triangles = new List<List<int>>();
            int n = graph.Count;

            for (int u = 0; u < n; u++)
            {
                var neighborsU = graph[u];

                for (int i = 0; i < neighborsU.Count; i++)
                {
                    int v = neighborsU[i];

                    foreach (int w in graph[v])
                    {
                        if (u < v && v < w && graph[u].Contains(w))
                        {
                            var triangle = new List<int> { u, v, w };
                            string triangleKey = string.Join(",", triangle);

                            if (seenTriangles.Add(triangleKey))
                            {
                                triangles.Add(triangle);
                            }
                        }
                    }
                }
            }

            return triangles;
        }

        private static bool IsGraphContainsTriangle(List<List<int>> graph, List<int> triangle, int newNode)
        {
            if (triangle.Any(node => node < 0 || node >= graph.Count))
            {
                return false;
            }
            int firstNode = triangle[0];
            int secondNode = triangle[1];
            int thirdNode = triangle[2];
            return graph[firstNode].Contains(newNode) && graph[secondNode].Contains(newNode) && graph[thirdNode].Contains(newNode);
        }


        static List<int> ColorGraph(List<List<int>> graph)
        {
            int n = graph.Count;
            List<int> colors = new List<int>(new int[n]); 
            for (int u = 0; u < n; u++)
            {
                HashSet<int> neighborColors = new HashSet<int>();
                foreach (int neighbor in graph[u])
                {
                    if (colors[neighbor] != 0)
                    {
                        neighborColors.Add(colors[neighbor]);
                    }
                }
                for (int color = 1; color <= 4; color++)
                {
                    if (!neighborColors.Contains(color))
                    {
                        colors[u] = color;
                        break;
                    }
                }
            }
            return colors;
        }

        static int RemoveRandomEdges(List<List<int>> graph)
        {
            int count = 0;
            Random rand = new Random();
            for (int u = 0; u < graph.Count; u++)
            { 
                for (int i = 0; i < graph[u].Count; i++)
                {
                    int v = graph[u][i];
                    if (rand.NextDouble() < REMOVAL_CHANCE)
                    {
                        graph[u].Remove(v);
                        graph[v].Remove(u); 
                        i--; 
                        count++;
                    }
                }
            }
            return count;
        }

        static int CountEdges(List<List<int>> graph)
        {
            int edgeCount = 0;

            for (int i = 0; i < graph.Count; i++)
            {
                edgeCount += graph[i].Count; 
            }

            return edgeCount / 2;
        }

        static void WriteGraph(List<List<int>> graph)
        {
            for (int i = 0; i < graph.Count; i++)
            {
                Console.Write($"Вершина {i}: ");

                for (int j = 0; j < graph[i].Count; j++)
                {
                    Console.Write(graph[i][j] + " ");
                }

                Console.WriteLine();
            }
        }

        static void SaveGraphToFileTxt(List<List<int>> graph, string fileName)
        {
            var lines = new List<string>();

            for (int i = 0; i < graph.Count; i++)
            {
                foreach (int neighbor in graph[i])
                {
                    if (i < neighbor) // Сохраняем только уникальные пары
                    {
                        lines.Add($"{i}-{neighbor}");
                    }
                }
            }

            File.WriteAllLines(fileName, lines);
            Console.WriteLine($"Граф сохранен в файл: {fileName}");
        }

        static List<List<int>> LoadGraphFromFileTxt(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Файл не найден. Генерируем новый граф.");
                return null;
            }

            var graph = new List<List<int>>();
            var lines = File.ReadAllLines(fileName);

            foreach (var line in lines)
            {
                var parts = line.Split('-');
                if (parts.Length == 2)
                {
                    int node1 = int.Parse(parts[0]);
                    int node2 = int.Parse(parts[1]);

                    while (graph.Count <= Math.Max(node1, node2))
                    {
                        graph.Add(new List<int>());
                    }

                    // Добавляем ребра
                    graph[node1].Add(node2);
                    graph[node2].Add(node1);
                }
            }

            Console.WriteLine("Граф загружен из файла.");
            Console.WriteLine("Граф содержит = " + graph.Count + " вершин");
            Console.WriteLine("Количество рёбер = " + CountEdges(graph));
            return graph;
        }



        static void SaveGraphToFileJson(List<List<int>> graph, string fileName)
        {
            string json = JsonConvert.SerializeObject(graph, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(fileName, json);
            Console.WriteLine("Граф сохранен в файл.");
        }
        static List<List<int>> LoadGraphFromFileJson(string fileName)
        {
            if (File.Exists(fileName))
            {
                string json = File.ReadAllText(fileName);
                List<List<int>> graph = JsonConvert.DeserializeObject<List<List<int>>>(json);
                Console.WriteLine("Граф загружен из файла.");
                Console.WriteLine("Граф содержит = " + graph.Count + " вершин");
                Console.WriteLine("Количество рёбер = " + CountEdges(graph));
                return graph;
            }
            else
            {
                Console.WriteLine("Файл не найден.\nГенерируем свой файл");
                return null;
            }
        }

    }
}
