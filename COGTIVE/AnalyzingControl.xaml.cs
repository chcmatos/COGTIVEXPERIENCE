using COGTIVE.Enums;
using COGTIVE.Utils;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace COGTIVE
{
    public sealed partial class AnalyzingControl : UserControl
    {
        #region DependencyProperties
        internal static readonly DependencyProperty AnalyzingStateProperty =
            DependencyProperty.Register(nameof(AnalyzingState), typeof(AnalyzingStates), typeof(AnalyzingControl),
                new PropertyMetadata(AnalyzingStates.Sleeping, OnAnalyzingStatePropertyChangedCallback));

        internal static readonly DependencyProperty SelectedFileProperty =
            DependencyProperty.Register(nameof(SelectedFile), typeof(IStorageItem), typeof(AnalyzingControl),
                new PropertyMetadata(default(IStorageItem), OnSelectedFilePropertyChangedCallback));

        private static readonly DependencyProperty AnalyzingTextProperty =
            DependencyProperty.Register(nameof(Text), typeof(AnalyzingText), typeof(AnalyzingControl),
                new PropertyMetadata(default(AnalyzingText)));

        internal static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register(nameof(ProgressValue), typeof(double), typeof(AnalyzingControl),
                new PropertyMetadata(default(double)));

        private static void OnAnalyzingStatePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnalyzingControl control)
            {
                control.Text = new AnalyzingText((AnalyzingStates) e.NewValue, control.SelectedFile);
            }
        }

        private static void OnSelectedFilePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnalyzingControl control)
            {
                control.Text = new AnalyzingText(control.AnalyzingState, (IStorageItem)e.NewValue);
            }
        }
        #endregion

        public AnalyzingStates AnalyzingState
        {
            get => (AnalyzingStates) this.GetValue(AnalyzingStateProperty);
            internal set => this.SetValue(AnalyzingStateProperty, value);
        }

        public IStorageItem SelectedFile
        {
            get => this.GetValue(SelectedFileProperty) as IStorageItem;
            internal set => this.SetValue(SelectedFileProperty, value);
        }

        internal AnalyzingText Text
        {
            get => this.GetValue(AnalyzingTextProperty) as AnalyzingText;
            private set => this.SetValue(AnalyzingTextProperty, value);
        }

        public double ProgressValue
        {
            get => (double) this.GetValue(ProgressValueProperty);
            set => this.SetValue(ProgressValueProperty, value);
        }

        public AnalyzingControl()
        {
            this.InitializeComponent();
        }
    }
}
