using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using MarkovSharp.TokenisationStrategies;
using PuppeteerSharp;
using StopWord;

namespace WhatsAppBot
{
    class Program
    {
        static Browser _browser;
        static Page _whatsAppPage;
        private static StringMarkov _model;

        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<BotArguments>(args).MapResult(
                    async (BotArguments result) => await LaunchProcessAsync(result),
                    _ => Task.FromResult<object>(null));
        }

        private static async Task LaunchProcessAsync(BotArguments args)
        {
            await InitBrowserAsync(args);
            await InitWhatsAppAsync(args);
            await InitMarkovAsync(args);

            Console.ReadLine();
            await _browser.CloseAsync();
        }

        private static async Task InitWhatsAppAsync(BotArguments args)
        {
            _whatsAppPage = await _browser.NewPageAsync();
            await _whatsAppPage.GoToAsync("https://web.whatsapp.com/");
            await _whatsAppPage.WaitForSelectorAsync("#pane-side");
            var input = (await _whatsAppPage.XPathAsync("//input[@title=\"Search or start new chat\"]")).First();
            await input.TypeAsync(args.ChatName);
            await _whatsAppPage.WaitForTimeoutAsync(500);
            var menuItem = (await _whatsAppPage.QuerySelectorAllAsync("._2wP_Y")).ElementAt(1);
            await menuItem.ClickAsync();

            await _whatsAppPage.ExposeFunctionAsync("newChat", async (string text) =>
            {
                Console.WriteLine(text);

                if (text.ToLower().Contains(args.TriggerWord) && !text.Contains(args.ResponseTemplate))
                {
                    await RespondAsync(args, text);
                }

                text = text.Replace(args.ResponseTemplate, string.Empty);
                await File.AppendAllTextAsync(args.SourceText, text + "\n");
            });

            await _whatsAppPage.EvaluateFunctionAsync(@"() => {
                var observer = new MutationObserver((mutations) => { 
                    for(var mutation of mutations) {
                        if(mutation.addedNodes.length && mutation.addedNodes[0].classList.value === 'vW7d1') {
                            newChat(mutation.addedNodes[0].querySelector('.copyable-text span').innerText);
                        }
                    }
                });
                observer.observe(document.querySelector('._9tCEa'), { attributes: false, childList: true, subtree: true });
            }");
        }

        private static async Task InitBrowserAsync(BotArguments args)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                UserDataDir = Path.Combine(".", "user-data-dir"),
                Headless = false
            });
        }

        private static async Task InitMarkovAsync(BotArguments args)
        {
            var chat = await File.ReadAllLinesAsync(args.SourceText);
            _model = new StringMarkov(5);
            _model.Learn(chat);
        }


        private static async Task RespondAsync(BotArguments args, string text)
        {
            string response = null;
            var words = text.RemoveStopWords(args.Language).RemovePunctuation().Replace(args.TriggerWord, string.Empty).Split(' ');
            for (var index = words.Length - 1; index >= 0; index--)
            {
                response = _model.Walk(1, words[index]).First();

                if (response == words[index])
                {
                    response = null;
                }
            }

            if (response == null)
            {
                response = _model.Walk(1).First();
            }

            Console.WriteLine(response);
            await WriteChatAsync(args.ResponseTemplate + " " + response);
        }

        private static async Task WriteChatAsync(string text)
        {
            var chatInput = (await _whatsAppPage.QuerySelectorAllAsync("._2S1VP")).First();
            await chatInput.TypeAsync(text);
            await (await _whatsAppPage.QuerySelectorAsync("._35EW6")).ClickAsync();
        }
    }
}
