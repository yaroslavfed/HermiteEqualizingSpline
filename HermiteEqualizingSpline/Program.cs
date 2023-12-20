using System.Text.Json;
using System.Diagnostics;
using static System.Math;

namespace HermiteEqualizingSpline;

internal class Program
{
    #region Fields

    private static readonly string s_configurationString = "data.json";

    private static Area s_inputData = new();
    private static Area s_outputData = new();

    private static readonly double s_weight = 1;
    private static readonly double s_alfa = 0;
    private static readonly double s_betta = 0.001;

    private static readonly int s_finiteElementsCount = 1;

    #endregion

    #region LifeCycle

    static void Main(string[] args)
    {
        s_inputData = SplitAreaToFiniteElements();
        s_outputData = ResolveSpline();

        DrawPlot();
    }

    #endregion

    #region Methods

    static Area SplitAreaToFiniteElements()
    {
        var nodesList = ReadDataFile();
        var nodesRange = Math.Ceiling((double)nodesList.Count / (double)s_finiteElementsCount);

        var elements = new List<FiniteElement>();
        for (int i = 0; i < nodesList.Count; i += (int)nodesRange - 1)
        {
            var temp = nodesList.Skip(i).Take((int)nodesRange).ToList();
            var element = new FiniteElement
            {
                Nodes = temp
            };
            elements.Add(element);
        }

        return new Area
        {
            Elements = elements.SkipLast(1).ToList()
        };
    }

    static IList<Node> ReadDataFile()
    {
        var fileName = s_configurationString;
        var jsonString = File.ReadAllText(fileName);
        var data = JsonSerializer.Deserialize<FiniteElement>(jsonString) ?? new FiniteElement();

        return data.Nodes;
    }

    static Area ResolveSpline()
    {
        var A = GetAMatrix();
        var b = GetBVector();

        var q = Gauss(A.Count, A, b);
        var S = GetSpline(q);


        var area = new Area()
        {
            Elements = new List<FiniteElement>()
            {
                new()
                {
                    Nodes = S
                }
            }
        };

        return area;
    }

    static IList<Node> GetSpline(IList<double> q)
    {
        IList<Node> result = new List<Node>();
        var elementsCount = s_inputData.Elements.Count;

        for (var index = 0; index < elementsCount; index++)
        {
            var subIndex = index * 2;

            var element = s_inputData.Elements[index];
            var firstItem = element.Nodes.FirstOrDefault()!.X;
            var lastItem = element.Nodes.LastOrDefault()!.X;
            for (var i = firstItem; i < lastItem; i += 0.01)
            {
                double item = 0;
                var splitP = q.Skip(subIndex).Take(2 * (elementsCount + 1)).ToList();
                for (int j = 0; j < splitP.Count; j++)
                {
                    item += splitP[j]
                            * GetBasicFunctions(
                                j,
                                i,
                                firstItem,
                                lastItem - firstItem);
                }

                result.Add(new Node { X = i, Y = item });
            }
        }

        return result;
    }

    static IList<IList<double>> GetAMatrix()
    {
        var iList = new List<IList<double>>();
        for (int i = 0; i < 2 * (s_inputData.Elements.Count + 1); i++)
        {
            var jList = new List<double>();
            for (int j = 0; j < 2 * (s_inputData.Elements.Count + 1); j++)
            {
                foreach (var element in s_inputData.Elements)
                {
                    double result = 0;
                    foreach (var node in element.Nodes)
                    {
                        result += s_weight
                                  * GetBasicFunctions(i,
                                      node.X, element.Nodes.FirstOrDefault()!.X,
                                      element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)
                                  * GetBasicFunctions(j,
                                      node.X, element.Nodes.FirstOrDefault()!.X,
                                      element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)
                                  + SupplementAlfa(element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)[i][j]
                                  + SupplementBetta(element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)[i][j];
                    }

                    jList.Add(result);
                }
            }

            iList.Add(jList);
        }

        return iList;
    }

    static IList<IList<double>> SupplementAlfa(double h)
    {
        var matrix = new List<IList<double>>
        {
            new List<double> { 36, 3 * h, -36, 3 * h },
            new List<double> { 3 * h, 4 * Pow(h, 2), -3 * h, -1 * Pow(h, 2) },
            new List<double> { -36, -3 * h, 36, -3 * h },
            new List<double> { 3 * h, -1 * Pow(h, 2), -3 * h, 4 * Pow(h, 2) }
        };

        foreach (var line in matrix)
        {
            for (var i = 0; i < line.Count; i++)
            {
                line[i] *= s_alfa / (30 * h);
            }
        }

        return matrix;
    }

    static IList<IList<double>> SupplementBetta(double h)
    {
        var matrix = new List<IList<double>>
        {
            new List<double> { 60, 30 * h, -60, 30 * h },
            new List<double> { 30 * h, 16 * Pow(h, 2), -30 * h, 14 * Pow(h, 2) },
            new List<double> { -60, -30 * h, 60, -30 * h },
            new List<double> { 30 * h, 14 * Pow(h, 2), -30 * h, 16 * Pow(h, 2) }
        };

        foreach (var line in matrix)
        {
            for (var i = 0; i < line.Count; i++)
            {
                line[i] *= s_betta / Pow(h, 3);
            }
        }

        return matrix;
    }

    static IList<double> GetBVector()
    {
        var fList = new List<double>();
        foreach (var element in s_inputData.Elements)
        {
            for (int i = 0; i < 2 * (s_inputData.Elements.Count + 1); i++)
            {
                double result = 0;
                foreach (var node in element.Nodes)
                {
                    result += s_weight * node.Y * GetBasicFunctions(i,
                        node.X, element.Nodes.FirstOrDefault()!.X,
                        element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X);
                }

                fList.Add(result);
            }
        }

        return fList;
    }

    static List<double> Gauss(int N, IList<IList<double>> B, IList<double> RightPart)
    {
        double[] x = new double[N];
        double R;

        for (int q = 0; q < N; q++)
        {
            R = 1 / B[q][q];
            B[q][q] = 1;
            for (int j = q + 1; j < N; j++)
                B[q][j] *= R;
            RightPart[q] *= R;
            for (int k = q + 1; k < N; k++)
            {
                R = B[k][q];
                B[k][q] = 0;
                for (int j = q + 1; j < N; j++)
                    B[k][j] = B[k][j] - B[q][j] * R;
                RightPart[k] -= RightPart[q] * R;
            }
        }

        for (int q = N - 1; q >= 0; q--)
        {
            R = RightPart[q];
            for (int j = q + 1; j < N; j++)
                R -= B[q][j] * x[j];
            x[q] = R;
        }

        return x.ToList();
    }

    static double GetBasicFunctions(int i, double x, double xi, double h)
    {
        var eps = (x - xi) / h;

        return i switch
        {
            0 => 1 - (3 * Pow(eps, 2)) + (2 * Pow(eps, 3)),
            1 => h * (eps - 2 * Pow(eps, 2) + Pow(eps, 3)),
            2 => (3 * Pow(eps, 2)) - (2 * Pow(eps, 3)),
            3 => h * (-Pow(eps, 2) + Pow(eps, 3)),
            _ => 0
        };
    }

    static void DrawPlot()
    {
        using Process myProcess = new Process();
        myProcess.StartInfo.FileName = "python";
        myProcess.StartInfo.Arguments = @"script.py";
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardInput = true;
        myProcess.StartInfo.RedirectStandardOutput = false;
        myProcess.Start();

        string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output.txt");
        using StreamWriter sw = new StreamWriter(outputPath);

        foreach (var element in s_inputData.Elements)
        {
            foreach (var point in element.Nodes)
            {
                sw.Write(point.X + " ");
            }
        }

        sw.WriteLine();

        foreach (var element in s_inputData.Elements)
        {
            foreach (var point in element.Nodes)
            {
                sw.Write(point.Y + " ");
            }
        }

        sw.WriteLine();

        foreach (var element in s_outputData.Elements)
        {
            foreach (var point in element.Nodes)
            {
                sw.Write(point.X + " ");
            }
        }

        sw.WriteLine();

        foreach (var element in s_outputData.Elements)
        {
            foreach (var point in element.Nodes)
            {
                sw.Write(point.Y + " ");
            }
        }

        sw.WriteLine();
    }

    #endregion
}