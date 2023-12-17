namespace HermiteEqualizingSpline;

internal class Area
{
    public IList<FiniteElement> Elements { get; set; } = Array.Empty<FiniteElement>();

    public override string ToString()
    {
        return Elements.Aggregate(string.Empty, (current, item) => current + (item + "\n"));
    }
}