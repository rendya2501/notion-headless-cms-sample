namespace hoge.Configuration;

public class AppConfiguration
{
    public string NotionAuthToken { get; private set; } = string.Empty;
    public string NotionDatabaseId { get; private set; } = string.Empty;
    public string OutputDirectoryPathTemplate { get; private set; } = string.Empty;

    public static AppConfiguration FromCommandLine(string[] args)
    {
        if (args.Length != 3)
        {
            throw new ArgumentException("Required arguments: [NotionAuthToken] [DatabaseId] [OutputPathTemplate]");
        }

        return new AppConfiguration
        {
            NotionAuthToken = args[0],
            NotionDatabaseId = args[1],
            OutputDirectoryPathTemplate = args[2]
        };
    }
}
