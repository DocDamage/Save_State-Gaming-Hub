using Avalonia;
using Avalonia.Controls;
using SaveState.Core.TournamentManagement.Models;

namespace SaveState.Presentation.Views.Esports;

/// <summary>
/// Custom control for visualizing tournament brackets.
/// </summary>
public partial class BracketView : UserControl
{
    /// <summary>
    /// Defines the Tournament property.
    /// </summary>
    public static readonly StyledProperty<Tournament?> TournamentProperty =
        AvaloniaProperty.Register<BracketView, Tournament?>(nameof(Tournament));

    /// <summary>
    /// Defines the Bracket property.
    /// </summary>
    public static readonly StyledProperty<TournamentBracket?> BracketProperty =
        AvaloniaProperty.Register<BracketView, TournamentBracket?>(nameof(Bracket));

    /// <summary>
    /// Gets or sets the tournament data.
    /// </summary>
    public Tournament? Tournament
    {
        get => GetValue(TournamentProperty);
        set => SetValue(TournamentProperty, value);
    }

    /// <summary>
    /// Gets or sets the bracket data.
    /// </summary>
    public TournamentBracket? Bracket
    {
        get => GetValue(BracketProperty);
        set => SetValue(BracketProperty, value);
    }

    public BracketView()
    {
        InitializeComponent();
    }
}
