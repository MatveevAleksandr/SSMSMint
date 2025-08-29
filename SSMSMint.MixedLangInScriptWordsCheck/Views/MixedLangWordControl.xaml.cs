using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SSMSMint.MixedLangInScriptWordsCheck.Views;

/// <summary>
/// Interaction logic for MixedLangWordControl.xaml
/// </summary>
public partial class MixedLangWordControl : UserControl
{
    public static readonly DependencyProperty LineIndexProperty = DependencyProperty.Register("LineIndex", typeof(int), typeof(MixedLangWordControl));
    public static readonly DependencyProperty ColumnIndexProperty = DependencyProperty.Register("ColumnIndex", typeof(int), typeof(MixedLangWordControl));
    public static readonly DependencyProperty WordProperty = DependencyProperty.Register("Word", typeof(string), typeof(MixedLangWordControl), new PropertyMetadata(OnHighlightParamsChange));

    public int LineIndex
    {
        get => (int)GetValue(LineIndexProperty);
        set => SetValue(LineIndexProperty, value);
    }
    public int ColumnIndex
    {
        get => (int)GetValue(ColumnIndexProperty);
        set => SetValue(ColumnIndexProperty, value);
    }
    public string Word
    {
        get => (string)GetValue(WordProperty);
        set => SetValue(WordProperty, value);
    }

    public MixedLangWordControl()
    {
        InitializeComponent();
    }

    private static void OnHighlightParamsChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MixedLangWordControl control)
            return;

        if (control.Word == null)
            return;

        control.TBWord.Inlines.Clear();

        var regex = new Regex(@"([а-яА-ЯёЁ]+)|([a-zA-Z]+)|([^a-zA-Zа-яА-ЯёЁ]+)");

        foreach (Match match in regex.Matches(control.Word))
        {
            var value = match.Value;
            var run = new Run(value);

            if (Regex.IsMatch(value, @"[а-яА-ЯёЁ]"))
            {
                run.Foreground = Brushes.Red;
            }
            else if (Regex.IsMatch(value, @"[a-zA-Z]"))
            {
                run.Foreground = Brushes.Blue;
            }
            else
            {
                run.Foreground = control.TBWord.Foreground;
            }

            control.TBWord.Inlines.Add(run);
        }
    }
}
