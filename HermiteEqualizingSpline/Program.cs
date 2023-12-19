using System.Text.Json;
using System.Diagnostics;
using static System.Math;
using System.Runtime.ConstrainedExecution;

namespace HermiteEqualizingSpline;

internal class Program
{
    #region Fields

    private static readonly string s_configurationString = "data.json";

    private static Area s_inputData = new();
    private static Area s_outputData = new();

    private static readonly double s_weight = 1;
    private static readonly int s_finiteElementsCount = 5;
    private static readonly int s_nodesPerElementCount = 4;

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
        var nodesCount = s_finiteElementsCount <= 1 ? s_nodesPerElementCount : nodesList.Count;

        var elements = new List<FiniteElement>();
        for (int i = 0; i < s_finiteElementsCount; i += nodesList.Count - 1)
        {
            var temp = nodesList.Skip(i).Take(s_nodesPerElementCount).ToList();
            var element = new FiniteElement
            {
                Nodes = temp
            };
            elements.Add(element);
        }

        return new Area
        {
            Elements = elements
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
        var node = new Node();

        foreach (var element in s_inputData.Elements)
        {
            for (double i = element.Nodes.FirstOrDefault()!.X; i < element.Nodes.LastOrDefault()!.X; i += 0.01)
            {
                double item = 0;
                for (int j = 0; j < q.Count; j++)
                {
                    item += q[j] * GetBasicFunctions(j, i, element.Nodes.FirstOrDefault()!.X,
                        element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X);
                }

                result.Add(
                    new Node
                    {
                        X = i,
                        Y = item
                    });
            }
        }

        return result;
    }

    static IList<IList<double>> GetAMatrix()
    {
        var iList = new List<IList<double>>();
        foreach (var element in s_inputData.Elements)
        {
            for (int i = 0; i < 2 * s_inputData.Elements.Count; i++)
            {
                var jList = new List<double>();
                for (int j = 0; j < 2 * s_inputData.Elements.Count; j++)
                {
                    double result = 0;
                    for (int k = 0; k < element.Nodes.Count; k++)
                    {
                        result += s_weight
                                  * GetBasicFunctions(i,
                                      element.Nodes[k].X, element.Nodes.FirstOrDefault()!.X,
                                      element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)
                                  * GetBasicFunctions(j,
                                      element.Nodes[k].X, element.Nodes.FirstOrDefault()!.X,
                                      element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X);
                    }

                    jList.Add(result);
                }

                iList.Add(jList);
            }
        }

        return iList;
    }

    static IList<double> GetBVector()
    {
        var fList = new List<double>();
        foreach (var element in s_inputData.Elements)
        {
            for (int i = 0; i < 2 * s_inputData.Elements.Count; i++)
            {
                double result = 0;
                for (int k = 0; k < element.Nodes.Count; k++)
                {
                    result += s_weight * element.Nodes[k].Y * GetBasicFunctions(i,
                        element.Nodes[k].X, element.Nodes.FirstOrDefault()!.X,
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

        // Прямой ход
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

        // Обратный ход
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

        switch (i)
        {
            case 0:
                return 1 - (3 * Pow(eps, 2)) + (2 * Pow(eps, 3));
            case 1:
                return h * (eps - 2 * Pow(eps, 2) + Pow(eps, 3));
            case 2:
                return (3 * Pow(eps, 2)) - (2 * Pow(eps, 3));
            case 3:
                return h * (-Pow(eps, 2) + Pow(eps, 3));
            default:
                return 0;
        }
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