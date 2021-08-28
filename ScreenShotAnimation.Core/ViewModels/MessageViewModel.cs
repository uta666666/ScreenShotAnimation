using Livet;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAnimation.ViewModels
{
    public class MessageViewModel : ViewModel
    {
        public MessageViewModel(string title, string message, string button1 = null, string button2 = null)
        {
            Title = title;
            Message = message;
            Button1Text = button1 ?? "OK";
            Button2Text = button2 ?? null;
            Button2Visible = !string.IsNullOrEmpty(Button2Text);
            ReturnType = new ReactiveProperty<MessageViewReturnType>(MessageViewReturnType.None);

            Button1Command = new ReactiveCommand();
            Button1Command.Subscribe(() =>
            {
                ReturnType.Value = MessageViewReturnType.Button1;
            });

            Button2Command = new ReactiveCommand();
            Button2Command.Subscribe(() =>
            {
                ReturnType.Value = MessageViewReturnType.Button2;
            });
        }


        public string Title { get; private set; }

        public string Message { get; private set; }

        public string Button1Text { get; private set; }

        public string Button2Text { get; private set; }

        public bool Button2Visible { get; private set; }

        public ReactiveCommand Button1Command { get; private set; }

        public ReactiveCommand Button2Command { get; private set; }

        public ReactiveProperty<MessageViewReturnType> ReturnType { get; set; }
    }

    public enum MessageViewReturnType
    {
        Button1,
        Button2,
        None
    }
}
