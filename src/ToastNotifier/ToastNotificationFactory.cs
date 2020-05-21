using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Notifications;

namespace ToastNotifier
{
    internal class ToastNotificationFactory
    {
        private static string[] _pseudoSoundNamesForDisabled = new[] { "-", "disabled", "none", "off" };

        public static ToastNotificationBuilder CreateToastNotificationBuilderFromOptions(Options options)
        {
            var textLines = options.TextLines.ToArray();
            var needImage = !string.IsNullOrEmpty(options.ImageFileOrUrl);
            ToastNotificationBuilder notificationBuilder;
            if (!string.IsNullOrWhiteSpace(options.StandardTemplateName))
            {
                notificationBuilder = GetAvailableNotificationBuilders().SingleOrDefault(x => string.Equals(x.ToastTemplateType.ToString(), options.StandardTemplateName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                notificationBuilder = FindTemplateBuilder(textLines.Length, needImage);
            }

            if (needImage)
            {
                if (options.ImageFileOrUrl.Contains("://"))
                {
                    notificationBuilder.SetImageFromUrl(options.ImageFileOrUrl);
                }
                else
                {
                    notificationBuilder.SetImageFromFile(options.ImageFileOrUrl);
                }
            }

            int line = 0;
            foreach (var text in textLines)
            {
                notificationBuilder.SetTextLine(++line, text);
            }

            notificationBuilder.SetDuration(options.LongDisplayDuration ? ToastNotificationBuilder.DisplayDuration.Long : ToastNotificationBuilder.DisplayDuration.Normal);

            UpdateSoundSelection(options.Sound, options.LoopSound, notificationBuilder);

            return notificationBuilder;
        }

        private static ToastNotificationBuilder FindTemplateBuilder(int textLineCount, bool needImage)
        {
            var notificationBuilders = GetAvailableNotificationBuilders()
                .Where(x => x.TemplateHasImage == needImage && x.TemplateAvailableTextLines == textLineCount);

            var matchingTemplates = (notificationBuilders?.Count()).GetValueOrDefault(0);
            if (matchingTemplates == 0)
            {
                throw new ArgumentException($"No templates available for {textLineCount} lines of text {(needImage ? "and an image" : "and no image")}!");
            }
            else if (matchingTemplates > 1)
            {
                throw new ArgumentException($"{matchingTemplates} template(s) available for {textLineCount} lines of text {(needImage ? "and an image" : "and no image")}! Please use the command line option 'template' to specify the exact template to use: {string.Join(", ", notificationBuilders.Select(x => x.ToastTemplateType))}");
            }

            var notificationBuilder = notificationBuilders.Single();
            return notificationBuilder;
        }

        private static IEnumerable<ToastNotificationBuilder> GetAvailableNotificationBuilders()
        {
            foreach (var template in Enum.GetValues(typeof(ToastTemplateType)))
            {
                yield return new ToastNotificationBuilder((ToastTemplateType)template);
            }
        }

        private static void UpdateSoundSelection(string soundParameter, bool loop, ToastNotificationBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(soundParameter))
            {
                return;
            }

            if (_pseudoSoundNamesForDisabled.Any(x => string.Equals(x, soundParameter, StringComparison.OrdinalIgnoreCase)))
            {
                builder.SetDisableSound();
                return;
            }

            builder.SetSoundLooping(loop);
            string sound = soundParameter;
            ushort variant = 1;
            bool variantSpecified = false;
            if (soundParameter.Contains(";"))
            {
                var soundParts = soundParameter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                sound = soundParts[0];
                if (soundParts.Length > 1)
                {
                    variantSpecified = true;
                    if (!ushort.TryParse(soundParts[1], out variant))
                    {
                        throw new ArgumentException($"Invalid sound variant '{soundParts[1]}' specified!", nameof(soundParameter));
                    }
                }
            }

            if (Enum.TryParse<ToastNotificationBuilder.VariableSounds>(sound, true, out var selectedLooping))
            {
                builder.SetSound(selectedLooping, variant);
                return;
            }

            if (variantSpecified)
            {
                throw new ArgumentException($"You can not specify a variant for the sound '{sound}'");
            }
            if (Enum.TryParse<ToastNotificationBuilder.Sounds>(sound, true, out var selected))
            {
                builder.SetSound(selected);
            }
            else
            {
                throw new ArgumentException($"The specified sound '{sound}' is invalid", nameof(soundParameter));
            }
        }
    }
}