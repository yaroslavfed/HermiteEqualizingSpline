namespace HermiteEqualizingSpline;

public class FiniteElement
{
    public IList<Node> Nodes { get; set; } = Array.Empty<Node>();

    public override string ToString()
    {
        return Nodes.Aggregate(string.Empty, (current, item) => current + (item + "\n"));
    }
}