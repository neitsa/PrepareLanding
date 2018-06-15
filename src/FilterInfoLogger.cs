using System.ComponentModel;
using System.Text;
using JetBrains.Annotations;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class FilterInfoLogger : INotifyPropertyChanged
    {
        private StringBuilder _stringBuilder;

        public FilterInfoLogger()
        {
            _stringBuilder = new StringBuilder();
        }

        public string Text => _stringBuilder.ToString();

        public event PropertyChangedEventHandler PropertyChanged;

        public void AppendErrorMessage(string text, string shortRimWorldMessage = null,
            bool rimWorldAlertMessage = true, bool sendToLog = false)
        {
            if (rimWorldAlertMessage)
            {
                if (PrepareLanding.Instance.MainWindow == null)
                {
                    Log.Error("[PrepareLanding] Main window is null in this context.");
                    return;
                }

                var tab = PrepareLanding.Instance.MainWindow.TabController.TabById("WorldInfo");
                var tabName = tab == null ? "PLMWINF_WorldInfo".Translate() : tab.Name;
                var shortMessage = shortRimWorldMessage.NullOrEmpty() ? "" : $": {shortRimWorldMessage}";
                Messages.Message(
                    $"[PrepareLanding] {string.Format("PLFILIL_FilterErrorOccurred".Translate(), shortMessage, tabName)}",
                    MessageTypeDefOf.RejectInput);
            }

            if (sendToLog)
                Log.Message($"[PrepareLanding] {text}");

            var errorText = RichText.Bold(RichText.Color(text, Color.red));
            AppendLine(errorText);
        }

        public void AppendMessage(string text, bool sendToLog = false, Color? textColor = null)
        {
            if (sendToLog)
                Log.Message($"[PrepareLanding] {text}");

            if (textColor != null)
                text = RichText.Color(text, (Color) textColor);

            AppendLine(text);
        }

        public void AppendTitleMessage(string text, bool sendToLog = false, Color? textColor = null)
        {
            var separator = "-".Repeat(80);
            AppendMessage($"{separator}\n{text}\n{separator}", sendToLog, textColor);
        }

        public void AppendSuccessMessage(string text, bool sendToLog = false)
        {
            if (sendToLog)
                Log.Message($"[PrepareLanding] {text}");

            var successText = RichText.Bold(RichText.Color(text, Color.green));
            AppendLine(successText);
        }

        public void AppendWarningMessage(string text, bool sendToLog = false)
        {
            if (sendToLog)
                Log.Message($"[PrepareLanding] {text}");

            var warningText = RichText.Bold(RichText.Color(text, ColorLibrary.Orange));
            AppendLine(warningText);
        }

        public void Clear()
        {
            // in .NET 3.5 we don't have a Clear() method. A possible solution would be to set length to 0 (and maybe capacity).
            // Anyway, we just set a new instance, the old one will be garbage collected.
            _stringBuilder = new StringBuilder();
            OnPropertyChanged(nameof(Text));
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Append(string text)
        {
            _stringBuilder.Append(text);
            OnPropertyChanged(nameof(Text));
        }

        private void AppendLine(string text)
        {
            _stringBuilder.AppendLine(text);
            OnPropertyChanged(nameof(Text));
        }
    }
}