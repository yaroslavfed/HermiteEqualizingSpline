using static System.Environment;

namespace HermiteEqualizingSpline;

using System.Text;

public struct Matrix
{
    #region Fields

    private readonly IList<IList<double>> m_matrix;

    #endregion

    #region LifeCycle

    public Matrix(IList<IList<double>> matrix)
    {
        m_matrix = matrix;
    }

    #endregion

    #region Operators

    public static Matrix operator +(Matrix a) => a;
    public static Matrix operator -(Matrix a)
    {
        foreach (var line in a.m_matrix)
        {
            for (int j = 0; j < line.Count; j++)
            {
                line[j] *= -1;
            }
        }

        return a;
    }
    public static Matrix operator +(Matrix a, Matrix b)
    {
        var c = new List<IList<double>>();
        for (int i = 0; i < a.m_matrix.Count; i++)
        {
            var cLine = new List<double>();
            for (int j = 0; j < a.m_matrix[i].Count; j++)
            {
                cLine.Add(a.m_matrix[i][j] + b.m_matrix[i][j]);
            }
            c.Add(cLine);
        }

        return new Matrix(c);
    }

    #endregion

    #region Methods

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var line in m_matrix)
        {
            foreach (var item in line)
            {
                sb.Append($"{item:N3}\t");
            }
            sb.Append(NewLine);
        }

        return sb.ToString();
    }

    #endregion
}