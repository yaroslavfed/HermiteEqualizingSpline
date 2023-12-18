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

    private static double weight = 1;
    private static double a = 0;
    private static double b = 0.001;

    private static IList<Node> list = new List<Node>()
    {
        new() { X = 0, Y = 10.1 },
        new() { X = 0.1, Y = 9.4 },
        new() { X = 0.2, Y = 11.2 },
        new() { X = 0.3, Y = 8.7 },
        new() { X = 0.4, Y = 10.6 },
        new() { X = 0.5, Y = 9.2 },
        new() { X = 0.6, Y = 10.8 },
        new() { X = 0.7, Y = 8.5 },
        new() { X = 0.8, Y = 9.5 },
        new() { X = 0.9, Y = 9.2 },
        new() { X = 1, Y = 7.4 },
        new() { X = 1.1, Y = 7.2 },
        new() { X = 1.2, Y = 5.5 },
        new() { X = 1.3, Y = 5.5 },
        new() { X = 1.4, Y = 4.1 },
        new() { X = 1.5, Y = 3.7 },
        new() { X = 1.6, Y = 2.2 },
        new() { X = 1.7, Y = 1.9 },
        new() { X = 1.8, Y = 1.4 },
        new() { X = 1.9, Y = 1.3 },
        new() { X = 2, Y = 1.15 },
        new() { X = 2.1, Y = 0.85 },
        new() { X = 2.2, Y = 1.2 },
        new() { X = 2.3, Y = 1.02 },
        new() { X = 2.4, Y = 0.8 },
        new() { X = 2.5, Y = 1 },
    };

    #endregion

    #region LifeCycle

    static void Main(string[] args)
    {
        s_inputData = ReadDataFile();
        s_outputData = ResolveSpline();

        DrawPlot();
    }

    #endregion

    #region Methods

    static Area ReadDataFile()
    {
        string fileName = s_configurationString;
        string jsonString = File.ReadAllText(fileName);

        var nodeList = JsonSerializer.Deserialize<FiniteElement>(jsonString)!;

        var elements = new List<FiniteElement>();
        for (int i = 0; i < nodeList.Nodes.Count; i += 3)
        {
            var element = new FiniteElement
            {
                Nodes = nodeList.Nodes.Skip(i).Take(4).ToList()
            };
            elements.Add(element);
        }

        return new Area
        {
            Elements = elements
        };
    }

    static Area ResolveSpline()
    {
        var A = GetAMatrix();
        var b = GetBVector();

        var q = Gauss(2, A, b);
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

        for (double i = list.FirstOrDefault()!.X; i < list.LastOrDefault()!.X; i += 0.01)
        {
            double item = 0;
            for (int j = 0; j < q.Count; j++)
            {
                item += q[j] * GetBasicFunctions(j, i, list.FirstOrDefault()!.X,
                    list.LastOrDefault()!.X - list.FirstOrDefault()!.X);
            }

            result.Add(
                new Node
                {
                    X = i,
                    Y = item
                });
        }

        return result;
    }

    static IList<IList<double>> GetAMatrix()
    {
        var iList = new List<IList<double>>();
        for (int i = 0; i < 2; i++)
        {
            var jList = new List<double>();
            for (int j = 0; j < 2; j++)
            {
                double result = 0;
                for (int k = 0; k < list.Count; k++)
                {
                    result += weight
                              * GetBasicFunctions(i,
                                  list[k].X, list.FirstOrDefault()!.X,
                                  list.LastOrDefault()!.X - list.FirstOrDefault()!.X)
                              * GetBasicFunctions(j,
                                  list[k].X, list.FirstOrDefault()!.X,
                                  list.LastOrDefault()!.X - list.FirstOrDefault()!.X);
                }

                jList.Add(result);
            }

            iList.Add(jList);
        }

        return iList;
    }

    static IList<double> GetBVector()
    {
        var fList = new List<double>();
        for (int i = 0; i < 2; i++)
        {
            double result = 0;
            for (int k = 0; k < list.Count; k++)
            {
                result += weight * list[k].Y * GetBasicFunctions(i,
                    list[k].X, list.FirstOrDefault()!.X,
                    list.LastOrDefault()!.X - list.FirstOrDefault()!.X);
            }

            fList.Add(result);
        }

        return fList;
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

    #endregion
}