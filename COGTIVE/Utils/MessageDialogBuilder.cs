using System;
using Windows.Foundation;
using Windows.UI.Popups;

namespace COGTIVE.Utils
{
    public sealed class MessageDialogBuilder : IDisposable
    {
        private MessageDialog dialog;
        private UICommand okCommand, cancelCommand;
        private bool isShowing;

        ~MessageDialogBuilder()
        {
            this.Dispose(false);
        }

        private MessageDialogBuilder(string message)  
        {
            this.dialog = new MessageDialog(ObjectUtils.RequireNonNullOrEmpty(message));
        }

        public static MessageDialogBuilder Builder(string message)
        {
            return new MessageDialogBuilder(message);
        }

        public static MessageDialogBuilder Builder(Exception error)
        {
            return Builder($"Ocorreu um erro durante a execução:\r\n{ObjectUtils.RequireNonNull(error).Message}").Title("Error");
        }

        public MessageDialogBuilder Title(string title)
        {
            this.dialog.Title = ObjectUtils.RequireNonNullOrEmpty(title);
            return this;
        }

        public MessageDialogBuilder OkCommand(string text, Action<IUICommand> onCommandAction = null)
        {
            this.dialog.DefaultCommandIndex = AddCommand(ref okCommand, text, onCommandAction);
            return this;
        }

        public MessageDialogBuilder OkCommand(Action<IUICommand> onCommandAction = null)
        {
            return OkCommand("OK", onCommandAction);
        }

        public MessageDialogBuilder CancelCommand(string text, Action<IUICommand> onCommandAction = null)
        {
            this.dialog.CancelCommandIndex = AddCommand(ref cancelCommand, text, onCommandAction);
            return this;
        }

        public MessageDialogBuilder CancelCommand(Action<IUICommand> onCommandAction = null)
        {
            return CancelCommand("Cancelar", onCommandAction);
        }

        public IAsyncOperation<IUICommand> ShowAsync()
        {
            try
            {
                isShowing = true;
                return dialog.ShowAsync();
            }
            catch
            {
                isShowing = false;
                throw;
            }
            finally
            {
                ((IDisposable)this).Dispose();
            }
        }

        public async void Show()
        {
            await ShowAsync();
        }

        private uint AddCommand(ref UICommand command, string text, Action<IUICommand> onCommandAction)
        {
            if (command != null)
            {
                command.Label = text;
                command.Invoked = onCommandAction == null ? null : new UICommandInvokedHandler(onCommandAction);
                return Convert.ToUInt32(this.dialog.Commands.IndexOf(command));
            }
            else
            {
                int count = this.dialog.Commands.Count;
                this.dialog.Commands.Add(command = onCommandAction == null ? 
                    new UICommand(text) :
                    new UICommand(text, new UICommandInvokedHandler(onCommandAction)));
                return Convert.ToUInt32(count);
            }
        }

        private void ClearCommand(UICommand command)
        {
            if (command != null)
            {
                command.Invoked = null;
            }
        }

        private void Dispose(bool isDisposing)
        {
            if(isDisposing)
            {
                if(!isShowing)
                {
                    dialog.Commands.Clear();
                    ClearCommand(okCommand);
                    ClearCommand(cancelCommand);
                }              
            }

            dialog = null;
            okCommand = null;
            cancelCommand = null;
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
