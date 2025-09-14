using AskMeNow.Core.Entities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AskMeNow.UI.Views;

public partial class HighlightedDocumentViewer : UserControl
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(DocumentPreview), typeof(HighlightedDocumentViewer),
            new PropertyMetadata(null, OnDocumentChanged));

    public DocumentPreview Document
    {
        get => (DocumentPreview)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public HighlightedDocumentViewer()
    {
        InitializeComponent();
    }

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightedDocumentViewer viewer && e.NewValue is DocumentPreview document)
        {
            viewer.RenderDocumentWithHighlights(document);
        }
    }

    private void RenderDocumentWithHighlights(DocumentPreview document)
    {
        if (string.IsNullOrEmpty(document.Content))
        {
            DocumentContentTextBlock.Text = "No content available.";
            return;
        }

        // Create a FlowDocument for rich text rendering
        var flowDocument = new FlowDocument
        {
            Background = Brushes.White,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            LineHeight = 24
        };

        var paragraph = new Paragraph
        {
            Margin = new Thickness(0)
        };

        // Sort highlights by start index
        var sortedHighlights = document.Highlights
            .OrderBy(h => h.StartIndex)
            .ToList();

        var content = document.Content;
        var lastIndex = 0;

        foreach (var highlight in sortedHighlights)
        {
            // Add text before highlight
            if (highlight.StartIndex > lastIndex)
            {
                var beforeText = content.Substring(lastIndex, highlight.StartIndex - lastIndex);
                paragraph.Inlines.Add(new Run(beforeText));
            }

            // Add highlighted text
            var highlightedText = content.Substring(highlight.StartIndex, highlight.EndIndex - highlight.StartIndex);
            var run = new Run(highlightedText);
            
            // Apply highlight styling
            var highlightStyle = GetHighlightStyle(highlight.Type);
            run.Background = highlightStyle.Background;
            run.Foreground = highlightStyle.Foreground;
            
            // Add tooltip
            var tooltip = new ToolTip();
            var tooltipContent = new StackPanel();
            tooltipContent.Children.Add(new TextBlock
            {
                Text = GetHighlightTypeName(highlight.Type),
                FontWeight = FontWeights.Bold
            });
            tooltipContent.Children.Add(new TextBlock
            {
                Text = highlight.Tooltip
            });
            tooltip.Content = tooltipContent;
            run.ToolTip = tooltip;

            paragraph.Inlines.Add(run);
            lastIndex = highlight.EndIndex;
        }

        // Add remaining text
        if (lastIndex < content.Length)
        {
            var remainingText = content.Substring(lastIndex);
            paragraph.Inlines.Add(new Run(remainingText));
        }

        flowDocument.Blocks.Add(paragraph);

        // Create a RichTextBox to display the FlowDocument
        var richTextBox = new RichTextBox
        {
            Document = flowDocument,
            IsReadOnly = true,
            Background = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };

        // Replace the TextBlock with the RichTextBox
        var parent = DocumentContentTextBlock.Parent as Panel;
        if (parent != null)
        {
            var index = parent.Children.IndexOf(DocumentContentTextBlock);
            parent.Children.Remove(DocumentContentTextBlock);
            parent.Children.Insert(index, richTextBox);
        }
    }

    private (Brush Background, Brush Foreground) GetHighlightStyle(HighlightType type)
    {
        return type switch
        {
            HighlightType.FrequentlyReferenced => (new SolidColorBrush(Color.FromRgb(254, 243, 199)), Brushes.Black),
            HighlightType.HighRelevance => (new SolidColorBrush(Color.FromRgb(219, 234, 254)), Brushes.Black),
            HighlightType.RecentReference => (new SolidColorBrush(Color.FromRgb(243, 232, 255)), Brushes.Black),
            HighlightType.KeyConcept => (new SolidColorBrush(Color.FromRgb(236, 253, 245)), Brushes.Black),
            _ => (Brushes.Transparent, Brushes.Black)
        };
    }

    private string GetHighlightTypeName(HighlightType type)
    {
        return type switch
        {
            HighlightType.FrequentlyReferenced => "Frequently Referenced",
            HighlightType.HighRelevance => "High Relevance",
            HighlightType.RecentReference => "Recent Reference",
            HighlightType.KeyConcept => "Key Concept",
            _ => "Highlight"
        };
    }
}
