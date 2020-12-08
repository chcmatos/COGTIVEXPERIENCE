using COGTIVE.Enums;
using COGTIVE.Model;
using COGTIVE.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace COGTIVE
{
    public sealed partial class MainPage : Page
    {
        private const double MIN_WIDTH  = 1024d;
        private const double MIN_HEIGHT = 768d;

        #region DependencyProperties
        internal static readonly DependencyProperty AnalyzingStateProperty =
            DependencyProperty.Register("AnalyzingState", typeof(AnalyzingStates), typeof(MainPage),
                new PropertyMetadata(AnalyzingStates.Sleeping));

        internal static readonly DependencyProperty SelectedFileProperty =
            DependencyProperty.Register(nameof(SelectedFile), typeof(IStorageItem), typeof(MainPage),
                new PropertyMetadata(default(IStorageItem), OnSelectedFilePropertyChangedCallback));

        private static readonly DependencyProperty HasSelectedFileProperty =
            DependencyProperty.Register(nameof(HasSelectedFile), typeof(bool), typeof(MainPage),
                new PropertyMetadata(default(bool)));

        internal static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register(nameof(ProgressValue), typeof(double), typeof(MainPage),
                new PropertyMetadata(default(double)));

        private static void OnSelectedFilePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is MainPage page && page.IsLoaded)
            {
                bool hasFile = e.NewValue != null;
                page.AnalyzingState = (page.HasSelectedFile = hasFile) ?
                    AnalyzingStates.Analyzing :
                    AnalyzingStates.Sleeping;

                if(hasFile)
                {
                    page.StartAnalyzingAsync();
                }
                else
                {
                    page.ClearValue(ProgressValueProperty);
                }
            }
        }
        #endregion

        public AnalyzingStates AnalyzingState
        {
            get => (AnalyzingStates)this.GetValue(AnalyzingStateProperty);
            internal set => this.SetValue(AnalyzingStateProperty, value);
        }

        public IStorageItem SelectedFile
        {
            get => this.GetValue(SelectedFileProperty) as IStorageItem;
            internal set => this.SetValue(SelectedFileProperty, value);
        }

        public bool HasSelectedFile
        {
            get => this.GetValue(HasSelectedFileProperty) is bool b && b;
            private set => this.SetValue(HasSelectedFileProperty, value);
        }

        internal double ProgressValue
        {
            get => (double)this.GetValue(ProgressValueProperty);
            set => this.SetValue(ProgressValueProperty, value);
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < MIN_WIDTH || e.NewSize.Height < MIN_HEIGHT)
            {
                ApplicationView.GetForCurrentView().TryResizeView(new Size(MIN_WIDTH, MIN_HEIGHT));
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(MIN_WIDTH, MIN_HEIGHT));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.UnloadFileAndCancel();
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Carregar Apontamentos";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private async void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                try
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    foreach (var item in items)
                    {
                        if(item.Name.EndsWith(".csv"))
                        {
                            this.LoadFileCSV(item);
                        }
                        else
                        {
                            throw new FormatException($"Arquivo \"{item.Name}\" não é tipo .csv!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageDialogBuilder.Builder(ex)
                        .OkCommand()
                        .Show();
                }
            }
        }

        private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fop = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            fop.FileTypeFilter.Add(".csv");
            this.LoadFileCSV(await fop.PickSingleFileAsync());
        }

        private void ClearFileButton_Click(object sender, RoutedEventArgs e)
        {
            if(AnalyzingState == AnalyzingStates.Analyzing)
            {
                MessageDialogBuilder
                    .Builder("Operação cancelada pelo usuário!")
                    .Title("Atenção")
                    .OkCommand().Show();
            }
            this.UnloadFileAndCancel();
        }

        private void LoadFileCSV(IStorageItem file)
        {
            this.SelectedFile = file;
        }

        private void UnloadFileAndCancel()
        {
            this.ClearValue(SelectedFileProperty);
        }

        private async void StartAnalyzingAsync()
        {
            using (Analyzer a = new Analyzer(this.SelectedFile)
                .SetSeparatorChar(';')
                .SetKeys("IdApontamento", "DataInicio", "DataFim", "NumeroLote", "IdEvento", "Quantidade")
                .UseCached())
            {
                Gaps gaps = await a.ReduceAsync<Gaps, Apontamento>(ApontamentoHelper.FromEntry, GapsHelper.CalcularGap);

                //Stopwatch sw = Stopwatch.StartNew();
                //await a.ForEachAsync((e, i) => { });
                //sw.Stop();
                //Debug.WriteLine(sw.ElapsedMilliseconds);

                //sw = Stopwatch.StartNew();
                //await a.ForEachAsync((e, i) => { });
                //sw.Stop();
                //Debug.WriteLine(sw.ElapsedMilliseconds);
            }
        }
    }
}
