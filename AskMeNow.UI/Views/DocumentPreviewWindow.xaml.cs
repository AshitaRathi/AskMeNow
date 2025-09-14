using AskMeNow.Core.Entities;
using System.Windows;
using System.Windows.Controls;

namespace AskMeNow.UI.Views;

public partial class DocumentPreviewWindow : Window
{
    public DocumentPreview Document { get; set; }

    public DocumentPreviewWindow(DocumentPreview document)
    {
        InitializeComponent();
        Document = document;
        DataContext = Document;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CopyContentButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(Document?.Content))
            {
                Clipboard.SetText(Document.Content);
                
                // Show a brief success message
                var button = sender as Button;
                if (button != null)
                {
                    var originalContent = button.Content;
                    button.Content = "✓ Copied!";
                    button.IsEnabled = false;
                    
                    // Reset after 2 seconds
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Tick += (s, args) =>
                    {
                        button.Content = originalContent;
                        button.IsEnabled = true;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy content to clipboard: {ex.Message}", 
                          "Copy Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Warning);
        }
    }
}
