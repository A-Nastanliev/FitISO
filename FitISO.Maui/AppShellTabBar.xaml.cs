using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace FitISO.Maui;

public partial class AppShellTabBar
{
    private ShellItem Item => BindingContext as ShellItem ??
                              throw new InvalidOperationException(
                                  "AppShellTabBar must have a ShellItem as its BindingContext");

    private readonly List<ShellSection> _trackedSections = new();

    public static AppShellTabBar? Current { get; private set; }

    private string _currentDefaultGlyph = string.Empty;

    public static readonly BindableProperty CommandProperty =
        BindableProperty.CreateAttached("Command", typeof(ICommand), typeof(AppShellTabBar),
            null, propertyChanged: OnTabBarAppearancePropertyChanged);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.CreateAttached("CommandParameter", typeof(object), typeof(AppShellTabBar),
            null, propertyChanged: OnTabBarAppearancePropertyChanged);

    public static readonly BindableProperty IconProperty =
        BindableProperty.CreateAttached("Icon", typeof(string), typeof(AppShellTabBar),
            null, propertyChanged: OnTabBarAppearancePropertyChanged);

    public static ICommand? GetCommand(BindableObject view) => (ICommand?)view.GetValue(CommandProperty);
    public static void SetCommand(BindableObject view, ICommand? value) => view.SetValue(CommandProperty, value);

    public static object? GetCommandParameter(BindableObject view) => view.GetValue(CommandParameterProperty);
    public static void SetCommandParameter(BindableObject view, object? value) => view.SetValue(CommandParameterProperty, value);

    public static string? GetIcon(BindableObject view) => (string?)view.GetValue(IconProperty);
    public static void SetIcon(BindableObject view, string? value) => view.SetValue(IconProperty, value);


    private void OnLoaded(object? sender, EventArgs e)
    {
        RefreshSelectedButtonCommand();
    }

    private static void OnTabBarAppearancePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (Current is not null && ReferenceEquals(bindable, Shell.Current?.CurrentPage))
        {
            Current.RefreshSelectedButtonCommand();
        }
    }

    public void RefreshSelectedButtonCommand()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return;

        ApplySelectedGlyph(page);
    }

    private void ApplySelectedGlyph(Page? page)
    {
        var overrideGlyph = page is not null ? GetIcon(page) : null;
        var glyph = !string.IsNullOrEmpty(overrideGlyph) ? overrideGlyph : _currentDefaultGlyph;
        ((FontImageSource)SelectedButton.Source).Glyph = glyph;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        RefreshSelectedButtonCommand();
    }

    public AppShellTabBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (ShellItem is not null)
        {
            ShellItem.PropertyChanged -= OnCurrentItemChanged;
            if (ShellItem.Items is INotifyCollectionChanged oldObservableItems)
            {
                oldObservableItems.CollectionChanged -= OnItemsCollectionChanged;
            }
        }

        UnsubscribeFromSections();

        if (Shell.Current is not null)
        {
            Shell.Current.Navigated -= OnShellNavigated;
        }

        if (BindingContext is ShellItem item)
        {
            ShellItem = item;
            item.PropertyChanged += OnCurrentItemChanged;

            if (item.Items is INotifyCollectionChanged observableItems)
            {
                observableItems.CollectionChanged += OnItemsCollectionChanged;
            }

            SubscribeToSections();

            Current = this;
            if (Shell.Current is not null)
            {
                Shell.Current.Navigated += OnShellNavigated;
            }

            var container = (View)SelectedShape.Parent.Parent;
            container.SizeChanged -= OnContainerSizeChanged;
            container.SizeChanged += OnContainerSizeChanged;

            RebuildButtons(animate: false);
            RefreshSelectedButtonCommand();
        }
    }

    private void OnContainerSizeChanged(object? sender, EventArgs e)
    {
        if (Opacity >= 1)
            return;

        if (((View)sender!).Width <= 0)
            return;

        UpdateCurrentItem(Item.CurrentItem, animate: false);
    }

    public ShellItem? ShellItem { get; set; }

    private List<ShellSection> VisibleSections => Item.Items.Where(i => i.IsVisible).ToList();

    private void SubscribeToSections()
    {
        foreach (var section in Item.Items)
        {
            section.PropertyChanged += OnSectionPropertyChanged;
            _trackedSections.Add(section);
        }
    }

    private void UnsubscribeFromSections()
    {
        foreach (var section in _trackedSections)
        {
            section.PropertyChanged -= OnSectionPropertyChanged;
        }

        _trackedSections.Clear();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeFromSections();
        SubscribeToSections();
        RebuildButtons(animate: false);
    }

    private void OnSectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BaseShellItem.IsVisible))
        {
            return;
        }

        var section = (ShellSection)sender!;
        var wasCurrent = section == Item.CurrentItem;

        RebuildButtons(animate: false);

        if (wasCurrent && !section.IsVisible)
        {
            var fallback = VisibleSections.FirstOrDefault();
            if (fallback is not null)
            {
                _ = Shell.Current.GoToAsync($"//{fallback.CurrentItem.Route}");
            }
        }
    }

    private void RebuildButtons(bool animate)
    {
        var visibleSections = VisibleSections;

        foreach (var child in Buttons.Children.OfType<ImageButton>())
        {
            child.Clicked -= IconClicked;
        }

        Buttons.Children.Clear();
        Buttons.ColumnDefinitions.Clear();

        for (var i = 0; i < visibleSections.Count; i++)
        {
            Buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var glyph = (visibleSections[i].Icon as FontImageSource)?.Glyph ?? string.Empty;

            var button = new ImageButton
            {
                Source = new FontImageSource { Glyph = glyph },
            };
            button.Clicked += IconClicked;
            Grid.SetColumn(button, i);
            Buttons.Children.Add(button);
        }

        UpdateCurrentItem(Item.CurrentItem, animate);
    }

    private void OnCurrentItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == ShellItem.CurrentItemProperty.PropertyName)
        {
            UpdateCurrentItem(ShellItem!.CurrentItem);
        }
    }

    private void UpdateCurrentItem(ShellSection currentItem, bool animate = true)
    {
        this.CancelAnimations();

        var visibleSections = VisibleSections;
        var numButtons = (double)Buttons.Children.Count;
        if (numButtons == 0)
        {
            return;
        }

        var selectedIndex = Math.Max(0, visibleSections.IndexOf(currentItem));

        for (var i = 0; i < Buttons.Children.Count; i++)
        {
            if (Buttons.Children[i] is ImageButton btn)
            {
                btn.Opacity = i == selectedIndex ? 0 : 1;
            }
        }

        var startPosition = TabBarShape.InsetPosition;
        var buttonFractionalOffset = numButtons > 1 ? 1.0 / (numButtons - 1) : 0.5;
        var endPosition = buttonFractionalOffset * selectedIndex;
        var startTranslationX = SelectedShape.TranslationX;
        var availableTranslationWidth = ((View)SelectedShape.Parent.Parent).Width;

        if (availableTranslationWidth <= 0)
        {
            return;
        }

        var selectedShapeWidth = SelectedShape.Width > 0 ? SelectedShape.Width : 56;
        var endInsetStartX = endPosition * (availableTranslationWidth - TabBarShape.InsetWidth);
        var endTranslationX = endInsetStartX + TabBarShape.InsetWidth / 2 - selectedShapeWidth / 2;

        if (!animate)
        {
            TabBarShape.InsetPosition = (float)endPosition;
            SelectedShapeContainer.TranslationX = endTranslationX;

            if (Buttons.Children.Count > selectedIndex &&
                Buttons.Children[selectedIndex] is ImageButton currentButton &&
                currentButton.Source is FontImageSource currentSource)
            {
                _currentDefaultGlyph = currentSource.Glyph;
                RefreshSelectedButtonCommand();
            }

            Opacity = 1;
            return;
        }

        Opacity = 1;
        AnimateSelectedShapeJump(selectedIndex);

        SelectedShapeContainer.TranslationX = startTranslationX - 0.001;
        this.Animate("CurrentItem",
            v =>
            {
                TabBarShape.InsetPosition = (float)(startPosition + (endPosition - startPosition) * v);
                SelectedShapeContainer.TranslationX = startTranslationX + (endTranslationX - startTranslationX) * v;
            },
            length: 250);
    }

    private const string SelectedJumpOut = nameof(SelectedJumpOut);
    private const string SelectedJumpIn = nameof(SelectedJumpIn);

    private void AnimateSelectedShapeJump(int selectedIndex, double deltaY = 50)
    {
        SelectedShapeContainer.ZIndex = 0;
        var startTranslationY = SelectedShape.TranslationY;
        var middleTranslationY = deltaY;
        var startOpacity = SelectedButton.Opacity;
        var middleOpacity = 0f;
        var endTranslationY = 0;
        var endOpacity = 1f;
        this.Animate(
            SelectedJumpOut,
            v =>
            {
                SelectedShape.TranslationY = startTranslationY + (middleTranslationY - startTranslationY) * v;
                SelectedButton.Opacity = startOpacity + (middleOpacity - startOpacity) * v;
            },
            length: 125,
            finished: (_, canceled) =>
            {
                if (canceled)
                {
                    return;
                }

                if (Buttons.Children.Count > selectedIndex &&
                    Buttons.Children[selectedIndex] is ImageButton selectedSourceButton &&
                    selectedSourceButton.Source is FontImageSource selectedFontSource)
                {
                    _currentDefaultGlyph = selectedFontSource.Glyph;
                    RefreshSelectedButtonCommand();
                }

                this.Animate(
                    SelectedJumpIn,
                    v =>
                    {
                        SelectedShape.TranslationY = middleTranslationY + (endTranslationY - middleTranslationY) * v;
                        SelectedButton.Opacity = middleOpacity + (endOpacity - middleOpacity) * v;
                    },
                    finished: (_, canceled2) =>
                    {
                        if (canceled2)
                        {
                            return;
                        }

                        SelectedShapeContainer.ZIndex = 2;
                    }
                );
            }
        );
    }

    private async void IconClicked(object? sender, EventArgs e)
    {
        var icon = (ImageButton)sender!;
        var parent = (Layout)icon.Parent!;
        var index = parent.IndexOf(icon);

        var visibleSections = VisibleSections;
        if (index < 0 || index >= visibleSections.Count)
        {
            return;
        }

        var targetSection = visibleSections[index];
        if (targetSection == Item.CurrentItem)
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{targetSection.CurrentItem.Route}");
    }

    private void SelectedButtonClicked(object? sender, EventArgs e)
    {
        this.AbortAnimation(SelectedJumpIn);
        this.AbortAnimation(SelectedJumpOut);

        var page = Shell.Current?.CurrentPage;
        if (page is not null)
        {
            var command = GetCommand(page);
            var parameter = GetCommandParameter(page);

            if (command?.CanExecute(parameter) == true)
                command.Execute(parameter);
        }

        var index = VisibleSections.IndexOf(Item.CurrentItem);
        AnimateSelectedShapeJump(Math.Max(0, index), 25);
    }
}