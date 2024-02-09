using BookTextAnalyzer;
class Program
{
    static async Task Main(string[] args)
    {
        var filePath = @"/Users/gunceozer/Desktop/100 books"; // when you run it don't forget to change the path 
        string[] files = Directory.GetFiles(filePath, "*.txt"); // retrieves all txt files in the directory
        var tasks = new Task[files.Length]; // array to hold tasks for processing each file concurrently

        for (int i = 0; i < files.Length; i++)
        {
            var processor = new FileProcessor(files[i]);
            tasks[i] = Task.Run(() => processor.ProcessAsync()); // processing files in parallel
        }

        await Task.WhenAll(tasks); // waits for all file processing tasks to complete
        
    }
}