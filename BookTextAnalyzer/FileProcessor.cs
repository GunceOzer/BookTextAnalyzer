using System.Text.RegularExpressions;

namespace BookTextAnalyzer;

public class FileProcessor
{
    private readonly string _filePath;

    public FileProcessor(string filePath)
    {
        _filePath = filePath;
    }

    public async Task
        ProcessAsync() // this function asynchronously processes the file ( extracts the title from the txt file and writes the logs)
    {
        Log($"Processing {_filePath}...");

        try
        {
            string content = await File.ReadAllTextAsync(_filePath);

            string title = Regex.Match(content, @"(?<=Title:\s*).*").Value.Trim();
            Log($"File {_filePath} read. Title: {title}");

            var analysisResults = await AnalyzeContentAsync(content);


            var words = content.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = words.Length;

            // computes sentence count
            var sentences = content.Split(new char[] { '.', '!', '?', '*' }, StringSplitOptions.RemoveEmptyEntries);
            var sentenceCount = sentences.Length;

            // Log analysis results
            Log("Analysis completed:");

            // Log and display processing stats
            Log($"Bytes processed: {content.Length}");
            Console.WriteLine($"Bytes processed: {content.Length}");

            Log($"Words processed: {wordCount}");
            Console.WriteLine($"Words processed: {wordCount}");

            Log($"Sentences processed: {sentenceCount}");
            Console.WriteLine($"Sentences processed: {sentenceCount}");

            //calling the function and creating a file with the name of the book and saving it 
            await SaveResultsAsync($"{title.Replace(" ", "_")}.txt", analysisResults);

            Log($"Processing of {_filePath} completed");
        }
        catch (Exception ex)
        {
            Log($"Error processing {_filePath}: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        Console.WriteLine(message);
    }

    private async Task<FileAnalysisResult> AnalyzeContentAsync(string content) // to ge
    {
        // Splits the content into sentences for further analysis.
        var sentences = await SplitContentIntoSentencesAsync(content);
        // Calls the function FindTop... to get top 10 results of longest sentences and shortest sentences
        var (longestSentences, shortestSentences) = await FindTopLongestAndShortestSentencesAsync(sentences);
        // Calls the function to get the longest words
        var (longestWords, words) = await ExtractWordsAndLongestWordAsync(content);
        var longestWordsTop10 = words.Distinct().OrderByDescending(w => w.Length).Take(10).ToList();
        // To get the most common letters with their frequencies 
        var letters = content.Where(char.IsLetter).GroupBy(char.ToLower).OrderByDescending(g => g.Count()).Take(10);
        var wordFrequency = await CalculateWordFrequencyAsync(words);
        // To get the word frequencies 
        var wordFrequencyTop10 = wordFrequency.OrderByDescending(g => g.Value).Take(10)
            .ToDictionary(g => g.Key, g => g.Value);

        //returns the results of the analysis
        return new FileAnalysisResult
        {
            LongestSentences = longestSentences,
            ShortestSentences = shortestSentences,
            LongestWords = longestWordsTop10,
            MostCommonLetters = letters,
            WordFrequencyTop10 = wordFrequencyTop10
        };
    }

    private async Task<List<string>> SplitContentIntoSentencesAsync(string content)
    {
        return await Task.Run(() =>
        {
            // to avoid to count the  Mr Mrs ..... as a sentence because when we try to find the shortest sentence in my
            // previous trials it counted the Mr. as a sentence but it is not.
            var commonAbbreviations = new HashSet<string> { "mr", "mrs", "dr", "prof", "rev", "ms", "jr", "sr", "st" };

            var sentences = Regex.Split(content, @"(?<=[\.!?])\s+(?=[A-Z])")
                .Where(s => !string.IsNullOrWhiteSpace(s) &&
                            !commonAbbreviations.Contains(s.ToLower()) &&
                            s.Split(' ').Length > 1)
                .Distinct()
                .ToList();

            return sentences;
        });
    }


    private async Task<(List<string>, List<string>)> FindTopLongestAndShortestSentencesAsync(List<string> sentences)
    {
        return await Task.Run(() =>
        {
            //This function is to validate sentences
            // It returns a sentence valid if it has more than one word
            // or is a single word followed by an exclamation or question mark
            bool IsValidSentence(string sentence)
            {
                var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return words.Length > 1 || (words.Length == 1 && (sentence.EndsWith('!') || sentence.EndsWith('?')));
            }

            // to keep track of the normalized unique sentences
            var uniqueNormalizedSentences = new HashSet<string>();

            // t store the original forms of the unique sentences
            var uniqueOriginalSentences = new List<string>();

            foreach (var sentence in sentences)
            {
                // normalize the sentence to remove leading and continues punctuation and convert to lowercase for comparison
                // ^\W+|\W+$ -> looks for sequences of one or more non-word characters at either the start or the end 
                var normalizedSentence = Regex.Replace(sentence, @"^\W+|\W+$", "").ToLower();
                if (uniqueNormalizedSentences.Add(normalizedSentence) && IsValidSentence(sentence))
                {
                    uniqueOriginalSentences.Add(sentence);
                }
            }

            // to get the top 10 longest and shortest sentences from the unique set we created above 
            var topLongestSentences = uniqueOriginalSentences
                .OrderByDescending(s => s.Length)
                .Take(10)
                .ToList();

            var topShortestSentences = uniqueOriginalSentences
                .Where(IsValidSentence)
                .OrderBy(s => s.Split(' ').Length)
                .ThenBy(s => s.Length)
                .Take(10)
                .ToList();

            return (topLongestSentences, topShortestSentences);
        });
    }

    private async Task<(string, List<string>)> ExtractWordsAndLongestWordAsync(string content)
    {
        return await Task.Run(() =>
        {
            // b is for ensuring the pattern matches complete words rather tahn parts of a word 
            var words = Regex.Matches(content, @"\b[a-zA-Z']+\b").Cast<Match>().Select(m => m.Value.ToLowerInvariant())
                .ToList();
            var longestWord = words.OrderByDescending(w => w.Length).FirstOrDefault() ?? "";
            return (longestWord, words);
        });
    }


    //to calculate the words frequencies and sort them in descending order 
    private async Task<Dictionary<string, int>> CalculateWordFrequencyAsync(List<string> words)
    {
        return await Task.Run(() =>
        {

            var wordFrequency = words.GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // orders the dictionary by descending frequency counts
            var orderedWordFrequency = wordFrequency.OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return orderedWordFrequency;
        });
    }

    private async Task SaveResultsAsync(string fileName, FileAnalysisResult results)
    {
        //creating a folder called AnalyzedBooks to write the files in here 
        string folderPath = @"/Users/gunceozer/Desktop/AnalyzedBooks";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fullPath = Path.Combine(folderPath, fileName);
        await using var writer = new StreamWriter(fullPath);

        // Writing the results asynchronously 

        await writer.WriteLineAsync("Top 10 Longest Sentences by Characters:");
        foreach (var length in results.LongestSentences.Select(s => s.Length))
        {
            await writer.WriteLineAsync($"{length} chars");
        }

        await writer.WriteLineAsync("\nTop 10 Shortest Sentences by Words:");
        foreach (var sentence in results.ShortestSentences)
        {
            await writer.WriteLineAsync(sentence);
        }

        await writer.WriteLineAsync("\nTop 10 Longest Words:");
        foreach (var word in results.LongestWords)
        {
            await writer.WriteLineAsync(word);
        }

        await writer.WriteLineAsync("\nTop 10 Most Common Letters:");
        foreach (var group in results.MostCommonLetters)
        {
            await writer.WriteLineAsync($"{group.Key}: {group.Count()} occurrences");
        }

        await writer.WriteLineAsync("\nTop 10 Words by Frequency:");
        foreach (var pair in results.WordFrequencyTop10)
        {
            await writer.WriteLineAsync($"{pair.Key}: {pair.Value}");
        }
    }
}