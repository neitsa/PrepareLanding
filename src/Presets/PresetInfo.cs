using System;
using System.Text;
using System.Xml.Linq;
using PrepareLanding.Core.Extensions;

namespace PrepareLanding.Presets
{
    public class PresetInfo
    {
        public const string InfoNode = "Info";
        public const string PresetVersionNode = "Version";
        public const string PresetVersion = "1.0";
        public const string PresetDescriptionNode = "Description";
        public const string PresetTemplateNode = "Template";
        public const string PresetAuthorNode = "Author";
        public const string PresetDateNode = "Date";

        public string Description { get; set; }

        public string Version { get; set; }

        public string Author { get; set; }

        public DateTime Date { get; set; }

        public bool IsTemplate { get; private set; }

        private StringBuilder _filterInfo;

        public string FilterInfo => _filterInfo.ToString();

        private StringBuilder _optionInfo;

        public string OptionInfo => _optionInfo.ToString();

        private int _indentLevel;

        public void SavePresetInfo(XContainer xRootElement)
        {
            var xInfoElement = new XElement(InfoNode);
            xRootElement.Add(xInfoElement);

            xInfoElement.Add(new XElement(PresetVersionNode, PresetVersion));
            xInfoElement.Add(new XElement(PresetAuthorNode, Author));
            xInfoElement.Add(new XElement(PresetTemplateNode, false));
            xInfoElement.Add(new XElement(PresetDescriptionNode, string.IsNullOrEmpty(Description) ? "None" : Description));
            //xInfoElement.Add(new XElement(PresetDateNode, DateTime.Now));
        }

        public void LoadPresetInfo(XContainer xRootNode)
        {
            _filterInfo = new StringBuilder();
            _optionInfo = new StringBuilder();

            var xInfoNode = xRootNode?.Element(InfoNode);
            if (xInfoNode == null)
                return;

            Version = xInfoNode.Element(PresetVersionNode)?.Value;
            Author = xInfoNode.Element(PresetAuthorNode)?.Value;
            //Date = xRootNode.Element(PresetDateNode)?.Value;
            Description = xInfoNode.Element(PresetDescriptionNode)?.Value;
            Preset.LoadBoolean(xInfoNode, PresetTemplateNode, b => IsTemplate = b);

            var xPresetNode = xInfoNode.Parent;

            var xFilters = xPresetNode?.Element(Preset.FilterNode);
            if (xFilters == null)
                return;

            _indentLevel = 0;
            LoadPresetInfoRecursive(xFilters, _filterInfo);

            var xOptions = xPresetNode.Element(Preset.OptionNode);
            if (xOptions == null)
                return;

            _indentLevel = 0;
            LoadPresetInfoRecursive(xOptions, _optionInfo);
        }

        private void LoadPresetInfoRecursive(XContainer xRootNode, StringBuilder sb)
        {
            foreach (var element in xRootNode.Elements())
            {
                var indentString = " ".Repeat(_indentLevel);
                sb.AppendLine(element.HasElements
                    ? $"{indentString}{element.Name}"
                    : $"{indentString}{element.Name}: {element.Value}");

                _indentLevel += 4;
                LoadPresetInfoRecursive(element, sb);
            }

            _indentLevel -= 4;
            if (_indentLevel < 0)
                _indentLevel = 0;
        }
    }
}
