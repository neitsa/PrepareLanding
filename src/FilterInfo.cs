using System.ComponentModel;
using System.Text;
using JetBrains.Annotations;
using PrepareLanding.Gui;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class FilterInfo : INotifyPropertyChanged
    {

        private StringBuilder _stringBuilder;

        public FilterInfo()
        {
            _stringBuilder = new StringBuilder();
        }

        public void Append(string text)
        {
            _stringBuilder.Append(text);
            OnPropertyChanged(nameof(Text));
        }

        public void AppendLine(string text)
        {
            _stringBuilder.AppendLine(text);
            OnPropertyChanged(nameof(Text));
        }

        public void AppendErrorMessage(string text, bool rimWorldAlertMessage = true)
        {
            if(rimWorldAlertMessage)
                Messages.Message("An error occurred. Please see the Info tab for a description.", MessageSound.RejectInput);

            var errorText = RichText.Bold(RichText.Color(text, Color.red));
            AppendLine(errorText);
        }

        public void AppendWarningMessage(string text)
        {
            var warningText = RichText.Bold(RichText.Color(text, Color.yellow));
            AppendLine(warningText);
        }

        public void AppendSuccessMessage(string text)
        {
            var successText = RichText.Bold(RichText.Color(text, Color.green));
            AppendLine(successText);
        }

        public void Clear()
        {
            // in .NET 3.5 we don't have a Clear() method. A possible solution would be to set length to 0 (and maybe capacity).
            // Anyway, we just set a new instance, the old one will be garbage collected.
            _stringBuilder = new StringBuilder();
            OnPropertyChanged(nameof(Text));
        }

        public string Text => _stringBuilder.ToString();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
