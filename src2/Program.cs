// Program.cs - エントリポイント
using hoge.Configuration;
using hoge.Services;
using Microsoft.Extensions.DependencyInjection;
using Notion.Client;

// DIコンテナの設定
var services = new ServiceCollection();

// コマンドライン引数から設定を取得
var config = AppConfiguration.FromCommandLine(args);
services.AddSingleton(config);

// NotionClientの登録
services.AddSingleton<INotionClient>(provider =>
    NotionClientFactory.Create(new ClientOptions
    {
        AuthToken = config.NotionAuthToken
    }));

// サービスの登録
services.AddSingleton<INotionClientWrapper, NotionClientWrapper>();
services.AddSingleton<IMarkdownGenerator, MarkdownGenerator>();
services.AddSingleton<INotionExporter, NotionExporter>();

// サービスプロバイダーの構築
var serviceProvider = services.BuildServiceProvider();

// サービスの取得と実行
var exporter = serviceProvider.GetRequiredService<INotionExporter>();
await exporter.ExportPagesAsync();

// リソースの解放
if (serviceProvider is IDisposable disposable)
{
    disposable.Dispose();
}




//// コマンドライン引数から設定を取得
//var config = AppConfiguration.FromCommandLine(args);
//var exporter = new NotionExporter(config);
//await exporter.ExportPagesAsync();


