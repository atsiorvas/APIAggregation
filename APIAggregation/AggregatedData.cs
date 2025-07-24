namespace APIAggregation;

public class AggregatedData
{
    public string Source { get; set; }

    public AggregatedData(string source, object data)
    {
        (Source, _) = (source, data);
    }
}