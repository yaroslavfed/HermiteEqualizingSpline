using System.Text.Json;
using System.Diagnostics;
using static System.Math;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        var elements = JsonSerializer.Deserialize<FiniteElement>(jsonString)!;

        var area = new Area()
        {
            Elements = new List<FiniteElement>
            {
                elements
            }
        };

        return area;
    }

    static Area ResolveSpline()
    {
        var tempList = new List<double>()
        {
            2.5, -0.0117, 2.48, -0.0673, 0.0488, -3.35
        };
        var globalStep = 0.01;


        var finitElement = s_inputData.Elements.FirstOrDefault();
        var nodes = finitElement!.Nodes;

        var newElements = new List<FiniteElement>();
        var newNodes = new List<Node>();
        for (double i = nodes.FirstOrDefault()!.X; i < nodes.LastOrDefault()!.X; i += globalStep)
        {
            double item = 0;
            for (int j = 0; j < 2 * s_inputData.Elements.Count; j++)
            {
                item += tempList[j] * GetBasicFunctions(j, i, nodes.FirstOrDefault()!.X,
                    nodes.LastOrDefault()!.X - nodes.FirstOrDefault()!.X);
            }

            var node = new Node()
            {
                X = i,
                Y = item
            };
            newNodes.Add(node);
        }

        var element = new FiniteElement()
        {
            Nodes = newNodes
        };
        newElements.Add(element);
        var area = new Area()
        {
            Elements = newElements
        };

        return area;
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
        //return 1 - (3 * Pow(eps, 2)) + (2 * Pow(eps, 3));
    }

    private static void DrawPlot()
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

            sw.WriteLine();
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

            sw.WriteLine();
            foreach (var point in element.Nodes)
            {
                sw.Write(point.Y + " ");
            }
        }

        sw.WriteLine();
    }

    #endregion
}