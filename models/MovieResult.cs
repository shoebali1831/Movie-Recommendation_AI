namespace MovieApi.Models;

public class MovieResult
{
    public string Title       { get; init; } = "";
    public string Genre       { get; init; } = "";
    public string Description { get; init; } = "";
    public double Score       { get; init; }
}
