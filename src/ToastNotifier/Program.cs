using CommandLine;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ToastNotifier
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            var exitCode = 1;
            Parser.Default.ParseArguments<Options>(args).
                WithNotParsed(parseErrors => { exitCode = 100; }).
                WithParsed(options =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(options.ApplicationId))
                        {
                            throw new ArgumentException("Application ID can not be empty!");
                        }

                        ExecuteCore(options);
                        exitCode = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        exitCode = 1;
                    }
                });

            return exitCode;
        }

        private static ToastNotification CreateFromTemplateFile(string templateFilePath)
        {
            var xml = File.ReadAllText(templateFilePath);
            var templateXml = new XmlDocument();
            templateXml.LoadXml(xml);
            return new ToastNotification(templateXml);
        }

        private static ToastNotification CreateToastNotificationFromOptions(Options options)
        {
            var templateFile = options.InputNotificationTemplateFilePath;
            if (string.IsNullOrWhiteSpace(templateFile))
            {
                return ToastNotificationFactory.CreateToastNotificationBuilderFromOptions(options)
                    .BuildToastNotification();
            }
            else
            {
                return CreateFromTemplateFile(options.InputNotificationTemplateFilePath);
            }
        }

        private static void ExecuteCore(Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.OutputNotificationTemplateFilePath))
            {
                ExportNotificationDefinition(options);
                return;
            }

            ShowNotificationAsDefinedByOptions(options);
        }

        private static void ExportNotificationDefinition(Options options)
        {
            var builder = ToastNotificationFactory.CreateToastNotificationBuilderFromOptions(options);
            File.WriteAllText(options.OutputNotificationTemplateFilePath, builder.BuildXml().GetXml());
        }

        private static SynchronousToastNotificationManager.Result ShowNotificationAsDefinedByOptions(Options options)
        {
            var notification = CreateToastNotificationFromOptions(options);

            var manager = new SynchronousToastNotificationManager(notification);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

            var baseApplicationId = Assembly.GetEntryAssembly().GetName().Name;
            return manager.ShowAndWait($"{baseApplicationId}.{options.ApplicationId}", cancellationTokenSource.Token);
        }
    }
}