using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SSMSMint.ResultsGridSearch.Views;

/// <summary>
/// Interaction logic for SearchResultItemControl.xaml
/// </summary>
public partial class SearchResultItemControl : UserControl
{
    public static readonly DependencyProperty GridIndexProperty =
        DependencyProperty.Register("GridIndex", typeof(int), typeof(SearchResultItemControl));
    public static readonly DependencyProperty RowIndexProperty =
        DependencyProperty.Register("RowIndex", typeof(long), typeof(SearchResultItemControl));
    public static readonly DependencyProperty ColHeaderProperty =
        DependencyProperty.Register("ColHeader", typeof(string), typeof(SearchResultItemControl));
    public static readonly DependencyProperty CellDataProperty =
        DependencyProperty.Register("CellData", typeof(string), typeof(SearchResultItemControl), new PropertyMetadata(OnHighlightParamsChange));
    public static readonly DependencyProperty HighlightTextProperty =
        DependencyProperty.Register("HighlightText", typeof(string), typeof(SearchResultItemControl), new PropertyMetadata(OnHighlightParamsChange));
    public static readonly DependencyProperty MatchCaseProperty =
        DependencyProperty.Register("MatchCase", typeof(bool), typeof(SearchResultItemControl), new PropertyMetadata(OnHighlightParamsChange));

    public int GridIndex
    {
        get => (int)GetValue(GridIndexProperty);
        set => SetValue(GridIndexProperty, value);
    }
    public long RowIndex
    {
        get => (long)GetValue(RowIndexProperty);
        set => SetValue(RowIndexProperty, value);
    }
    public string ColHeader
    {
        get => (string)GetValue(ColHeaderProperty);
        set => SetValue(ColHeaderProperty, value);
    }
    public string CellData
    {
        get => (string)GetValue(CellDataProperty);
        set => SetValue(CellDataProperty, value);
    }
    public string HighlightText
    {
        get => (string)GetValue(HighlightTextProperty);
        set => SetValue(HighlightTextProperty, value);
    }
    public bool MatchCase
    {
        get => (bool)GetValue(MatchCaseProperty);
        set => SetValue(MatchCaseProperty, value);
    }

    public SearchResultItemControl()
    {
        InitializeComponent();
    }

    private static void OnHighlightParamsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SearchResultItemControl control)
            return;

        control.UpdateHighlight();
    }

    // Будем выделять цветом часть текста совпадающую с поисковым запросом
    private void UpdateHighlight()
    {
        TBCellData.Inlines.Clear();

        if (string.IsNullOrEmpty(CellData) || string.IsNullOrEmpty(HighlightText))
        {
            TBCellData.Inlines.Add(new Run(CellData ?? string.Empty));
            return;
        }

        var sc = MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        int idx = 0;

        while (true)
        {
            int found = CellData.IndexOf(HighlightText, idx, sc);
            if (found < 0)
            {
                TBCellData.Inlines.Add(new Run(CellData.Substring(idx)));
                break;
            }
            if (found > idx)
            {
                TBCellData.Inlines.Add(new Run(CellData.Substring(idx, found - idx)));
            }

            TBCellData.Inlines.Add(new Run(CellData.Substring(found, HighlightText.Length))
            {
                Foreground = Brushes.Red
            });
            idx = found + HighlightText.Length;
        }
    }
}
