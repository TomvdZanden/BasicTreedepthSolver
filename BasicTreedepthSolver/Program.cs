using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;

namespace BasicTreedepthSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            //MainTest(args);

            Graph g = Graph.ParsePACE2016(Console.In);
            g.Treedepth();
        }
    }

    public struct Edge
    {
        public int Weight;
        public Vertex From, To;

        public Edge(Vertex From, Vertex To) : this(From, To, 0)
        {

        }
        public Edge(Vertex From, Vertex To, int Weight)
        {
            this.From = From; this.To = To; this.Weight = Weight;
        }

        public override bool Equals(object obj)
        {
            Edge oth = (Edge)obj;
            return oth.Weight == this.Weight && ((oth.To == this.To && oth.From == this.From) || (oth.To == this.From && oth.From == this.To));
        }

        public override int GetHashCode()
        {
            if (From.Id < To.Id)
            {
                int hashCode = From.GetHashCode();
                hashCode = ((hashCode << 5) + hashCode) ^ To.GetHashCode();
                hashCode = ((hashCode << 5) + hashCode) ^ Weight.GetHashCode();
                return hashCode;
            }
            else
            {
                int hashCode = To.GetHashCode();
                hashCode = ((hashCode << 5) + hashCode) ^ From.GetHashCode();
                hashCode = ((hashCode << 5) + hashCode) ^ Weight.GetHashCode();
                return hashCode;
            }
        }
    }

    // Black magic from https://stackoverflow.com/a/109025/1327791
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BitCount(this int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }

    class Graph
    {
        public Vertex[] Vertices;
        public List<Edge> Edges;

        public static int UB = 0;
        public Graph(int n)
        {
            Vertices = new Vertex[n];
            for (int i = 0; i < n; i++)
                Vertices[i] = new Vertex(i);
            Edges = new List<Edge>();
        }

        Stopwatch sw = new Stopwatch();
        public void Treedepth()
        {
            sw.Start();
            for (UB = 1; UB <= Vertices.Length; UB++)
                if (TrySolve()) return;
        }

        public bool TrySolve()
        {
            PriorityQueue<PartialSolution> q = new PriorityQueue<PartialSolution>();
            HashSet<PartialSolution> processed = new HashSet<PartialSolution>();

            foreach (Vertex v in Vertices)
            {
                PartialSolution ps = new PartialSolution(this);
                ps = ps.AddVertex(v);
                if (ps == null) continue;
                q.Enqueue(ps, ps.DepthBelow);
            }

            while (q.Count > 0)// && sw.ElapsedMilliseconds < 1000 * 60 * 3)
            {
                PartialSolution ps = q.Dequeue();

                if (processed.Contains(ps)) continue;

                int count = ps.Vertices.Count() + ps.Separator.Count();

                if (count == Vertices.Length)
                {
                    //Console.WriteLine("Solution: " + ps.LowerBound);

                    //Console.WriteLine();

                    PartialSolution result = ps;
                    foreach (Vertex v in ps.Separator)
                    {
                        result = result.AddVertex(v);
                        throw new Exception("Unexpected condition");
                    }
                    result.Print();

                    return true;
                }

                foreach (Vertex v in ps.Separator)
                {
                    PartialSolution newPS = ps.AddVertex(v);
                    if (newPS == null) continue;
                    q.Enqueue(newPS, newPS.DepthBelow);

                    int scount = newPS.Separator.Count();
                    PartialSolution key = new PartialSolution(this);
                    key.VertexSubset = newPS.SeparatorSubset;
                }

                List<PartialSolution> toAdd = new List<PartialSolution>();

                foreach (PartialSolution mergeCandidate in processed)
                {
                    PartialSolution newPS = ps.Join(mergeCandidate);
                    if (newPS == null) continue;
                    toAdd.Add(newPS);
                }

                foreach (PartialSolution newPS in toAdd)
                {
                    q.Enqueue(newPS, newPS.DepthBelow);

                    int scount = newPS.Separator.Count();
                    PartialSolution key = new PartialSolution(this);
                    key.VertexSubset = newPS.SeparatorSubset;
                }

                processed.Add(ps);
            }

            /*if(sw.ElapsedMilliseconds >= 1000 * 60 * 3)
            {
                Console.WriteLine("Time out at: " + UB);

                return true;
            }*/

            return false;
        }

        public IEnumerable<Graph> ConnectedComponents()
        {
            int[] ccId = new int[Vertices.Length];
            int[] ccIdx = new int[Vertices.Length];
            Vertex[] cc = new Vertex[Vertices.Length];

            int ccCount = 0;
            foreach (Vertex v in Vertices)
            {
                if (ccId[v.Id] == 0)
                {
                    ccCount++;
                    int ccSize = 0;
                    Visit(v, ccCount, ref ccSize, ccId, ccIdx, cc);

                    Graph component = new Graph(ccSize);

                    for (int i = 0; i < ccSize; i++)
                    {
                        Vertex u = cc[i];
                        component.Vertices[ccIdx[u.Id]].OriginalLabel = u.OriginalLabel;

                        foreach (Edge e in u.Adj)
                        {
                            if (ccId[e.To.Id] == ccId[u.Id] && ccIdx[u.Id] < ccIdx[e.To.Id])
                                component.AddEdge(ccIdx[u.Id], ccIdx[e.To.Id], e.Weight);
                        }
                    }

                    yield return component;
                }
            }

            yield break;
        }

        public void Visit(Vertex v, int id, ref int idx, int[] ccId, int[] ccIdx, Vertex[] cc)
        {
            ccId[v.Id] = id;
            ccIdx[v.Id] = idx;
            cc[idx] = v;

            idx++;

            foreach (Vertex u in v.Adj.Select((e) => e.To))
            {
                if (ccId[u.Id] == 0)
                    Visit(u, id, ref idx, ccId, ccIdx, cc);
            }
        }

        // Parses a graph in PACE 2016/2017 format (https://pacechallenge.wordpress.com/pace-2016/track-a-treewidth/)
        public static Graph ParsePACE2016(TextReader sr)
        {
            Graph G = null;

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                string[] cf = line.Split();
                if (cf[0] == "p")
                    G = new Graph(int.Parse(cf[2]));
                if (G != null)
                {
                    try
                    {
                        int weight = 0;
                        if (cf.Length >= 3) int.TryParse(cf[2], out weight);
                        G.AddEdge(int.Parse(cf[0]) - 1, int.Parse(cf[1]) - 1, weight);
                    }
                    catch { }
                }
            }

            return G;
        }

        // Parses a graph in PACE 2018 format (https://pacechallenge.wordpress.com/pace-2018/)
        public static Graph ParsePACE2018(TextReader sr)
        {
            Graph G = null;

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                string[] cf = line.Split();
                if (cf[0] == "Nodes")
                    G = new Graph(int.Parse(cf[1]));
                if (cf[0] == "E" && G != null)
                {
                    try
                    {
                        int weight = 0;
                        int.TryParse(cf[3], out weight);
                        G.AddEdge(int.Parse(cf[1]) - 1, int.Parse(cf[2]) - 1, weight);
                    }
                    catch { }
                }
            }

            for (string line = sr.ReadLine(); line != "END" && line != null; line = sr.ReadLine())
            {
                // Terminals don't matter
            }

            return G;
        }

        public void AddEdge(int a, int b, int w)
        {
            Vertices[a].Adj.Add(new Edge(Vertices[a], Vertices[b], w));
            Vertices[b].Adj.Add(new Edge(Vertices[b], Vertices[a], w));

            if (a < b)
                Edges.Add(new Edge(Vertices[a], Vertices[b], w));
            else
                Edges.Add(new Edge(Vertices[b], Vertices[a], w));
        }
    }

    class PartialSolution
    {
        public Graph G;
        public int[] VertexSubset, SeparatorSubset;
        public int SeparatorSize, DepthBelow;

        // Data structure for representing solution:
        // Case 1: child is set --> this is a node in the TD containing vertex rootVertexID, with (possibly) a subtree contained in child
        // Case 2: left and right are set --> we have a binary tree consisting of partial solutions that are joined (i.e. the children of a single parent node)
        //public PartialSolution Child;
        public PartialSolution Left, Right;
        public int RootVertexID = -1;

        public void Print()
        {
            Vertex[] parents = new Vertex[G.Vertices.Length];
            Print(parents, null);

            Console.WriteLine(DepthBelow);

            foreach (Vertex v in G.Vertices.OrderBy((v) => v.OriginalLabel))
            {
                Vertex parent = parents[v.Id];
                if (parent == null)
                    Console.WriteLine("0");
                else
                    Console.WriteLine((parent.OriginalLabel + 1));
            }

            /*foreach (Vertex v in G.Vertices.OrderBy((v) => v.OriginalLabel))
            {
                Vertex parent = parents[v.Id];
                if (parent == null)
                    Console.WriteLine((v.OriginalLabel + 1) + "\t" + "0");
                else
                    Console.WriteLine((v.OriginalLabel + 1) + "\t" + (parent.OriginalLabel + 1));
            }*/
        }

        private void Print(Vertex[] parents, Vertex parent)
        {
            if (RootVertexID != -1)
            {
                parents[RootVertexID] = parent;
                parent = G.Vertices[RootVertexID];
            }

            /*if(Child != null)
            {
                Child.Print(parents, parent);
            }*/

            if (Left != null)
            {
                Left.Print(parents, parent);
            }

            if (Right != null)
            {
                Right.Print(parents, parent);
            }
        }

        public PartialSolution Join(PartialSolution other)
        {
            int newSepSize = 0;
            for (int i = 0; i < VertexSubset.Length; i++)
            {
                if ((this.VertexSubset[i] & other.VertexSubset[i]) != 0) return null;
                newSepSize += (this.SeparatorSubset[i] | other.SeparatorSubset[i]).BitCount();
            }

            if (newSepSize == SeparatorSize + other.SeparatorSize) return null;

            if (newSepSize + Math.Max(DepthBelow, other.DepthBelow) > Graph.UB) return null;

            PartialSolution result = new PartialSolution(other);

            result.Left = this;
            result.Right = other;

            // Copy subsets
            for (int i = 0; i < VertexSubset.Length; i++)
                result.VertexSubset[i] |= this.VertexSubset[i];

            for (int i = 0; i < SeparatorSubset.Length; i++)
                result.SeparatorSubset[i] |= this.SeparatorSubset[i];

            result.SeparatorSize = newSepSize;
            result.DepthBelow = Math.Max(DepthBelow, other.DepthBelow);

            foreach (Vertex v in result.Separator)
            {
                if (!v.Nb.Any((u) => !result.Contains(u) && !result.IsSep(u)))
                {
                    PartialSolution resultChild = new PartialSolution(result);
                    resultChild.RootVertexID = result.RootVertexID;
                    resultChild.Left = result.Left; resultChild.Right = result.Right;
                    result.Left = resultChild; result.Right = null;
                    result.RootVertexID = v.Id;

                    result.SeparatorSubset[v.Id / 32] &= ~(1 << (v.Id % 32));
                    result.VertexSubset[v.Id / 32] |= 1 << (v.Id % 32);
                    result.SeparatorSize--;
                    result.DepthBelow++;
                }
            }

            return result;
        }

        public PartialSolution(Graph G)
        {
            VertexSubset = new int[(G.Vertices.Length + 31) / 32];
            SeparatorSubset = new int[(G.Vertices.Length + 31) / 32];
            this.G = G;
        }

        public PartialSolution(PartialSolution parent)
        {
            G = parent.G;
            VertexSubset = (int[])parent.VertexSubset.Clone();
            SeparatorSubset = (int[])parent.SeparatorSubset.Clone();

            SeparatorSize = parent.SeparatorSize;
            DepthBelow = parent.DepthBelow;
        }
        public PartialSolution AddVertex(Vertex v)
        {
            PartialSolution result = new PartialSolution(this);

            if (Contains(v))
                return result;

            result.VertexSubset[v.Id / 32] |= (1 << (v.Id % 32));
            result.DepthBelow++;

            result.RootVertexID = v.Id;
            //result.Child = this;
            result.Left = this;

            if (IsSep(v))
            {
                result.SeparatorSubset[v.Id / 32] &= ~(1 << (v.Id % 32));
                result.SeparatorSize--;
            }

            foreach (Vertex u in v.Nb)
            {
                if (!Contains(u) && !IsSep(u))
                {
                    if (u.Adj.Count == 1 && v.Adj.Count != 1) return null;

                    result.SeparatorSize++;
                    result.SeparatorSubset[u.Id / 32] |= 1 << (u.Id % 32);
                }
            }

            if (result.LowerBound > Graph.UB) return null;

            return result;
        }

        public bool IsSep(Vertex v)
        {
            return (SeparatorSubset[v.Id / 32] & (1 << (v.Id % 32))) != 0;
        }
        public bool Contains(Vertex v)
        {
            return (VertexSubset[v.Id / 32] & (1 << (v.Id % 32))) != 0;
        }

        public IEnumerable<Vertex> Separator
        {
            get
            {
                foreach (Vertex v in G.Vertices)
                {
                    if (IsSep(v))
                        yield return v;
                }
            }
        }

        public IEnumerable<Vertex> Vertices
        {
            get
            {
                foreach (Vertex v in G.Vertices)
                {
                    if (Contains(v))
                        yield return v;
                }
            }
        }

        public int LowerBound
        {
            get
            {
                return SeparatorSize + DepthBelow;
            }
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (int i in VertexSubset)
            {
                //hash = HashCode.Combine(hash, i);
                int newHash = 17;
                newHash = newHash * 31 + hash;
                newHash = newHash * 31 + i;
                hash = newHash;
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PartialSolution)) return false;
            return ((PartialSolution)obj).VertexSubset.SequenceEqual(VertexSubset);
        }
    }

    public class Vertex
    {
        public int Color = -1; // Gives a (tw+1)-coloring of the graph
        public List<Edge> Adj = new List<Edge>();
        public int Id;
        public int Weight = 1;
        public int OriginalLabel;

        public Vertex(int Id) : this(Id, Id)
        {
        }

        public Vertex(int Id, int Label)
        {
            this.Id = Id;
            this.OriginalLabel = Label;
            Adj = new List<Edge>();
        }

        public override bool Equals(object obj)
        {
            Vertex oth = (Vertex)obj;
            return oth.Id == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public IEnumerable<Vertex> Nb
        {
            get
            {
                foreach (Edge e in Adj)
                    yield return e.To;
                yield break;
            }
        }
    }

    class PriorityQueue<T>
    {
        T[] heap;
        long[] priorities;
        int heapSize;

        public void print()
        {
            for (int i = 0; i < heapSize; i++)
                Console.Write(priorities[i]);
        }

        public PriorityQueue() : this(64) { }

        public PriorityQueue(int size)
        {
            heapSize = 0;
            heap = new T[size];
            priorities = new long[size];
        }

        public T Dequeue()
        {
            if (heapSize <= 0)
                throw new Exception("PQueue empty!");
            T result = heap[0];

            heapSize--;
            heap[0] = heap[heapSize];
            priorities[0] = priorities[heapSize];

            int k = 0;
            while (true)
            {
                int l = 2 * k + 1;
                int r = l + 1;

                if (r < heapSize && priorities[r] < priorities[k] && priorities[l] >= priorities[r])
                    Swap(k, (k = r));
                else if (l < heapSize && priorities[l] < priorities[k])
                    Swap(k, (k = l));
                else
                    break;
            }

            return result;
        }

        public T Peek()
        {
            return heap[0];
        }

        public long PeekDist()
        {
            return priorities[0];
        }

        public void Enqueue(T obj, long priority)
        {
            if (heapSize == heap.Length)
            {
                T[] newHeap = new T[heap.Length * 2];
                long[] newP = new long[heap.Length * 2];

                for (int i = 0; i < heap.Length; i++)
                {
                    newHeap[i] = heap[i];
                    newP[i] = priorities[i];
                }

                priorities = newP;
                heap = newHeap;
            }

            priorities[heapSize] = priority;
            heap[heapSize] = obj;
            int k = heapSize++;

            while (k > 0 && priorities[k] < priorities[(k + 1) / 2 - 1])
                Swap(k, (k = (k + 1) / 2 - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int a, int b)
        {
            long oldP = priorities[a];
            T oldO = heap[a];
            heap[a] = heap[b];
            priorities[a] = priorities[b];
            heap[b] = oldO;
            priorities[b] = oldP;
        }

        public bool IsEmpty()
        {
            return heapSize == 0;
        }

        public int Count
        {
            get
            {
                return heapSize;
            }
        }
    }
}