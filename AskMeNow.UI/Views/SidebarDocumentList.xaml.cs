using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using AskMeNow.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AskMeNow.UI.Views;

public partial class SidebarDocumentList : UserControl
{
    private IDocumentPreviewService? _documentPreviewService;

    public SidebarDocumentList()
    {
        InitializeComponent();
    }

    public SidebarDocumentList(IDocumentPreviewService documentPreviewService) : this()
    {
        _documentPreviewService = documentPreviewService;
    }

    private async void DocumentItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is DocumentInfo documentInfo)
        {
            try
            {
                // Get the service from the DataContext (MainViewModel) if not injected
                var service = _documentPreviewService;
                if (service == null && DataContext is MainViewModel viewModel)
                {
                    service = viewModel.DocumentPreviewService;
                }

                if (service == null)
                {
                    MessageBox.Show("Document preview service is not available.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show loading indicator or disable UI here if needed
                var documentPreview = await service.GetDocumentPreviewAsync(documentInfo.FilePath);
                
                if (documentPreview != null)
                {
                    var previewWindow = new DocumentPreviewWindow(documentPreview)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    
                    previewWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Unable to load document preview.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document preview: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
