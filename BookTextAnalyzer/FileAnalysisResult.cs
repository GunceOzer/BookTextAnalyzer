namespace BookTextAnalyzer;

public class FileAnalysisResult
{
    public List<string> LongestSentences { get; set; } = new List<string>();
    public List<string> ShortestSentences { get; set; } = new List<string>();
    public List<string> LongestWords { get; set; } = new List<string>();
    public IEnumerable<IGrouping<char, char>> MostCommonLetters { get; set; }
    public Dictionary<string, int> WordFrequencyTop10 { get; set; } = new Dictionary<string, int>();
}