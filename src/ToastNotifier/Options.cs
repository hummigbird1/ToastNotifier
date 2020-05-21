using CommandLine;
using System.Collections.Generic;

namespace ToastNotifier
{
    internal class Options
    {
        private const string SetName_FromParameters = "FromParameters";
        private const string SetName_FromXml = "FromXmlTemplateFile";

        [Option('a', "app-id", Required = true, HelpText = "A system unique string to identify the sender of the notification")]
        public string ApplicationId { get; set; }

        [Option('i', "image", SetName = SetName_FromParameters, HelpText = "Image to display in the notification. Can be either a local full qualified file path, or an image url!")]
        public string ImageFileOrUrl { get; set; }

        [Option("template-file-path", SetName = SetName_FromXml, HelpText = "Load a preconfigured template file and show as notification!")]
        public string InputNotificationTemplateFilePath { get; set; }

        [Option('l', "long-display-duration", SetName = SetName_FromParameters, HelpText = "Notification will be displayed longer than the standard duration of 7s. If specified the message will be displayed 25s.")]
        public bool LongDisplayDuration { get; set; }

        [Option('r', "sound-repeat", SetName = SetName_FromParameters, HelpText = "Only allowed when long display duration has been selected. Specifies to repeat the selected sound.")]
        public bool LoopSound { get; set; }

        [Option('o', "output-template-file-path", SetName = SetName_FromParameters, HelpText = "When specified, this will save the Notification as a template file instead of displaying it.")]
        public string OutputNotificationTemplateFilePath { get; set; }

        [Option('s', "sound", SetName = SetName_FromParameters, HelpText = "Choose the sound that is played when displaying the notification To disable sound playback, specify: Off (Sounds available: Default, IM, Mail, Reminder, SMS, Call, Alarm (Call and alarm each have a variation of 1 to 10 - specify as e.g. Call;5))")]
        public string Sound { get; set; }

        [Option("template", SetName = SetName_FromParameters, HelpText = "Specifies which standard notification template to use! (Available: ToastImageAndText01, ToastImageAndText02, ToastImageAndText03, ToastImageAndText04, ToastText01, ToastText02, ToastText03, ToastText04)")]
        public string StandardTemplateName { get; set; }

        [Option('t', "text-lines", SetName = SetName_FromParameters, HelpText = "Required. Lines of text to display in the notification. (A maximum of 3 lines are possible)")]
        public IEnumerable<string> TextLines { get; set; }
    }
}