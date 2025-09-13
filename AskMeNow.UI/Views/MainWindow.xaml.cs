using AskMeNow.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AskMeNow.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Subscribe to messages collection changes for auto-scrolling
        if (viewModel.Messages is System.Collections.Specialized.INotifyCollectionChanged observableCollection)
        {
            observableCollection.CollectionChanged += (s, e) => ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (ChatScrollViewer != null)
        {
            ChatScrollViewer.ScrollToEnd();
        }
    }
}
