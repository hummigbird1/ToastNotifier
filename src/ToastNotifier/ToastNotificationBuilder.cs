using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ToastNotifier
{
    internal class ToastNotificationBuilder
    {
        private readonly string[] _lineTexts;
        private readonly XmlDocument _templateXml;
        private bool _hasBeenBuild = false;
        private string _image = null;
        private bool _longDuration = false;
        private SoundDefinition _soundDefinition = new SoundDefinition { SoundType = SoundType.Normal, PlaybackDefinition = new PlayableSoundDefinition() };

        public ToastNotificationBuilder(ToastTemplateType toastTemplateType)
        {
            _templateXml = ToastNotificationManager.GetTemplateContent(toastTemplateType);
            _lineTexts = new string[GetTextElements().Count];
            ToastTemplateType = toastTemplateType;
        }

        public enum DisplayDuration
        {
            Normal,
            Long
        }

        public enum Sounds
        {
            Default,
            IM,
            Mail,
            Reminder,
            SMS
        }

        public enum SoundType
        {
            Silent,
            Normal,
            Looping
        }

        public enum VariableSounds
        {
            Alarm,
            Call
        }

        public int TemplateAvailableTextLines => GetTextElements().Count;

        public bool TemplateHasImage => GetImageElements().Count > 0;

        public ToastTemplateType ToastTemplateType { get; }

        public string this[int line]
        {
            get
            {
                return _lineTexts[line - 1];
            }
            set
            {
                CheckChangesAllowed();
                _lineTexts[line - 1] = value;
            }
        }

        public ToastNotification BuildToastNotification()
        {
            return new ToastNotification(BuildXml());
        }

        public XmlDocument BuildXml()
        {
            if (!_hasBeenBuild)
            {
                UpdateTexts();
                UpdateImage();
                UpdateDuration();
                UpdateSound();
                _hasBeenBuild = true;
            }
            return _templateXml;
        }

        public ToastNotificationBuilder ClearImage()
        {
            _image = null;
            return this;
        }

        public ToastNotificationBuilder SetDisableSound()
        {
            CheckChangesAllowed();
            _soundDefinition.SoundType = SoundType.Silent;
            return this;
        }

        public ToastNotificationBuilder SetDuration(DisplayDuration duration)
        {
            CheckChangesAllowed();

            _longDuration = duration == DisplayDuration.Long;
            return this;
        }

        public ToastNotificationBuilder SetImageFromFile(string filePath)
        {
            CheckChangesAllowed();
            if (!TemplateHasImage)
            {
                throw new InvalidOperationException("Selected template does not support image!");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _image = filePath;
            return this;
        }

        public ToastNotificationBuilder SetImageFromUrl(string url)
        {
            CheckChangesAllowed();
            if (!TemplateHasImage)
            {
                throw new InvalidOperationException("Selected template does not support image!");
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            _image = url;
            return this;
        }

        public ToastNotificationBuilder SetSound(VariableSounds sound, ushort variant = 1)
        {
            CheckChangesAllowed();
            if (variant == 0 || variant > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(variant), "Only Variants 1 to 10 are possible");
            }

            _soundDefinition.PlaybackDefinition.SetVariableSound(sound, variant);
            if (_soundDefinition.SoundType == SoundType.Silent)
            {
                _soundDefinition.SoundType = SoundType.Normal;
            }
            return this;
        }

        public ToastNotificationBuilder SetSound(Sounds sound)
        {
            CheckChangesAllowed();

            _soundDefinition.PlaybackDefinition.SetNormalSound(sound);
            if (_soundDefinition.SoundType == SoundType.Silent)
            {
                _soundDefinition.SoundType = SoundType.Normal;
            }
            return this;
        }

        public ToastNotificationBuilder SetSoundLooping(bool loop)
        {
            CheckChangesAllowed();

            _soundDefinition.SoundType = loop ? SoundType.Looping : SoundType.Normal;
            return this;
        }

        public ToastNotificationBuilder SetTextLine(int line, string text)
        {
            CheckChangesAllowed();
            this[line] = text;
            return this;
        }

        public override string ToString()
        {
            return _templateXml.GetXml();
        }

        private void CheckChangesAllowed()
        {
            if (_hasBeenBuild)
            {
                throw new InvalidOperationException("This instance has already been build. Changes are not allowed anymore.");
            }
        }

        private string ConvertFilePathToFileUrl(string image)
        {
            var fileUri = image.Replace("\\", "/");
            return $"file:///{fileUri}";
        }

        private XmlNodeList GetImageElements()
        {
            return _templateXml.GetElementsByTagName("image");
        }

        private string GetImageSource()
        {
            if (_image.Contains("://"))
            {
                return _image;
            }

            return ConvertFilePathToFileUrl(_image);
        }

        private string GetSoundAttributeValue()
        {
            if (_soundDefinition.PlaybackDefinition.IsNormalSound)
            {
                return GetSoundAttributeValue(_soundDefinition.PlaybackDefinition.NormalSound);
            }

            return GetSoundAttributeValue(_soundDefinition.PlaybackDefinition.VariableSound, _soundDefinition.PlaybackDefinition.VariableSoundVariant);
        }

        private string GetSoundAttributeValue(Sounds sound)
        {
            var soundKey = "Default";
            switch (sound)
            {
                case Sounds.IM:
                    soundKey = "IM";
                    break;

                case Sounds.Mail:
                    soundKey = "Mail";
                    break;

                case Sounds.Reminder:
                    soundKey = "Reminder";
                    break;

                case Sounds.SMS:
                    soundKey = "SMS";
                    break;
            }
            return $"ms-winsoundevent:Notification.{soundKey}";
        }

        private string GetSoundAttributeValue(VariableSounds loopingSound, ushort variant)
        {
            var soundBase = $"ms-winsoundevent:Notification.Looping.Alarm";
            if (loopingSound == VariableSounds.Call)
                soundBase = $"ms-winsoundevent:Notification.Looping.Call";

            if (variant > 1)
            {
                soundBase += $"{variant}";
            }
            return soundBase;
        }

        private XmlNodeList GetTextElements()
        {
            return _templateXml.GetElementsByTagName("text");
        }

        private IXmlNode GetToastNode()
        {
            return _templateXml.SelectSingleNode("/toast");
        }

        private void UpdateDuration()
        {
            if (!_longDuration)
            {
                return;
            }

            var toastElement = GetToastNode();

            var att = _templateXml.CreateAttribute("duration");
            att.NodeValue = "long";
            toastElement.Attributes.SetNamedItem(att);
        }

        private void UpdateImage()
        {
            if (string.IsNullOrWhiteSpace(_image))
            {
                return;
            }

            var imageElement = GetImageElements()[0];
            imageElement.Attributes[1].NodeValue = GetImageSource();
        }

        private void UpdateSound()
        {
            if (_soundDefinition.SoundType == SoundType.Silent)
            {
                UpdateToSilent();
                return;
            }

            var soundValue = GetSoundAttributeValue();

            var toastElement = GetToastNode();
            var audioElement = _templateXml.CreateElement("audio");
            var audioSourceAttribute = _templateXml.CreateAttribute("src");
            audioSourceAttribute.NodeValue = soundValue;
            audioElement.Attributes.SetNamedItem(audioSourceAttribute);

            if (_soundDefinition.SoundType == SoundType.Looping)
            {
                var loopAttribute = _templateXml.CreateAttribute("loop");
                loopAttribute.NodeValue = "true";
                audioElement.Attributes.SetNamedItem(loopAttribute);
            }

            toastElement.AppendChild(audioElement);
        }

        private void UpdateTexts()
        {
            var textNodes = GetTextElements();
            for (int x = 0; x < _lineTexts.Length; x++)
            {
                textNodes[x].AppendChild(_templateXml.CreateTextNode(_lineTexts[x]));
            }
        }

        private void UpdateToSilent()
        {
            var toastElement = GetToastNode();
            var audio = _templateXml.CreateElement("audio");
            var silenAttribute = _templateXml.CreateAttribute("silent");
            silenAttribute.NodeValue = "true";
            audio.Attributes.SetNamedItem(silenAttribute);
            toastElement.AppendChild(audio);
        }

        private struct SoundDefinition
        {
            public PlayableSoundDefinition PlaybackDefinition { get; set; }
            public SoundType SoundType { get; set; }
        }

        private class PlayableSoundDefinition
        {
            public bool IsNormalSound { get; private set; } = true;

            public Sounds NormalSound { get; private set; } = Sounds.Default;

            public VariableSounds VariableSound { get; private set; }

            public ushort VariableSoundVariant { get; private set; }

            public void SetNormalSound(Sounds sound)
            {
                IsNormalSound = true;
                NormalSound = sound;
            }

            public void SetVariableSound(VariableSounds sound, ushort variation)
            {
                VariableSound = sound;
                VariableSoundVariant = variation;
                IsNormalSound = false;
            }
        }
    }
}