using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FlowPainter.Application.Workflow;

namespace FlowPainter.App;

internal sealed class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog(string projectName)
    {
        string displayName = string.IsNullOrWhiteSpace(projectName)
            ? "the current project"
            : $"'{projectName.Trim()}'";

        Title = "Unsaved changes";
        Width = 520;
        SizeToContent = Avalonia.Controls.SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        TextBlock message = new()
        {
            Text = $"Save changes to {displayName} before continuing?",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            Foreground = Brushes.White
        };
        TextBlock explanation = new()
        {
            Text = "Choosing Discard permanently removes the unsaved project changes.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#AEB4BF"))
        };

        Button saveButton = new()
        {
            Content = "Save",
            MinWidth = 100,
            Padding = new Thickness(16, 8)
        };
        saveButton.Click += (_, _) => Close(UnsavedChangesDecision.Save);

        Button discardButton = new()
        {
            Content = "Discard",
            MinWidth = 100,
            Padding = new Thickness(16, 8)
        };
        discardButton.Click += (_, _) => Close(UnsavedChangesDecision.Discard);

        Button cancelButton = new()
        {
            Content = "Cancel",
            MinWidth = 100,
            Padding = new Thickness(16, 8)
        };
        cancelButton.Click += (_, _) => Close(UnsavedChangesDecision.Cancel);

        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children =
            {
                cancelButton,
                discardButton,
                saveButton
            }
        };

        Content = new Border
        {
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Color.Parse("#202329")),
            Child = new StackPanel
            {
                Spacing = 14,
                Children =
                {
                    message,
                    explanation,
                    buttons
                }
            }
        };
    }
}
