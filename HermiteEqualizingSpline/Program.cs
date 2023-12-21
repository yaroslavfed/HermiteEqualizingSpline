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
    private static readonly double s_betta = 0.01;

    private static readonly int s_finiteElementsCount = 2;

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

        //TODO: переписать рассчёт вектора b (сейчас считает только для одного конечного элемента)
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
                var splitQ = q.Skip(subIndex).Take(2 * (elementsCount + 1)).ToList();
                var item = splitQ.Select((sq, j) => sq * GetBasicFunctions(j, i, firstItem, lastItem - firstItem)).Sum();

                result.Add(new Node { X = i, Y = item });
            }
        }

        return result;
    }

    static IList<IList<double>> GetAMatrix()
    {
        var matrixList = SetLocalMatrices();
        var matrixSize = 2 * (s_inputData.Elements.Count + 1);

        double[,] globalMatrixArray = new double[matrixSize, matrixSize];
        for (int i = 0; i < matrixSize; i++)
        {
            for (int j = 0; j < matrixSize; j++)
            {
                globalMatrixArray[i, j] = 0;
            }
        }

        for (int k = 0; k < matrixList.Count; k++)
        {
            for (int i = 0; i < matrixList[k].Count; i++)
            {
                for (int j = 0; j < matrixList[k][i].Count; j++)
                {
                    globalMatrixArray[i + (k * 2), j + (k * 2)] = matrixList[k][i][j];
                }
            }
        }

        var globalMatrixList = new List<IList<double>>();
        for (int i = 0; i < matrixSize; i++)
        {
            var globalMatrixLine = new List<double>();
            for (int j = 0; j < matrixSize; j++)
            {
                globalMatrixLine.Add(globalMatrixArray[i, j]);
            }

            globalMatrixList.Add(globalMatrixLine);
        }

        Console.WriteLine(new Matrix(globalMatrixList));

        return globalMatrixList;
    }

    static IList<IList<IList<double>>> SetLocalMatrices()
    {
        var matrixList = new List<IList<IList<double>>>();
        foreach (var element in s_inputData.Elements)
        {
            var matrix = new List<IList<double>>(); // 2 * (s_inputData.Elements.Count + 1)
            for (int i = 0; i < 4; i++)
            {
                var line = new List<double>();
                for (int j = 0; j < 4; j++)
                {
                    var result = element.Nodes.Sum(node =>
                        s_weight
                        * GetBasicFunctions(
                            i,
                            node.X,
                            element.Nodes.FirstOrDefault()!.X,
                            element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)
                        * GetBasicFunctions(
                            j,
                            node.X,
                            element.Nodes.FirstOrDefault()!.X,
                            element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)
                    );

                    result += SupplementAlfa(element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)[i][j]
                              + SupplementBetta(element.Nodes.LastOrDefault()!.X -
                                                element.Nodes.FirstOrDefault()!.X)[i][j];

                    line.Add(result);
                }

                matrix.Add(line);
            }

            matrixList.Add(matrix);
        }

        return matrixList;
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
                double result = element.Nodes.Sum(node =>
                    s_weight
                    * node.Y
                    * GetBasicFunctions(
                        i,
                        node.X,
                        element.Nodes.FirstOrDefault()!.X,
                        element.Nodes.LastOrDefault()!.X - element.Nodes.FirstOrDefault()!.X)
                );

                fList.Add(result);
            }
        }

        return fList;
    }

    static List<double> Gauss(int n, IList<IList<double>> a, IList<double> b)
    {
        double[] x = new double[n];
        double r;

        for (int q = 0; q < n; q++)
        {
            r = 1 / a[q][q];
            a[q][q] = 1;
            for (int j = q + 1; j < n; j++)
                a[q][j] *= r;
            b[q] *= r;
            for (int k = q + 1; k < n; k++)
            {
                r = a[k][q];
                a[k][q] = 0;
                for (int j = q + 1; j < n; j++)
                    a[k][j] = a[k][j] - a[q][j] * r;
                b[k] -= b[q] * r;
            }
        }

        for (int q = n - 1; q >= 0; q--)
        {
            r = b[q];
            for (int j = q + 1; j < n; j++)
                r -= a[q][j] * x[j];
            x[q] = r;
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
        using var myProcess = new Process();
        myProcess.StartInfo.FileName = "python";
        myProcess.StartInfo.Arguments = @"script.py";
        myProcess.StartInfo.UseShellExecute = false;
        myProcess.StartInfo.RedirectStandardInput = true;
        myProcess.StartInfo.RedirectStandardOutput = false;
        myProcess.Start();

        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output.txt");
        using var sw = new StreamWriter(outputPath);

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