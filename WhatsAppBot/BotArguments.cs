using CommandLine;

namespace WhatsAppBot
{
    public class BotArguments
    {
        [Option('t', "trigger", Required = true, HelpText = "Trigger word.")]
        public string TriggerWord { get; set; }
        [Option('c', "chat", Required = true, HelpText = "Chat name.")]
        public string ChatName { get; set; }
        [Option('r', "response", Required = true, HelpText = "Response template.")]
        public string ResponseTemplate { get; set; }
        [Option('l', "language", Required = true, HelpText = "Language.")]
        public string Language { get; set; }
        [Option('f', "file", Required = true, HelpText = "Source text file.")]
        public string SourceText { get; set; }
    }
}
