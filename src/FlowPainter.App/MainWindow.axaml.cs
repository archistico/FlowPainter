using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using IoPath = System.IO.Path;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Application.Interaction;
using FlowPainter.Application.Images;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Semantics;
using FlowPainter.Application.Workflow;
using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Strokes;
using FlowPainter.Domain.Semantics;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Detail;
using FlowPainter.Rendering.Skia.Strokes;

namespace FlowPainter.App;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Avalonia owns the Window lifetime; native resources are released after the Closed event and any active operation has stopped.")]
public partial class MainWindow : Window
{
    private const ulong InitialSeed = 0xF10A_2026UL;
    private const double MinimumManualRegionSize = 0.002d;

    private readonly SkiaImageLoader _imageLoader = new();
    private readonly SkiaImageProxyGenerator _proxyGenerator = new();
    private readonly SkiaStrokePlanRenderer _renderer = new();
    private readonly SkiaPngEncoder _pngEncoder = new();
    private readonly SkiaImageEncoder _imageEncoder = new();
    private readonly ImageDetailAnalyzer _detailAnalyzer = new();
    private readonly HeuristicSemanticImportanceAnalyzer _semanticAnalyzer = new();
    private readonly DetailMapOverlayRenderer _detailOverlayRenderer = new();
    private readonly FlowPainterPlanner _planner = new(new DefaultFlowFieldFactory());
    private readonly FlowPainterWorkspace _workspace = new(InitialSeed, BuiltInFlowPainterPresets.All[0].Settings);
    private readonly RecentPathList _recentProjects = new();
    private readonly RecentPathList _recentPresets = new();

    private readonly Button _openButton;
    private readonly Button _openProjectButton;
    private readonly Button _saveProjectButton;
    private readonly Button _renderButton;
    private readonly Button _cancelButton;
    private readonly Button _saveButton;
    private readonly Button _exportFinalButton;
    private readonly StackPanel _settingsPanel;
    private readonly ComboBox _presetComboBox;
    private readonly ComboBox _previewQualityComboBox;
    private readonly ComboBox _finalFormatComboBox;
    private readonly ComboBox _recentProjectComboBox;
    private readonly ComboBox _recentPresetComboBox;
    private readonly ComboBox _flowFieldComboBox;
    private readonly ComboBox _backgroundComboBox;
    private readonly ComboBox _brushKindComboBox;
    private readonly ComboBox _detailIntentComboBox;
    private readonly ComboBox _semanticOverlayModeComboBox;
    private readonly TextBox _presetNameTextBox;
    private readonly TextBox _projectNameTextBox;
    private readonly TextBox _finalMaximumDimensionTextBox;
    private readonly TextBox _jpegQualityTextBox;
    private readonly TextBox _seedTextBox;
    private readonly TextBox _strokeCountTextBox;
    private readonly TextBox _segmentCountTextBox;
    private readonly TextBox _fieldScaleTextBox;
    private readonly TextBox _octavesTextBox;
    private readonly TextBox _persistenceTextBox;
    private readonly TextBox _lacunarityTextBox;
    private readonly TextBox _angleOffsetTextBox;
    private readonly TextBox _densityTextBox;
    private readonly TextBox _lengthScaleTextBox;
    private readonly TextBox _maximumCurveTextBox;
    private readonly TextBox _minimumWidthTextBox;
    private readonly TextBox _maximumWidthTextBox;
    private readonly TextBox _opacityTextBox;
    private readonly TextBox _brushHardnessTextBox;
    private readonly TextBox _brushSizeJitterTextBox;
    private readonly TextBox _brushOpacityJitterTextBox;
    private readonly TextBox _brushBristleCountTextBox;
    private readonly TextBox _brushBristleSpreadTextBox;
    private readonly TextBox _baseDetailTextBox;
    private readonly TextBox _edgeWeightTextBox;
    private readonly TextBox _contrastWeightTextBox;
    private readonly TextBox _smoothingRadiusTextBox;
    private readonly TextBox _placementBiasTextBox;
    private readonly TextBox _detailedLengthTextBox;
    private readonly TextBox _backgroundLengthTextBox;
    private readonly TextBox _detailedWidthTextBox;
    private readonly TextBox _backgroundWidthTextBox;
    private readonly TextBox _regionStrengthTextBox;
    private readonly TextBox _semanticInfluenceTextBox;
    private readonly TextBox _semanticSaliencyWeightTextBox;
    private readonly TextBox _semanticSubjectWeightTextBox;
    private readonly TextBox _semanticSilhouetteWeightTextBox;
    private readonly TextBox _semanticFocalWeightTextBox;
    private readonly TextBox _semanticThresholdTextBox;
    private readonly TextBox _semanticMinimumAreaTextBox;
    private readonly TextBox _semanticMaximumSubjectsTextBox;
    private readonly TextBox _semanticCenterBiasTextBox;
    private readonly TextBox _semanticSmoothingRadiusTextBox;
    private readonly TextBox _semanticBoundaryRadiusTextBox;
    private readonly ListBox _regionListBox;
    private readonly ListBox _semanticRegionListBox;
    private readonly TextBox _regionLabelTextBox;
    private readonly TextBox _regionLeftTextBox;
    private readonly TextBox _regionTopTextBox;
    private readonly TextBox _regionWidthTextBox;
    private readonly TextBox _regionHeightTextBox;
    private readonly CheckBox _showDetailOverlayCheckBox;
    private readonly CheckBox _enableSemanticAnalysisCheckBox;
    private readonly Canvas _selectionCanvas;
    private readonly Grid _sourceViewportHost;
    private readonly Grid _sourceViewportContent;
    private readonly Grid _resultViewportHost;
    private readonly Grid _resultViewportContent;
    private readonly MatrixTransform _sourceViewportTransform = new();
    private readonly MatrixTransform _resultViewportTransform = new();
    private readonly Image _sourceImageView;
    private readonly Image _resultImageView;
    private readonly TextBlock _sourceInfoText;
    private readonly TextBlock _resultInfoText;
    private readonly TextBlock _finalOutputInfoText;
    private readonly TextBlock _finalMemoryInfoText;
    private readonly TextBlock _regionCountText;
    private readonly TextBlock _semanticRegionCountText;
    private readonly TextBlock _statusText;
    private readonly ProgressBar _operationProgressBar;

    private SkiaImage? _sourceImage;
    private SkiaImage? _proxyImage;
    private SkiaImage? _renderedImage;
    private Bitmap? _sourcePreviewBitmap;
    private Bitmap? _detailOverlayPreviewBitmap;
    private Bitmap? _semanticOverlayPreviewBitmap;
    private Bitmap? _resultPreviewBitmap;
    private DetailMap? _automaticDetailMap;
    private DetailMap? _composedDetailMap;
    private SemanticAnalysisResult? _semanticAnalysisResult;
    private StrokePlan? _previewStrokePlan;
    private BrushSettings? _previewBrushSettings;
    private DetailAnalysisSettings? _activeDetailAnalysisSettings;
    private SemanticAnalysisSettings? _activeSemanticAnalysisSettings;
    private readonly SynchronizedImageViewportState _imageViewportState = new();
    private NormalizedPoint? _selectionStart;
    private NormalizedPoint? _selectionCurrent;
    private Grid? _panViewportHost;
    private Point _panLastPosition;
    private CancellationTokenSource? _operationCancellation;
    private string? _currentSourcePath;
    private string? _currentProjectPath;
    private bool _suppressRegionSelectionChange;
    private bool _isClosed;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _openButton = FindRequiredControl<Button>("OpenButton");
        _openProjectButton = FindRequiredControl<Button>("OpenProjectButton");
        _saveProjectButton = FindRequiredControl<Button>("SaveProjectButton");
        _renderButton = FindRequiredControl<Button>("RenderButton");
        _cancelButton = FindRequiredControl<Button>("CancelButton");
        _saveButton = FindRequiredControl<Button>("SaveButton");
        _exportFinalButton = FindRequiredControl<Button>("ExportFinalButton");
        _settingsPanel = FindRequiredControl<StackPanel>("SettingsPanel");
        _presetComboBox = FindRequiredControl<ComboBox>("PresetComboBox");
        _previewQualityComboBox = FindRequiredControl<ComboBox>("PreviewQualityComboBox");
        _finalFormatComboBox = FindRequiredControl<ComboBox>("FinalFormatComboBox");
        _recentProjectComboBox = FindRequiredControl<ComboBox>("RecentProjectComboBox");
        _recentPresetComboBox = FindRequiredControl<ComboBox>("RecentPresetComboBox");
        _flowFieldComboBox = FindRequiredControl<ComboBox>("FlowFieldComboBox");
        _backgroundComboBox = FindRequiredControl<ComboBox>("BackgroundComboBox");
        _brushKindComboBox = FindRequiredControl<ComboBox>("BrushKindComboBox");
        _detailIntentComboBox = FindRequiredControl<ComboBox>("DetailIntentComboBox");
        _semanticOverlayModeComboBox = FindRequiredControl<ComboBox>("SemanticOverlayModeComboBox");
        _presetNameTextBox = FindRequiredControl<TextBox>("PresetNameTextBox");
        _projectNameTextBox = FindRequiredControl<TextBox>("ProjectNameTextBox");
        _finalMaximumDimensionTextBox = FindRequiredControl<TextBox>("FinalMaximumDimensionTextBox");
        _jpegQualityTextBox = FindRequiredControl<TextBox>("JpegQualityTextBox");
        _seedTextBox = FindRequiredControl<TextBox>("SeedTextBox");
        _strokeCountTextBox = FindRequiredControl<TextBox>("StrokeCountTextBox");
        _segmentCountTextBox = FindRequiredControl<TextBox>("SegmentCountTextBox");
        _fieldScaleTextBox = FindRequiredControl<TextBox>("FieldScaleTextBox");
        _octavesTextBox = FindRequiredControl<TextBox>("OctavesTextBox");
        _persistenceTextBox = FindRequiredControl<TextBox>("PersistenceTextBox");
        _lacunarityTextBox = FindRequiredControl<TextBox>("LacunarityTextBox");
        _angleOffsetTextBox = FindRequiredControl<TextBox>("AngleOffsetTextBox");
        _densityTextBox = FindRequiredControl<TextBox>("DensityTextBox");
        _lengthScaleTextBox = FindRequiredControl<TextBox>("LengthScaleTextBox");
        _maximumCurveTextBox = FindRequiredControl<TextBox>("MaximumCurveTextBox");
        _minimumWidthTextBox = FindRequiredControl<TextBox>("MinimumWidthTextBox");
        _maximumWidthTextBox = FindRequiredControl<TextBox>("MaximumWidthTextBox");
        _opacityTextBox = FindRequiredControl<TextBox>("OpacityTextBox");
        _brushHardnessTextBox = FindRequiredControl<TextBox>("BrushHardnessTextBox");
        _brushSizeJitterTextBox = FindRequiredControl<TextBox>("BrushSizeJitterTextBox");
        _brushOpacityJitterTextBox = FindRequiredControl<TextBox>("BrushOpacityJitterTextBox");
        _brushBristleCountTextBox = FindRequiredControl<TextBox>("BrushBristleCountTextBox");
        _brushBristleSpreadTextBox = FindRequiredControl<TextBox>("BrushBristleSpreadTextBox");
        _baseDetailTextBox = FindRequiredControl<TextBox>("BaseDetailTextBox");
        _edgeWeightTextBox = FindRequiredControl<TextBox>("EdgeWeightTextBox");
        _contrastWeightTextBox = FindRequiredControl<TextBox>("ContrastWeightTextBox");
        _smoothingRadiusTextBox = FindRequiredControl<TextBox>("SmoothingRadiusTextBox");
        _placementBiasTextBox = FindRequiredControl<TextBox>("PlacementBiasTextBox");
        _detailedLengthTextBox = FindRequiredControl<TextBox>("DetailedLengthTextBox");
        _backgroundLengthTextBox = FindRequiredControl<TextBox>("BackgroundLengthTextBox");
        _detailedWidthTextBox = FindRequiredControl<TextBox>("DetailedWidthTextBox");
        _backgroundWidthTextBox = FindRequiredControl<TextBox>("BackgroundWidthTextBox");
        _regionStrengthTextBox = FindRequiredControl<TextBox>("RegionStrengthTextBox");
        _semanticInfluenceTextBox = FindRequiredControl<TextBox>("SemanticInfluenceTextBox");
        _semanticSaliencyWeightTextBox = FindRequiredControl<TextBox>("SemanticSaliencyWeightTextBox");
        _semanticSubjectWeightTextBox = FindRequiredControl<TextBox>("SemanticSubjectWeightTextBox");
        _semanticSilhouetteWeightTextBox = FindRequiredControl<TextBox>("SemanticSilhouetteWeightTextBox");
        _semanticFocalWeightTextBox = FindRequiredControl<TextBox>("SemanticFocalWeightTextBox");
        _semanticThresholdTextBox = FindRequiredControl<TextBox>("SemanticThresholdTextBox");
        _semanticMinimumAreaTextBox = FindRequiredControl<TextBox>("SemanticMinimumAreaTextBox");
        _semanticMaximumSubjectsTextBox = FindRequiredControl<TextBox>("SemanticMaximumSubjectsTextBox");
        _semanticCenterBiasTextBox = FindRequiredControl<TextBox>("SemanticCenterBiasTextBox");
        _semanticSmoothingRadiusTextBox = FindRequiredControl<TextBox>("SemanticSmoothingRadiusTextBox");
        _semanticBoundaryRadiusTextBox = FindRequiredControl<TextBox>("SemanticBoundaryRadiusTextBox");
        _regionListBox = FindRequiredControl<ListBox>("RegionListBox");
        _semanticRegionListBox = FindRequiredControl<ListBox>("SemanticRegionListBox");
        _regionLabelTextBox = FindRequiredControl<TextBox>("RegionLabelTextBox");
        _regionLeftTextBox = FindRequiredControl<TextBox>("RegionLeftTextBox");
        _regionTopTextBox = FindRequiredControl<TextBox>("RegionTopTextBox");
        _regionWidthTextBox = FindRequiredControl<TextBox>("RegionWidthTextBox");
        _regionHeightTextBox = FindRequiredControl<TextBox>("RegionHeightTextBox");
        _showDetailOverlayCheckBox = FindRequiredControl<CheckBox>("ShowDetailOverlayCheckBox");
        _enableSemanticAnalysisCheckBox = FindRequiredControl<CheckBox>("EnableSemanticAnalysisCheckBox");
        _selectionCanvas = FindRequiredControl<Canvas>("SelectionCanvas");
        _sourceViewportHost = FindRequiredControl<Grid>("SourceViewportHost");
        _sourceViewportContent = FindRequiredControl<Grid>("SourceViewportContent");
        _resultViewportHost = FindRequiredControl<Grid>("ResultViewportHost");
        _resultViewportContent = FindRequiredControl<Grid>("ResultViewportContent");
        _sourceViewportContent.RenderTransform = _sourceViewportTransform;
        _resultViewportContent.RenderTransform = _resultViewportTransform;
        _sourceImageView = FindRequiredControl<Image>("SourceImageView");
        _resultImageView = FindRequiredControl<Image>("ResultImageView");
        _sourceInfoText = FindRequiredControl<TextBlock>("SourceInfoText");
        _resultInfoText = FindRequiredControl<TextBlock>("ResultInfoText");
        _finalOutputInfoText = FindRequiredControl<TextBlock>("FinalOutputInfoText");
        _finalMemoryInfoText = FindRequiredControl<TextBlock>("FinalMemoryInfoText");
        _regionCountText = FindRequiredControl<TextBlock>("RegionCountText");
        _semanticRegionCountText = FindRequiredControl<TextBlock>("SemanticRegionCountText");
        _statusText = FindRequiredControl<TextBlock>("StatusText");
        _operationProgressBar = FindRequiredControl<ProgressBar>("OperationProgressBar");

        _presetComboBox.ItemsSource = BuiltInFlowPainterPresets.All;
        _previewQualityComboBox.ItemsSource = Enum.GetValues<PreviewQuality>();
        _previewQualityComboBox.SelectedItem = PreviewQuality.Standard;
        _finalFormatComboBox.ItemsSource = Enum.GetValues<RasterImageFormat>();
        ApplyFinalRenderSettings(new FinalRenderSettings());
        _flowFieldComboBox.ItemsSource = Enum.GetValues<FlowFieldKind>();
        _backgroundComboBox.ItemsSource = Enum.GetValues<StrokePlanBackgroundMode>();
        _brushKindComboBox.ItemsSource = Enum.GetValues<BrushKind>();
        _detailIntentComboBox.ItemsSource = Enum.GetValues<DetailRegionIntent>();
        _semanticOverlayModeComboBox.ItemsSource = Enum.GetValues<SemanticOverlayMode>();
        _semanticOverlayModeComboBox.SelectedItem = SemanticOverlayMode.CombinedDetail;
        _detailIntentComboBox.SelectedItem = DetailRegionIntent.IncreaseDetail;
        _regionStrengthTextBox.Text = "80";
        _projectNameTextBox.Text = "Untitled project";
        LoadRecentItemsAtStartup();
        _presetComboBox.SelectionChanged += (_, _) => ApplySelectedBuiltInPreset();
        ApplyPreset(BuiltInFlowPainterPresets.All[0], InitialSeed);
        _presetComboBox.SelectedIndex = 0;
        Closed += WindowClosed;
    }

    private async void OpenProjectClick(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open FlowPainter project",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("FlowPainter project")
                    {
                        Patterns = ["*.flowpainter.json", "*.json"]
                    }
                ]
            }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return;
        }

        string projectPath = files[0].Path.LocalPath;
        await RunLoadingProjectAsync(cancellationToken => LoadProjectFromPathAsync(
            projectPath,
            cancellationToken)).ConfigureAwait(true);
    }

    private async void OpenRecentProjectClick(object? sender, RoutedEventArgs e)
    {
        if (_recentProjectComboBox.SelectedItem is not string projectPath)
        {
            _statusText.Text = "Select a recent project first.";
            return;
        }

        if (!File.Exists(projectPath))
        {
            _recentProjects.Remove(projectPath);
            RefreshRecentItemControls();
            await PersistRecentItemsBestEffortAsync(CancellationToken.None).ConfigureAwait(true);
            _statusText.Text = "The selected recent project no longer exists and was removed from the list.";
            return;
        }

        await RunLoadingProjectAsync(cancellationToken => LoadProjectFromPathAsync(
            projectPath,
            cancellationToken)).ConfigureAwait(true);
    }

    private async Task LoadProjectFromPathAsync(
        string projectPath,
        CancellationToken cancellationToken)
    {
        await using FileStream projectStream = File.OpenRead(projectPath);
        FlowPainterProject project = await FlowPainterProjectSerializer.DeserializeAsync(
            projectStream,
            cancellationToken).ConfigureAwait(true);
        string sourcePath = ProjectPathResolver.ResolveSourcePath(projectPath, project.SourcePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"The project source image was not found: {sourcePath}",
                sourcePath);
        }

        _previewQualityComboBox.SelectedItem = project.Preview.Quality;
        ApplyFinalRenderSettings(project.FinalRender);
        ApplyPreset(new FlowPainterPreset(project.Name, project.Settings), project.Seed);
        _projectNameTextBox.Text = project.Name;

        Progress<ImageOperationProgress> progress = CreateImageProgress();
        await using FileStream sourceStream = File.OpenRead(sourcePath);
        SkiaImage? loaded = null;
        SkiaImage? proxy = null;
        SkiaImage? overlay = null;
        bool adopted = false;

        try
        {
            loaded = await _imageLoader.LoadAsync(
                sourceStream,
                IoPath.GetFileName(sourcePath),
                progress,
                cancellationToken).ConfigureAwait(true);
            int previewMaximumDimension = project.Preview.MaximumDimension;
            proxy = await _proxyGenerator.CreateProxyAsync(
                loaded,
                previewMaximumDimension,
                previewMaximumDimension,
                progress,
                cancellationToken).ConfigureAwait(true);
            AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                proxy,
                project.Settings.DetailAnalysis,
                project.Settings.SemanticAnalysis,
                cancellationToken).ConfigureAwait(true);
            DetailMap composedMap = DetailMapComposer.ApplyRegions(
                maps.Automatic,
                project.DetailRegions,
                cancellationToken);
            overlay = await _detailOverlayRenderer.RenderAsync(
                proxy,
                composedMap,
                cancellationToken: cancellationToken).ConfigureAwait(true);

            ReplaceSource(
                loaded,
                proxy,
                maps.Automatic,
                composedMap,
                maps.Semantic,
                project.Settings.DetailAnalysis,
                project.Settings.SemanticAnalysis,
                overlay);
            adopted = true;

            _currentSourcePath = sourcePath;
            _currentProjectPath = projectPath;
            FlowPainterProject resolvedProject = new(
                project.Name,
                sourcePath,
                project.Seed,
                project.Settings,
                project.Preview,
                project.DetailRegions,
                project.FinalRender);
            _workspace.LoadProject(resolvedProject, projectPath);
            _recentProjects.Add(projectPath);
            await PersistRecentItemsBestEffortAsync(cancellationToken).ConfigureAwait(true);
            RefreshRecentItemControls();
            _saveProjectButton.IsEnabled = true;
            _sourceInfoText.Text = $"{loaded.Size.Width:N0} × {loaded.Size.Height:N0} → {proxy.Size.Width:N0} × {proxy.Size.Height:N0}";
            UpdateFinalOutputEstimate();
            _statusText.Text = $"Loaded project '{project.Name}' with {project.DetailRegions.Count:N0} manual regions.";
            UpdateSourcePreviewSelection();
            RefreshRegionVisuals();
        }
        finally
        {
            overlay?.Dispose();
            if (!adopted)
            {
                proxy?.Dispose();
                loaded?.Dispose();
            }
        }
    }

    private async void SaveProjectClick(object? sender, RoutedEventArgs e)
    {
        string? currentSourcePath = _currentSourcePath;
        if (string.IsNullOrWhiteSpace(currentSourcePath))
        {
            _statusText.Text = "Open an image before saving a project.";
            return;
        }

        FlowPainterProject project;
        try
        {
            string name = string.IsNullOrWhiteSpace(_projectNameTextBox.Text)
                ? IoPath.GetFileNameWithoutExtension(currentSourcePath) ?? "FlowPainter project"
                : _projectNameTextBox.Text.Trim();
            _workspace.SetSeed(ParseUnsignedInteger(_seedTextBox, "Seed"));
            _workspace.SetSettings(ReadSettingsFromControls());
            _workspace.SetPreview(ReadPreviewSettings());
            _workspace.SetFinalRender(ReadFinalRenderSettings());
            project = _workspace.CreateProject(name);
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save FlowPainter project",
                SuggestedFileName = string.IsNullOrWhiteSpace(_currentProjectPath)
                    ? "flowpainter.flowpainter.json"
                    : IoPath.GetFileName(_currentProjectPath),
                DefaultExtension = "json",
                FileTypeChoices =
                [
                    new FilePickerFileType("FlowPainter project")
                    {
                        Patterns = ["*.flowpainter.json", "*.json"]
                    }
                ]
            }).ConfigureAwait(true);

        if (file is null)
        {
            return;
        }

        await RunSavingProjectAsync(async cancellationToken =>
        {
            string projectPath = file.Path.LocalPath;
            string sourceReference = ProjectPathResolver.CreateSourceReference(
                projectPath,
                currentSourcePath);
            FlowPainterProject persistedProject = new(
                project.Name,
                sourceReference,
                project.Seed,
                project.Settings,
                project.Preview,
                project.DetailRegions,
                project.FinalRender);
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await FlowPainterProjectSerializer.SerializeAsync(
                persistedProject,
                output,
                cancellationToken).ConfigureAwait(true);
            _currentProjectPath = projectPath;
            _workspace.MarkSaved(projectPath, project.Name);
            _recentProjects.Add(projectPath);
            await PersistRecentItemsBestEffortAsync(cancellationToken).ConfigureAwait(true);
            RefreshRecentItemControls();
            _statusText.Text = $"Saved project '{project.Name}'.";
        }).ConfigureAwait(true);
    }

    private async void RebuildPreviewClick(object? sender, RoutedEventArgs e)
    {
        SkiaImage? source = _sourceImage;
        if (source is null)
        {
            _statusText.Text = "Open an image before rebuilding the preview.";
            return;
        }

        DetailAnalysisSettings detailSettings;
        PreviewSettings previewSettings;
        try
        {
            detailSettings = ReadDetailAnalysisSettings();
            previewSettings = ReadPreviewSettings();
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        await RunRebuildingPreviewAsync(async cancellationToken =>
        {
            Progress<ImageOperationProgress> progress = CreateImageProgress();
            SkiaImage? proxy = null;
            SkiaImage? overlay = null;
            bool adopted = false;
            try
            {
                proxy = await _proxyGenerator.CreateProxyAsync(
                    source,
                    previewSettings.MaximumDimension,
                    previewSettings.MaximumDimension,
                    progress,
                    cancellationToken).ConfigureAwait(true);
                SemanticAnalysisSettings semanticSettings = ReadSemanticAnalysisSettings();
                AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                    proxy,
                    detailSettings,
                    semanticSettings,
                    cancellationToken).ConfigureAwait(true);
                DetailMap composedMap = DetailMapComposer.ApplyRegions(
                    maps.Automatic,
                    _workspace.Regions.Regions,
                    cancellationToken);
                overlay = await _detailOverlayRenderer.RenderAsync(
                    proxy,
                    composedMap,
                    cancellationToken: cancellationToken).ConfigureAwait(true);
                ReplaceProxy(
                    proxy,
                    maps.Automatic,
                    composedMap,
                    maps.Semantic,
                    detailSettings,
                    semanticSettings,
                    overlay);
                adopted = true;
                _statusText.Text = $"Preview rebuilt at {previewSettings.MaximumDimension:N0}px maximum dimension.";
            }
            finally
            {
                overlay?.Dispose();
                if (!adopted)
                {
                    proxy?.Dispose();
                }
            }
        }).ConfigureAwait(true);
    }

    private async void OpenImageClick(object? sender, RoutedEventArgs e)
    {
        FlowPainterSettings settings;
        PreviewSettings previewSettings;
        FinalRenderSettings finalRenderSettings;
        ulong seed;
        try
        {
            settings = ReadSettingsFromControls();
            previewSettings = ReadPreviewSettings();
            finalRenderSettings = ReadFinalRenderSettings();
            seed = ParseUnsignedInteger(_seedTextBox, "Seed");
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        DetailAnalysisSettings detailSettings = settings.DetailAnalysis;

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open source image",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Raster images")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"]
                    }
                ]
            }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return;
        }

        await RunLoadingImageAsync(async cancellationToken =>
        {
            Progress<ImageOperationProgress> progress = CreateImageProgress();
            await using Stream stream = await files[0].OpenReadAsync().ConfigureAwait(true);
            SkiaImage? loaded = null;
            SkiaImage? proxy = null;
            SkiaImage? overlay = null;
            bool adopted = false;

            try
            {
                loaded = await _imageLoader.LoadAsync(
                    stream,
                    files[0].Name,
                    progress,
                    cancellationToken).ConfigureAwait(true);
                proxy = await _proxyGenerator.CreateProxyAsync(
                    loaded,
                    previewSettings.MaximumDimension,
                    previewSettings.MaximumDimension,
                    progress,
                    cancellationToken).ConfigureAwait(true);
                AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                    proxy,
                    detailSettings,
                    settings.SemanticAnalysis,
                    cancellationToken).ConfigureAwait(true);
                overlay = await _detailOverlayRenderer.RenderAsync(
                    proxy,
                    maps.Automatic,
                    cancellationToken: cancellationToken).ConfigureAwait(true);

                ReplaceSource(
                    loaded,
                    proxy,
                    maps.Automatic,
                    maps.Automatic,
                    maps.Semantic,
                    detailSettings,
                    settings.SemanticAnalysis,
                    overlay);
                adopted = true;

                string sourcePath = files[0].Path.LocalPath;
                _currentSourcePath = sourcePath;
                _currentProjectPath = null;
                _workspace.SetSource(sourcePath);
                _workspace.SetSeed(seed);
                _workspace.SetSettings(settings);
                _workspace.SetPreview(previewSettings);
                _workspace.SetFinalRender(finalRenderSettings);
                _projectNameTextBox.Text = IoPath.GetFileNameWithoutExtension(files[0].Name);
                _saveProjectButton.IsEnabled = true;
                _sourceInfoText.Text = $"{loaded.Size.Width:N0} × {loaded.Size.Height:N0} → {proxy.Size.Width:N0} × {proxy.Size.Height:N0}";
                UpdateFinalOutputEstimate();
                _statusText.Text = "Image loaded and structural/semantic importance analyzed. Promote a detected region or drag on the image to refine focus.";
            }
            finally
            {
                overlay?.Dispose();
                if (!adopted)
                {
                    proxy?.Dispose();
                    loaded?.Dispose();
                }
            }
        }).ConfigureAwait(true);
    }

    private async void RenderPreviewClick(object? sender, RoutedEventArgs e)
    {
        SkiaImage? proxyImage = _proxyImage;
        if (proxyImage is null)
        {
            return;
        }

        FlowPainterSettings settings;
        ulong seed;
        try
        {
            settings = ReadSettingsFromControls();
            seed = ParseUnsignedInteger(_seedTextBox, "Seed");
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        await RunRenderingPreviewAsync(async cancellationToken =>
        {
            DetailMap detailMap = await EnsureDetailMapAsync(
                proxyImage,
                settings.DetailAnalysis,
                settings.SemanticAnalysis,
                cancellationToken).ConfigureAwait(true);
            StrokeDensityMap densityMap = StrokeDensityMap.CreateUniform(
                proxyImage.Size,
                settings.UniformDensity);
            Progress<StrokePlanningProgress> planningProgress = new(value =>
            {
                string message = value.Stage == StrokePlanningStage.PlanningStrokes
                    ? $"Planning detail-aware strokes {value.CompletedStrokes:N0} / {value.TotalStrokes:N0}"
                    : $"Planning: {value.Stage}";
                ReportOperationProgress(value.Fraction, message);
            });

            StrokePlan plan = await Task.Run(
                () => _planner.CreatePlan(
                    proxyImage,
                    densityMap,
                    detailMap,
                    seed,
                    settings,
                    planningProgress,
                    cancellationToken),
                cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();

            Progress<StrokeRenderProgress> renderProgress = new(value =>
            {
                string message = value.Stage == StrokeRenderStage.DrawingStrokes
                    ? $"Rendering strokes {value.CompletedStrokes:N0} / {value.TotalStrokes:N0}"
                    : $"Rendering: {value.Stage}";
                ReportOperationProgress(value.Fraction, message);
            });

            SkiaImage rendered = await _renderer.RenderAsync(
                plan,
                proxyImage.Size,
                proxyImage,
                renderProgress,
                settings.Brush,
                cancellationToken).ConfigureAwait(true);
            bool adopted = false;

            try
            {
                ReplaceRendered(rendered, plan, settings.Brush);
                adopted = true;

                _resultInfoText.Text = $"{rendered.Size.Width:N0} × {rendered.Size.Height:N0} · {plan.Strokes.Count:N0} strokes · {settings.Brush.Kind} · {_workspace.Regions.Count:N0} regions";
                _statusText.Text = $"Preview rendered with the {settings.Brush.Kind} brush and detail guidance.";
            }
            finally
            {
                if (!adopted)
                {
                    rendered.Dispose();
                }
            }
        }).ConfigureAwait(true);
    }

    private void FinalOutputSettingsChanged(object? sender, SelectionChangedEventArgs e)
    {
        _jpegQualityTextBox.IsEnabled = _finalFormatComboBox.SelectedItem is RasterImageFormat.Jpeg;
        UpdateFinalOutputEstimate();
    }

    private void UpdateFinalEstimateClick(object? sender, RoutedEventArgs e)
    {
        UpdateFinalOutputEstimate(showValidationMessage: true);
    }

    private async void ExportFinalClick(object? sender, RoutedEventArgs e)
    {
        SkiaImage? sourceImage = _sourceImage;
        StrokePlan? plan = _previewStrokePlan;
        BrushSettings? brush = _previewBrushSettings;
        if (sourceImage is null || plan is null || brush is null)
        {
            _statusText.Text = "Render a preview before exporting the final image.";
            return;
        }

        FinalRenderSettings finalSettings;
        try
        {
            finalSettings = ReadFinalRenderSettings();
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        ImageSize outputSize = finalSettings.GetOutputSize(sourceImage.Size);
        string extension = finalSettings.DefaultFileExtension;
        string suggestedBaseName = string.IsNullOrWhiteSpace(_projectNameTextBox.Text)
            ? "flowpainter-final"
            : SanitizeFileName(_projectNameTextBox.Text.Trim());
        FilePickerFileType fileType = finalSettings.Format == RasterImageFormat.Png
            ? new FilePickerFileType("PNG image") { Patterns = ["*.png"] }
            : new FilePickerFileType("JPEG image") { Patterns = ["*.jpg", "*.jpeg"] };
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export final FlowPainter image",
                SuggestedFileName = $"{suggestedBaseName}.{extension}",
                DefaultExtension = extension,
                FileTypeChoices = [fileType]
            }).ConfigureAwait(true);

        if (file is null)
        {
            return;
        }

        await RunExportingImageAsync(async cancellationToken =>
        {
            _workspace.SetFinalRender(finalSettings);
            Progress<StrokeRenderProgress> renderProgress = new(value =>
            {
                string message = value.Stage == StrokeRenderStage.DrawingStrokes
                    ? $"Final rendering {value.CompletedStrokes:N0} / {value.TotalStrokes:N0} strokes"
                    : $"Final rendering: {value.Stage}";
                ReportOperationProgress(value.Fraction * 0.88d, message);
            });
            using SkiaImage finalImage = await _renderer.RenderAsync(
                plan,
                outputSize,
                plan.BackgroundMode == StrokePlanBackgroundMode.SourceImage ? sourceImage : null,
                renderProgress,
                brush,
                cancellationToken).ConfigureAwait(true);

            Progress<ImageOperationProgress> encodeProgress = new(value =>
                ReportOperationProgress(
                    0.88d + (value.Fraction * 0.12d),
                    value.Message));
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await _imageEncoder.EncodeAsync(
                finalImage,
                output,
                finalSettings.Format,
                finalSettings.JpegQuality,
                encodeProgress,
                cancellationToken).ConfigureAwait(true);
            _statusText.Text = $"Exported {file.Name} at {outputSize.Width:N0} × {outputSize.Height:N0} with the {brush.Kind} brush.";
        }).ConfigureAwait(true);
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        _operationCancellation?.Cancel();
    }

    private void NewSeedClick(object? sender, RoutedEventArgs e)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        _seedTextBox.Text = BinaryPrimitives.ReadUInt64LittleEndian(bytes).ToString(CultureInfo.InvariantCulture);
        _presetComboBox.SelectedIndex = -1;
        _statusText.Text = "A new random seed was generated.";
    }

    private async void ReanalyzeDetailClick(object? sender, RoutedEventArgs e)
    {
        SkiaImage? proxy = _proxyImage;
        if (proxy is null)
        {
            _statusText.Text = "Open an image before analyzing detail.";
            return;
        }

        DetailAnalysisSettings detailSettings;
        SemanticAnalysisSettings semanticSettings;
        try
        {
            detailSettings = ReadDetailAnalysisSettings();
            semanticSettings = ReadSemanticAnalysisSettings();
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                proxy,
                detailSettings,
                semanticSettings,
                cancellationToken).ConfigureAwait(true);
            await ReplaceAnalyzedDetailMapsAsync(
                maps,
                detailSettings,
                semanticSettings,
                cancellationToken).ConfigureAwait(true);
            _statusText.Text = semanticSettings.Enabled
                ? $"Structural and semantic maps updated. {maps.Semantic.Regions.Count:N0} semantic regions found."
                : "Structural detail map updated; semantic analysis is disabled.";
        }).ConfigureAwait(true);
    }

    private async void RemoveLastRegionClick(object? sender, RoutedEventArgs e)
    {
        if (_workspace.Regions.Count == 0 || _automaticDetailMap is null)
        {
            return;
        }

        _workspace.RemoveLastRegion();
        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            _statusText.Text = "Last manual detail region removed.";
        }).ConfigureAwait(true);
    }

    private async void ClearRegionsClick(object? sender, RoutedEventArgs e)
    {
        if (_workspace.Regions.Count == 0 || _automaticDetailMap is null)
        {
            return;
        }

        _workspace.ClearRegions();
        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            _statusText.Text = "All manual detail regions cleared.";
        }).ConfigureAwait(true);
    }

    private void DetailOverlayVisibilityClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        UpdateSourcePreviewSelection();
        RefreshRegionVisuals();
    }


    private async void SemanticOverlayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        SemanticOverlayMode mode = ReadSelectedSemanticOverlayMode();
        if (mode == SemanticOverlayMode.CombinedDetail
            || _showDetailOverlayCheckBox.IsChecked != true)
        {
            UpdateSourcePreviewSelection();
            return;
        }

        SkiaImage? proxy = _proxyImage;
        SemanticAnalysisResult? semantic = _semanticAnalysisResult;
        if (proxy is null || semantic is null || _operationCancellation is not null)
        {
            UpdateSourcePreviewSelection();
            return;
        }

        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            DetailMap map = SelectSemanticOverlayMap(semantic, mode);
            using SkiaImage overlay = await _detailOverlayRenderer.RenderAsync(
                proxy,
                map,
                cancellationToken: cancellationToken).ConfigureAwait(true);
            ReplaceSemanticOverlayPreview(overlay);
            _statusText.Text = $"Showing {mode} overlay from {semantic.ProviderId}.";
        }).ConfigureAwait(true);
    }

    private async void PromoteSemanticFocusClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await PromoteSelectedSemanticRegionAsync(0.85d, "focus").ConfigureAwait(true);
    }

    private async void PromoteSemanticCriticalClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await PromoteSelectedSemanticRegionAsync(1d, "critical detail").ConfigureAwait(true);
    }

    private async Task PromoteSelectedSemanticRegionAsync(
        double strength,
        string roleLabel)
    {
        SemanticRegion? region = GetSelectedSemanticRegion();
        if (region is null || _automaticDetailMap is null)
        {
            _statusText.Text = "Select an analyzed semantic region first.";
            return;
        }

        string label = $"{region.Label ?? region.Id} · {roleLabel}";
        DetailRegion manual = _workspace.AddRegion(
            region.Bounds,
            strength,
            DetailRegionIntent.IncreaseDetail,
            label);
        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            RefreshRegionVisuals(manual.Id);
            _statusText.Text = $"Promoted '{region.Label ?? region.Id}' to manual {roleLabel}.";
        }).ConfigureAwait(true);
    }

    private void ViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Grid viewportHost || _proxyImage is null)
        {
            return;
        }

        PointerPoint pointerPoint = e.GetCurrentPoint(viewportHost);
        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            _panViewportHost = viewportHost;
            _panLastPosition = e.GetPosition(viewportHost);
            e.Pointer.Capture(viewportHost);
            e.Handled = true;
            return;
        }

        if (!ReferenceEquals(viewportHost, _sourceViewportHost)
            || !pointerPoint.Properties.IsLeftButtonPressed
            || _composedDetailMap is null
            || _operationCancellation is not null)
        {
            return;
        }

        UniformImageViewport? viewport = CreateViewport(viewportHost);
        if (viewport is null)
        {
            return;
        }

        Point position = e.GetPosition(viewportHost);
        if (!_imageViewportState.TryMapToNormalized(
            viewport,
            new ViewportPoint(position.X, position.Y),
            out NormalizedPoint normalized))
        {
            return;
        }

        _selectionStart = normalized;
        _selectionCurrent = normalized;
        e.Pointer.Capture(viewportHost);
        e.Handled = true;
        RefreshRegionVisuals();
    }

    private void ViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_panViewportHost is Grid panHost)
        {
            UniformImageViewport? viewport = CreateViewport(panHost);
            if (viewport is null)
            {
                return;
            }

            Point position = e.GetPosition(panHost);
            _imageViewportState.PanBy(
                viewport,
                position.X - _panLastPosition.X,
                position.Y - _panLastPosition.Y);
            _panLastPosition = position;
            ApplySynchronizedViewportTransforms();
            e.Handled = true;
            return;
        }

        if (_selectionStart is null || !ReferenceEquals(sender, _sourceViewportHost))
        {
            return;
        }

        UniformImageViewport? sourceViewport = CreateSourceViewport();
        if (sourceViewport is null)
        {
            return;
        }

        Point sourcePosition = e.GetPosition(_sourceViewportHost);
        _selectionCurrent = _imageViewportState.MapClampedToNormalized(
            sourceViewport,
            new ViewportPoint(sourcePosition.X, sourcePosition.Y));
        e.Handled = true;
        RefreshRegionVisuals();
    }

    private async void ViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_panViewportHost is not null)
        {
            _panViewportHost = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (!ReferenceEquals(sender, _sourceViewportHost) || _selectionStart is null)
        {
            return;
        }

        NormalizedPoint? start = _selectionStart;
        NormalizedPoint? end = _selectionCurrent;
        _selectionStart = null;
        _selectionCurrent = null;
        e.Pointer.Capture(null);
        e.Handled = true;

        if (start is null || end is null)
        {
            RefreshRegionVisuals();
            return;
        }

        double width = Math.Abs(end.Value.X - start.Value.X);
        double height = Math.Abs(end.Value.Y - start.Value.Y);
        if (width < MinimumManualRegionSize || height < MinimumManualRegionSize)
        {
            _statusText.Text = "The selected area is too small.";
            RefreshRegionVisuals();
            return;
        }

        DetailRegionIntent intent;
        double strength;
        try
        {
            intent = ReadSelectedDetailIntent();
            strength = ParseDouble(_regionStrengthTextBox, "Region strength") / 100d;
            NormalizedRect bounds = NormalizedRect.FromCorners(start.Value, end.Value);
            _workspace.AddRegion(
                bounds,
                strength,
                intent);
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            RefreshRegionVisuals();
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            RefreshRegionVisuals();
            return;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            RefreshRegionVisuals();
            return;
        }

        RefreshRegionVisuals();
        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            _statusText.Text = "Manual detail region added. Render the preview to apply it to the strokes.";
        }).ConfigureAwait(true);
    }

    private void ViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not Grid viewportHost || _proxyImage is null || _selectionStart is not null)
        {
            return;
        }

        UniformImageViewport? viewport = CreateViewport(viewportHost);
        if (viewport is null)
        {
            return;
        }

        Point position = e.GetPosition(viewportHost);
        _imageViewportState.ZoomAt(
            viewport,
            new ViewportPoint(position.X, position.Y),
            e.Delta.Y);
        ApplySynchronizedViewportTransforms();
        RefreshRegionVisuals();
        e.Handled = true;
    }

    private void ViewportHostSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ApplySynchronizedViewportTransforms();
        if (ReferenceEquals(sender, _sourceViewportHost))
        {
            RefreshRegionVisuals();
        }
    }

    private void SourceCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        RefreshRegionVisuals();
    }

    private void RegionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressRegionSelectionChange)
        {
            return;
        }

        PopulateRegionEditorControls(GetSelectedRegion());
    }

    private async void ApplyRegionChangesClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null || _automaticDetailMap is null)
        {
            _statusText.Text = "Select a manual region before applying changes.";
            return;
        }

        try
        {
            double left = ParseDouble(_regionLeftTextBox, "Region left") / 100d;
            double top = ParseDouble(_regionTopTextBox, "Region top") / 100d;
            double width = ParseDouble(_regionWidthTextBox, "Region width") / 100d;
            double height = ParseDouble(_regionHeightTextBox, "Region height") / 100d;
            double strength = ParseDouble(_regionStrengthTextBox, "Region strength") / 100d;
            DetailRegionIntent intent = ReadSelectedDetailIntent();
            NormalizedRect bounds = new(
                left,
                top,
                left + width,
                top + height);
            _workspace.UpdateRegion(
                selected.Id,
                bounds,
                strength,
                intent,
                _regionLabelTextBox.Text);
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        await RecomposeRegionsFromEditorAsync("Selected detail region updated.").ConfigureAwait(true);
    }

    private async void MoveRegionEarlierClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null || !_workspace.MoveRegionEarlier(selected.Id))
        {
            return;
        }

        await RecomposeRegionsFromEditorAsync("Selected detail region moved earlier in the composition order.", selected.Id).ConfigureAwait(true);
    }

    private async void MoveRegionLaterClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null || !_workspace.MoveRegionLater(selected.Id))
        {
            return;
        }

        await RecomposeRegionsFromEditorAsync("Selected detail region moved later in the composition order.", selected.Id).ConfigureAwait(true);
    }

    private async void DeleteSelectedRegionClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null || !_workspace.RemoveRegion(selected.Id))
        {
            return;
        }

        await RecomposeRegionsFromEditorAsync("Selected detail region deleted.").ConfigureAwait(true);
    }

    private async Task RecomposeRegionsFromEditorAsync(string completionMessage, string? selectedRegionId = null)
    {
        if (_automaticDetailMap is null)
        {
            RefreshRegionVisuals(selectedRegionId);
            return;
        }

        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            RefreshRegionVisuals(selectedRegionId);
            _statusText.Text = completionMessage;
        }).ConfigureAwait(true);
    }

    private async void LoadPresetClick(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Load FlowPainter preset",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("FlowPainter preset")
                    {
                        Patterns = ["*.flowpreset.json", "*.json"]
                    }
                ]
            }).ConfigureAwait(true);

        if (files.Count == 0)
        {
            return;
        }

        string presetPath = files[0].Path.LocalPath;
        await RunLoadingPresetAsync(cancellationToken => LoadPresetFromPathAsync(
            presetPath,
            cancellationToken)).ConfigureAwait(true);
    }

    private async void LoadRecentPresetClick(object? sender, RoutedEventArgs e)
    {
        if (_recentPresetComboBox.SelectedItem is not string presetPath)
        {
            _statusText.Text = "Select a recent preset first.";
            return;
        }

        if (!File.Exists(presetPath))
        {
            _recentPresets.Remove(presetPath);
            RefreshRecentItemControls();
            await PersistRecentItemsBestEffortAsync(CancellationToken.None).ConfigureAwait(true);
            _statusText.Text = "The selected recent preset no longer exists and was removed from the list.";
            return;
        }

        await RunLoadingPresetAsync(cancellationToken => LoadPresetFromPathAsync(
            presetPath,
            cancellationToken)).ConfigureAwait(true);
    }

    private async Task LoadPresetFromPathAsync(
        string presetPath,
        CancellationToken cancellationToken)
    {
        await using FileStream input = File.OpenRead(presetPath);
        FlowPainterPreset preset = await FlowPainterPresetSerializer.DeserializeAsync(
            input,
            cancellationToken).ConfigureAwait(true);
        _presetComboBox.SelectedIndex = -1;
        ApplyPreset(preset, ParseUnsignedInteger(_seedTextBox, "Seed"));
        _recentPresets.Add(presetPath);
        await PersistRecentItemsBestEffortAsync(cancellationToken).ConfigureAwait(true);
        RefreshRecentItemControls();
        _statusText.Text = $"Loaded preset '{preset.Name}'. Reanalyze detail to apply changed analysis parameters.";
    }

    private async void SavePresetClick(object? sender, RoutedEventArgs e)
    {
        FlowPainterPreset preset;
        try
        {
            string name = _presetNameTextBox.Text ?? string.Empty;
            preset = new FlowPainterPreset(name, ReadSettingsFromControls());
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save FlowPainter preset",
                SuggestedFileName = "flowpainter.flowpreset.json",
                DefaultExtension = "json",
                FileTypeChoices =
                [
                    new FilePickerFileType("FlowPainter preset")
                    {
                        Patterns = ["*.flowpreset.json", "*.json"]
                    }
                ]
            }).ConfigureAwait(true);

        if (file is null)
        {
            return;
        }

        await RunSavingPresetAsync(async cancellationToken =>
        {
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await FlowPainterPresetSerializer.SerializeAsync(
                preset,
                output,
                cancellationToken).ConfigureAwait(true);
            string presetPath = file.Path.LocalPath;
            _recentPresets.Add(presetPath);
            await PersistRecentItemsBestEffortAsync(cancellationToken).ConfigureAwait(true);
            RefreshRecentItemControls();
            _statusText.Text = $"Saved preset '{preset.Name}'.";
        }).ConfigureAwait(true);
    }

    private async void SavePngClick(object? sender, RoutedEventArgs e)
    {
        SkiaImage? renderedImage = _renderedImage;
        if (renderedImage is null)
        {
            return;
        }

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save rendered preview",
                SuggestedFileName = "flowpainter-preview.png",
                DefaultExtension = "png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG image")
                    {
                        Patterns = ["*.png"]
                    }
                ]
            }).ConfigureAwait(true);

        if (file is null)
        {
            return;
        }

        await RunExportingImageAsync(async cancellationToken =>
        {
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await _pngEncoder.EncodeAsync(
                renderedImage,
                output,
                CreateImageProgress(),
                cancellationToken).ConfigureAwait(true);
            _statusText.Text = $"Saved {file.Name}";
        }).ConfigureAwait(true);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Recent-item persistence is optional UI state; startup must remain usable if the file is missing or corrupt.")]
    private void LoadRecentItemsAtStartup()
    {
        string recentItemsPath = GetRecentItemsPath();
        if (!File.Exists(recentItemsPath))
        {
            RefreshRecentItemControls();
            return;
        }

        try
        {
            using FileStream input = File.OpenRead(recentItemsPath);
            RecentItemsDocument document = RecentItemsSerializer.DeserializeAsync(input)
                .GetAwaiter()
                .GetResult();
            _recentProjects.Replace(document.Projects);
            _recentPresets.Replace(document.Presets);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException
            or ArgumentException)
        {
            _recentProjects.Clear();
            _recentPresets.Clear();
            _statusText.Text = "Recent items could not be restored; the list was reset.";
        }

        RefreshRecentItemControls();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Recent-item persistence must never fail the primary image, project or preset operation.")]
    private async Task PersistRecentItemsBestEffortAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveRecentItemsAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Recent-item persistence is best-effort and must not replace the primary operation result.
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException
            or ArgumentException)
        {
            _workspace.AddValidation(new WorkspaceValidationMessage(
                "recent-items.save",
                "The recent-items list could not be saved.",
                ValidationSeverity.Warning));
        }
    }

    private async Task SaveRecentItemsAsync(CancellationToken cancellationToken)
    {
        string recentItemsPath = GetRecentItemsPath();
        string? directory = IoPath.GetDirectoryName(recentItemsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        RecentItemsDocument document = new(
            RecentItemsDocument.CurrentSchemaVersion,
            _recentProjects.Paths.ToArray(),
            _recentPresets.Paths.ToArray());
        await using FileStream output = new(
            recentItemsPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await RecentItemsSerializer.SerializeAsync(
            document,
            output,
            cancellationToken).ConfigureAwait(true);
    }

    private void RefreshRecentItemControls()
    {
        string? selectedProject = _recentProjectComboBox.SelectedItem as string;
        string? selectedPreset = _recentPresetComboBox.SelectedItem as string;
        string[] projects = _recentProjects.Paths.ToArray();
        string[] presets = _recentPresets.Paths.ToArray();

        _recentProjectComboBox.ItemsSource = projects;
        _recentPresetComboBox.ItemsSource = presets;
        _recentProjectComboBox.SelectedIndex = FindPathIndex(projects, selectedProject);
        _recentPresetComboBox.SelectedIndex = FindPathIndex(presets, selectedPreset);
    }

    private static int FindPathIndex(string[] paths, string? selectedPath)
    {
        if (paths.Length == 0)
        {
            return -1;
        }

        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            for (int index = 0; index < paths.Length; index++)
            {
                if (string.Equals(paths[index], selectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }
        }

        return 0;
    }

    private static string GetRecentItemsPath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return IoPath.Combine(localApplicationData, "FlowPainter", "recent-items.json");
    }

    private void ApplySelectedBuiltInPreset()
    {
        if (_presetComboBox.SelectedItem is FlowPainterPreset preset)
        {
            ulong seed = TryParseUnsignedInteger(_seedTextBox.Text, out ulong currentSeed)
                ? currentSeed
                : InitialSeed;
            ApplyPreset(preset, seed);
            _statusText.Text = $"Applied preset '{preset.Name}'. Reanalyze detail after changing analysis settings.";
        }
    }

    private void ApplyPreset(FlowPainterPreset preset, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(preset);
        FlowPainterSettings settings = preset.Settings;
        _presetNameTextBox.Text = preset.Name;
        _seedTextBox.Text = seed.ToString(CultureInfo.InvariantCulture);
        _flowFieldComboBox.SelectedItem = settings.Field.Kind;
        _backgroundComboBox.SelectedItem = settings.BackgroundMode;
        _strokeCountTextBox.Text = settings.StrokeCount.ToString(CultureInfo.CurrentCulture);
        _segmentCountTextBox.Text = settings.SegmentCount.ToString(CultureInfo.CurrentCulture);
        _fieldScaleTextBox.Text = FormatDouble(settings.Field.Scale);
        _octavesTextBox.Text = settings.Field.Octaves.ToString(CultureInfo.CurrentCulture);
        _persistenceTextBox.Text = FormatDouble(settings.Field.Persistence);
        _lacunarityTextBox.Text = FormatDouble(settings.Field.Lacunarity);
        _angleOffsetTextBox.Text = FormatDouble(RadiansToDegrees(settings.Field.AngleOffsetRadians));
        _densityTextBox.Text = FormatDouble(settings.UniformDensity);
        _lengthScaleTextBox.Text = FormatDouble(settings.LengthScale);
        _maximumCurveTextBox.Text = FormatDouble(RadiansToDegrees(settings.MaximumCurveRadians));
        _minimumWidthTextBox.Text = FormatDouble(settings.MinimumStrokeWidthPixels);
        _maximumWidthTextBox.Text = FormatDouble(settings.MaximumStrokeWidthPixels);
        _opacityTextBox.Text = FormatDouble(settings.StrokeOpacity * 100d);
        _brushKindComboBox.SelectedItem = settings.Brush.Kind;
        _brushHardnessTextBox.Text = FormatDouble(settings.Brush.Hardness * 100d);
        _brushSizeJitterTextBox.Text = FormatDouble(settings.Brush.SizeJitter * 100d);
        _brushOpacityJitterTextBox.Text = FormatDouble(settings.Brush.OpacityJitter * 100d);
        _brushBristleCountTextBox.Text = settings.Brush.BristleCount.ToString(CultureInfo.CurrentCulture);
        _brushBristleSpreadTextBox.Text = FormatDouble(settings.Brush.BristleSpread * 100d);
        _baseDetailTextBox.Text = FormatDouble(settings.DetailAnalysis.BaseDetail * 100d);
        _edgeWeightTextBox.Text = FormatDouble(settings.DetailAnalysis.EdgeWeight);
        _contrastWeightTextBox.Text = FormatDouble(settings.DetailAnalysis.ContrastWeight);
        _smoothingRadiusTextBox.Text = settings.DetailAnalysis.SmoothingRadius.ToString(CultureInfo.CurrentCulture);
        _placementBiasTextBox.Text = FormatDouble(settings.DetailInfluence.PlacementBias);
        _detailedLengthTextBox.Text = FormatDouble(settings.DetailInfluence.DetailedLengthMultiplier * 100d);
        _backgroundLengthTextBox.Text = FormatDouble(settings.DetailInfluence.BackgroundLengthMultiplier * 100d);
        _detailedWidthTextBox.Text = FormatDouble(settings.DetailInfluence.DetailedWidthMultiplier * 100d);
        _backgroundWidthTextBox.Text = FormatDouble(settings.DetailInfluence.BackgroundWidthMultiplier * 100d);
        _enableSemanticAnalysisCheckBox.IsChecked = settings.SemanticAnalysis.Enabled;
        _semanticInfluenceTextBox.Text = FormatDouble(settings.SemanticAnalysis.OverallInfluence * 100d);
        _semanticSaliencyWeightTextBox.Text = FormatDouble(settings.SemanticAnalysis.SaliencyWeight);
        _semanticSubjectWeightTextBox.Text = FormatDouble(settings.SemanticAnalysis.SubjectWeight);
        _semanticSilhouetteWeightTextBox.Text = FormatDouble(settings.SemanticAnalysis.SilhouetteWeight);
        _semanticFocalWeightTextBox.Text = FormatDouble(settings.SemanticAnalysis.FocalWeight);
        _semanticThresholdTextBox.Text = FormatDouble(settings.SemanticAnalysis.SubjectThreshold * 100d);
        _semanticMinimumAreaTextBox.Text = FormatDouble(settings.SemanticAnalysis.MinimumSubjectAreaRatio * 100d);
        _semanticMaximumSubjectsTextBox.Text = settings.SemanticAnalysis.MaximumSubjects.ToString(CultureInfo.CurrentCulture);
        _semanticCenterBiasTextBox.Text = FormatDouble(settings.SemanticAnalysis.CenterBias);
        _semanticSmoothingRadiusTextBox.Text = settings.SemanticAnalysis.SmoothingRadius.ToString(CultureInfo.CurrentCulture);
        _semanticBoundaryRadiusTextBox.Text = settings.SemanticAnalysis.BoundaryRadius.ToString(CultureInfo.CurrentCulture);
    }

    private void ApplyFinalRenderSettings(FinalRenderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _finalMaximumDimensionTextBox.Text = settings.MaximumDimension.ToString(CultureInfo.CurrentCulture);
        _finalFormatComboBox.SelectedItem = settings.Format;
        _jpegQualityTextBox.Text = settings.JpegQuality.ToString(CultureInfo.CurrentCulture);
        _jpegQualityTextBox.IsEnabled = settings.Format == RasterImageFormat.Jpeg;
        UpdateFinalOutputEstimate();
    }

    private FinalRenderSettings ReadFinalRenderSettings()
    {
        RasterImageFormat format = _finalFormatComboBox.SelectedItem is RasterImageFormat selectedFormat
            ? selectedFormat
            : throw new InvalidOperationException("Select a final image format.");
        return new FinalRenderSettings(
            ParseInteger(_finalMaximumDimensionTextBox, "Final maximum dimension"),
            format,
            ParseInteger(_jpegQualityTextBox, "JPEG quality"));
    }

    private void UpdateFinalOutputEstimate(bool showValidationMessage = false)
    {
        SkiaImage? source = _sourceImage;
        if (source is null)
        {
            _finalOutputInfoText.Text = "Open an image to estimate final output.";
            _finalMemoryInfoText.Text = "Memory estimate unavailable.";
            return;
        }

        try
        {
            FinalRenderSettings settings = ReadFinalRenderSettings();
            ImageSize outputSize = settings.GetOutputSize(source.Size);
            ImageSize proxySize = _proxyImage?.Size ?? source.Size.FitWithin(PreviewSettings.StandardMaximumDimension, PreviewSettings.StandardMaximumDimension);
            ImageSize previewSize = _renderedImage?.Size ?? proxySize;
            FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
                source.Size,
                proxySize,
                previewSize,
                outputSize,
                includeDetailOverlay: _detailOverlayPreviewBitmap is not null);
            _finalOutputInfoText.Text = $"{outputSize.Width:N0} × {outputSize.Height:N0} · {settings.Format}";
            _finalMemoryInfoText.Text = $"Known peak RGBA buffers: {estimate.KnownPeakMebibytes:N0} MiB · {estimate.Risk} risk";
            if (showValidationMessage)
            {
                _statusText.Text = estimate.Risk == FinalRenderMemoryRisk.High
                    ? "High memory estimate. Close other applications before final rendering."
                    : $"Final output estimate updated: {estimate.KnownPeakMebibytes:N0} MiB known RGBA buffers.";
            }
        }
        catch (FormatException exception)
        {
            _finalOutputInfoText.Text = "Final output settings are invalid.";
            _finalMemoryInfoText.Text = exception.Message;
            if (showValidationMessage)
            {
                _statusText.Text = exception.Message;
            }
        }
        catch (ArgumentException exception)
        {
            _finalOutputInfoText.Text = "Final output settings are invalid.";
            _finalMemoryInfoText.Text = exception.Message;
            if (showValidationMessage)
            {
                _statusText.Text = exception.Message;
            }
        }
        catch (InvalidOperationException exception)
        {
            _finalOutputInfoText.Text = "Final output settings are invalid.";
            _finalMemoryInfoText.Text = exception.Message;
            if (showValidationMessage)
            {
                _statusText.Text = exception.Message;
            }
        }
    }

    private static string SanitizeFileName(string value)
    {
        HashSet<char> invalidCharacters = [.. IoPath.GetInvalidFileNameChars()];
        string sanitized = string.Concat(value.Select(character => invalidCharacters.Contains(character) ? '_' : character)).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "flowpainter-final" : sanitized;
    }

    private PreviewSettings ReadPreviewSettings()
    {
        return _previewQualityComboBox.SelectedItem is PreviewQuality quality
            ? new PreviewSettings(quality)
            : throw new InvalidOperationException("Select a preview quality.");
    }

    private FlowPainterSettings ReadSettingsFromControls()
    {
        FlowFieldKind fieldKind = _flowFieldComboBox.SelectedItem is FlowFieldKind selectedField
            ? selectedField
            : throw new InvalidOperationException("Select a flow-field kind.");
        StrokePlanBackgroundMode backgroundMode = _backgroundComboBox.SelectedItem is StrokePlanBackgroundMode selectedBackground
            ? selectedBackground
            : throw new InvalidOperationException("Select a background mode.");

        FlowFieldSettings field = new(
            fieldKind,
            ParseDouble(_fieldScaleTextBox, "Field scale"),
            ParseInteger(_octavesTextBox, "Octaves"),
            ParseDouble(_persistenceTextBox, "Persistence"),
            ParseDouble(_lacunarityTextBox, "Lacunarity"),
            DegreesToRadians(ParseDouble(_angleOffsetTextBox, "Field rotation")));
        BrushKind brushKind = _brushKindComboBox.SelectedItem is BrushKind selectedBrush
            ? selectedBrush
            : throw new InvalidOperationException("Select a brush type.");
        BrushSettings brush = new(
            brushKind,
            ParseDouble(_brushHardnessTextBox, "Brush hardness") / 100d,
            ParseDouble(_brushSizeJitterTextBox, "Brush size jitter") / 100d,
            ParseDouble(_brushOpacityJitterTextBox, "Brush opacity jitter") / 100d,
            ParseInteger(_brushBristleCountTextBox, "Bristle count"),
            ParseDouble(_brushBristleSpreadTextBox, "Bristle spread") / 100d);

        return new FlowPainterSettings(
            field,
            ParseInteger(_strokeCountTextBox, "Strokes"),
            ParseInteger(_segmentCountTextBox, "Segments"),
            ReadPreviewSettings().MaximumDimension,
            ParseDouble(_densityTextBox, "Uniform density"),
            ParseDouble(_lengthScaleTextBox, "Length scale"),
            DegreesToRadians(ParseDouble(_maximumCurveTextBox, "Maximum curve")),
            ParseDouble(_minimumWidthTextBox, "Minimum width"),
            ParseDouble(_maximumWidthTextBox, "Maximum width"),
            ParseDouble(_opacityTextBox, "Opacity") / 100d,
            backgroundMode,
            ReadDetailAnalysisSettings(),
            ReadDetailInfluenceSettings(),
            brush,
            ReadSemanticAnalysisSettings());
    }

    private DetailAnalysisSettings ReadDetailAnalysisSettings()
    {
        return new DetailAnalysisSettings(
            ParseDouble(_baseDetailTextBox, "Base detail") / 100d,
            ParseDouble(_edgeWeightTextBox, "Edge weight"),
            ParseDouble(_contrastWeightTextBox, "Contrast weight"),
            ParseInteger(_smoothingRadiusTextBox, "Smoothing radius"));
    }

    private DetailInfluenceSettings ReadDetailInfluenceSettings()
    {
        return new DetailInfluenceSettings(
            ParseDouble(_placementBiasTextBox, "Placement bias"),
            ParseDouble(_detailedLengthTextBox, "Detailed length") / 100d,
            ParseDouble(_backgroundLengthTextBox, "Background length") / 100d,
            ParseDouble(_detailedWidthTextBox, "Detailed width") / 100d,
            ParseDouble(_backgroundWidthTextBox, "Background width") / 100d);
    }

    private SemanticAnalysisSettings ReadSemanticAnalysisSettings()
    {
        return new SemanticAnalysisSettings(
            _enableSemanticAnalysisCheckBox.IsChecked == true,
            ParseDouble(_semanticInfluenceTextBox, "Semantic influence") / 100d,
            ParseDouble(_semanticSaliencyWeightTextBox, "Saliency weight"),
            ParseDouble(_semanticSubjectWeightTextBox, "Subject weight"),
            ParseDouble(_semanticSilhouetteWeightTextBox, "Silhouette weight"),
            ParseDouble(_semanticFocalWeightTextBox, "Focal weight"),
            ParseDouble(_semanticThresholdTextBox, "Subject threshold") / 100d,
            ParseDouble(_semanticMinimumAreaTextBox, "Minimum subject area") / 100d,
            ParseInteger(_semanticMaximumSubjectsTextBox, "Maximum subjects"),
            ParseDouble(_semanticCenterBiasTextBox, "Center bias"),
            ParseInteger(_semanticSmoothingRadiusTextBox, "Semantic smoothing"),
            ParseInteger(_semanticBoundaryRadiusTextBox, "Silhouette radius"));
    }

    private DetailRegionIntent ReadSelectedDetailIntent()
    {
        return _detailIntentComboBox.SelectedItem is DetailRegionIntent intent
            ? intent
            : throw new InvalidOperationException("Select a manual detail-region intent.");
    }

    private async Task<DetailMap> AnalyzeDetailMapAsync(
        SkiaImage proxy,
        DetailAnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        Progress<DetailAnalysisProgress> progress = new(value =>
        {
            string message = value.Stage switch
            {
                DetailAnalysisStage.Preparing => "Preparing structural analysis...",
                DetailAnalysisStage.AnalyzingStructure => $"Analyzing image structure {value.CompletedRows:N0} / {value.TotalRows:N0}",
                DetailAnalysisStage.Smoothing => $"Smoothing structural map {value.CompletedRows:N0} / {value.TotalRows:N0}",
                DetailAnalysisStage.Completed => "Structural analysis completed.",
                _ => "Analyzing structure..."
            };
            ReportOperationProgress(value.Fraction * 0.42d, message);
        });

        return await _detailAnalyzer.AnalyzeAsync(
            proxy,
            settings,
            progress,
            cancellationToken).ConfigureAwait(true);
    }

    private async Task<SemanticAnalysisResult> AnalyzeSemanticImportanceAsync(
        SkiaImage proxy,
        SemanticAnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        Progress<SemanticAnalysisProgress> progress = new(value =>
        {
            string message = value.Stage switch
            {
                SemanticAnalysisStage.Preparing => "Preparing semantic importance analysis...",
                SemanticAnalysisStage.ComputingSaliency => $"Computing saliency {value.CompletedRows:N0} / {value.TotalRows:N0}",
                SemanticAnalysisStage.SegmentingSubjects => "Segmenting generic subjects...",
                SemanticAnalysisStage.BuildingSilhouettes => $"Building subject silhouettes {value.CompletedRows:N0} / {value.TotalRows:N0}",
                SemanticAnalysisStage.CombiningMaps => "Combining semantic maps...",
                SemanticAnalysisStage.Completed => "Semantic importance analysis completed.",
                _ => "Analyzing semantic importance..."
            };
            ReportOperationProgress(0.42d + (value.Fraction * 0.5d), message);
        });

        return await _semanticAnalyzer.AnalyzeAsync(
            proxy,
            settings,
            progress,
            cancellationToken).ConfigureAwait(true);
    }

    private async Task<AutomaticAnalysisMaps> AnalyzeAutomaticMapsAsync(
        SkiaImage proxy,
        DetailAnalysisSettings detailSettings,
        SemanticAnalysisSettings semanticSettings,
        CancellationToken cancellationToken)
    {
        DetailMap structural = await AnalyzeDetailMapAsync(
            proxy,
            detailSettings,
            cancellationToken).ConfigureAwait(true);
        SemanticAnalysisResult semantic = await AnalyzeSemanticImportanceAsync(
            proxy,
            semanticSettings,
            cancellationToken).ConfigureAwait(true);
        DetailMap automatic = SemanticDetailMapComposer.Combine(
            structural,
            semantic,
            semanticSettings,
            cancellationToken);
        ReportOperationProgress(0.94d, "Automatic importance maps combined.");
        return new AutomaticAnalysisMaps(structural, semantic, automatic);
    }

    private async Task<DetailMap> EnsureDetailMapAsync(
        SkiaImage proxy,
        DetailAnalysisSettings detailSettings,
        SemanticAnalysisSettings semanticSettings,
        CancellationToken cancellationToken)
    {
        if (_automaticDetailMap is not null
            && _composedDetailMap is not null
            && DetailAnalysisSettingsEqual(_activeDetailAnalysisSettings, detailSettings)
            && SemanticAnalysisSettingsEqual(_activeSemanticAnalysisSettings, semanticSettings))
        {
            return _composedDetailMap;
        }

        AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
            proxy,
            detailSettings,
            semanticSettings,
            cancellationToken).ConfigureAwait(true);
        await ReplaceAnalyzedDetailMapsAsync(
            maps,
            detailSettings,
            semanticSettings,
            cancellationToken).ConfigureAwait(true);
        return _composedDetailMap
            ?? throw new InvalidOperationException("The composed detail map was not created.");
    }

    private async Task ReplaceAnalyzedDetailMapsAsync(
        AutomaticAnalysisMaps maps,
        DetailAnalysisSettings detailSettings,
        SemanticAnalysisSettings semanticSettings,
        CancellationToken cancellationToken)
    {
        DetailMap composed = DetailMapComposer.ApplyRegions(
            maps.Automatic,
            _workspace.Regions.Regions,
            cancellationToken);
        SkiaImage proxy = _proxyImage
            ?? throw new InvalidOperationException("The analysis proxy is not available.");
        using SkiaImage overlay = await _detailOverlayRenderer.RenderAsync(
            proxy,
            composed,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        ReplaceDetailVisualization(composed, overlay);
        _automaticDetailMap = maps.Automatic;
        _semanticAnalysisResult = maps.Semantic;
        _activeDetailAnalysisSettings = detailSettings;
        _activeSemanticAnalysisSettings = semanticSettings;
        ClearSemanticOverlayPreview();
        RefreshSemanticRegions();
    }

    private async Task RecomposeDetailMapAsync(CancellationToken cancellationToken)
    {
        DetailMap automatic = _automaticDetailMap
            ?? throw new InvalidOperationException("The automatic detail map is not available.");
        DetailMap composed = DetailMapComposer.ApplyRegions(
            automatic,
            _workspace.Regions.Regions,
            cancellationToken);
        SkiaImage proxy = _proxyImage
            ?? throw new InvalidOperationException("The analysis proxy is not available.");
        using SkiaImage overlay = await _detailOverlayRenderer.RenderAsync(
            proxy,
            composed,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        ReplaceDetailVisualization(composed, overlay);
    }

    private Task RunLoadingImageAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.LoadingImage, "Loading image...", operation);
    }

    private Task RunLoadingProjectAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.LoadingProject, "Loading project...", operation);
    }

    private Task RunLoadingPresetAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.LoadingPreset, "Loading preset...", operation);
    }

    private Task RunRebuildingPreviewAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.RebuildingPreview, "Rebuilding preview...", operation);
    }

    private Task RunAnalyzingDetailAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.AnalyzingDetail, "Updating detail map...", operation);
    }

    private Task RunRenderingPreviewAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.RenderingPreview, "Rendering preview...", operation);
    }

    private Task RunSavingProjectAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.SavingProject, "Saving project...", operation);
    }

    private Task RunSavingPresetAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.SavingPreset, "Saving preset...", operation);
    }

    private Task RunExportingImageAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.ExportingImage, "Exporting image...", operation);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The desktop UI boundary converts unexpected operation failures into structured validation and a visible status message.")]
    private async Task RunOperationAsync(
        WorkspaceOperationKind kind,
        string initialMessage,
        Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (_operationCancellation is not null || _isClosed)
        {
            return;
        }

        using CancellationTokenSource cancellation = new();
        _operationCancellation = cancellation;
        _workspace.BeginOperation(kind, initialMessage);
        _workspace.ClearValidation();
        _statusText.Text = initialMessage;
        SetOperationState(true);

        try
        {
            await operation(cancellation.Token).ConfigureAwait(true);
            string completionMessage = string.IsNullOrWhiteSpace(_statusText.Text)
                ? "Operation completed."
                : _statusText.Text;
            _workspace.CompleteOperation(completionMessage);
        }
        catch (OperationCanceledException)
        {
            if (!_isClosed)
            {
                _statusText.Text = "Operation cancelled.";
            }
        }
        catch (Exception exception)
        {
            _workspace.AddValidation(new WorkspaceValidationMessage(
                $"operation.{kind}",
                exception.Message));
            if (!_isClosed)
            {
                _statusText.Text = exception.Message;
            }
        }
        finally
        {
            _workspace.ResetOperation();
            _operationCancellation = null;
            if (_isClosed)
            {
                DisposeOwnedImages();
            }
            else
            {
                SetOperationState(false);
            }
        }
    }

    private Progress<ImageOperationProgress> CreateImageProgress()
    {
        return new Progress<ImageOperationProgress>(value =>
            ReportOperationProgress(value.Fraction, value.Message));
    }

    private void ReportOperationProgress(double fraction, string message)
    {
        _operationProgressBar.Value = fraction * 100d;
        _statusText.Text = message;
        if (_workspace.Operation.IsBusy)
        {
            _workspace.ReportOperation(fraction, message);
        }
    }

    private void SetOperationState(bool running)
    {
        _openButton.IsEnabled = !running;
        _openProjectButton.IsEnabled = !running;
        _saveProjectButton.IsEnabled = !running && !string.IsNullOrWhiteSpace(_currentSourcePath);
        _renderButton.IsEnabled = !running && _proxyImage is not null && _composedDetailMap is not null;
        _saveButton.IsEnabled = !running && _renderedImage is not null;
        _exportFinalButton.IsEnabled = !running && _sourceImage is not null && _previewStrokePlan is not null;
        _cancelButton.IsEnabled = running;
        _settingsPanel.IsEnabled = !running;
        _selectionCanvas.IsEnabled = !running && _composedDetailMap is not null;
        if (!running)
        {
            _operationProgressBar.Value = 0d;
        }
    }

    private void ReplaceSource(
        SkiaImage source,
        SkiaImage proxy,
        DetailMap automaticDetailMap,
        DetailMap composedDetailMap,
        SemanticAnalysisResult semanticAnalysis,
        DetailAnalysisSettings detailSettings,
        SemanticAnalysisSettings semanticSettings,
        SkiaImage detailOverlay)
    {
        Bitmap? preview = null;
        Bitmap? overlayPreview = null;
        SkiaImage? previousSource = _sourceImage;
        SkiaImage? previousProxy = _proxyImage;
        SkiaImage? previousRendered = _renderedImage;
        Bitmap? previousSourcePreview = _sourcePreviewBitmap;
        Bitmap? previousOverlayPreview = _detailOverlayPreviewBitmap;
        Bitmap? previousSemanticOverlayPreview = _semanticOverlayPreviewBitmap;
        Bitmap? previousResultPreview = _resultPreviewBitmap;
        bool adopted = false;

        try
        {
            preview = CreateAvaloniaBitmap(proxy);
            overlayPreview = CreateAvaloniaBitmap(detailOverlay);

            adopted = true;
            _sourceImage = source;
            _proxyImage = proxy;
            _renderedImage = null;
            _previewStrokePlan = null;
            _previewBrushSettings = null;
            _sourcePreviewBitmap = preview;
            _detailOverlayPreviewBitmap = overlayPreview;
            _semanticOverlayPreviewBitmap = null;
            _resultPreviewBitmap = null;
            _automaticDetailMap = automaticDetailMap;
            _composedDetailMap = composedDetailMap;
            _semanticAnalysisResult = semanticAnalysis;
            _activeDetailAnalysisSettings = detailSettings;
            _activeSemanticAnalysisSettings = semanticSettings;
            _workspace.ClearRegions();
            _imageViewportState.Reset();

            UpdateSourcePreviewSelection();
            _resultImageView.Source = null;
            _resultInfoText.Text = "Not rendered";
            _saveButton.IsEnabled = false;
            _exportFinalButton.IsEnabled = false;
            UpdateFinalOutputEstimate();
            ApplySynchronizedViewportTransforms();
            RefreshSemanticRegions();
            RefreshRegionVisuals();
        }
        finally
        {
            if (adopted)
            {
                previousSource?.Dispose();
                previousProxy?.Dispose();
                previousRendered?.Dispose();
                previousSourcePreview?.Dispose();
                previousOverlayPreview?.Dispose();
                previousSemanticOverlayPreview?.Dispose();
                previousResultPreview?.Dispose();
            }
            else
            {
                preview?.Dispose();
                overlayPreview?.Dispose();
            }
        }
    }

    private void ReplaceProxy(
        SkiaImage proxy,
        DetailMap automaticDetailMap,
        DetailMap composedDetailMap,
        SemanticAnalysisResult semanticAnalysis,
        DetailAnalysisSettings detailSettings,
        SemanticAnalysisSettings semanticSettings,
        SkiaImage detailOverlay)
    {
        Bitmap? preview = null;
        Bitmap? overlayPreview = null;
        SkiaImage? previousProxy = _proxyImage;
        SkiaImage? previousRendered = _renderedImage;
        Bitmap? previousSourcePreview = _sourcePreviewBitmap;
        Bitmap? previousOverlayPreview = _detailOverlayPreviewBitmap;
        Bitmap? previousSemanticOverlayPreview = _semanticOverlayPreviewBitmap;
        Bitmap? previousResultPreview = _resultPreviewBitmap;
        bool adopted = false;

        try
        {
            preview = CreateAvaloniaBitmap(proxy);
            overlayPreview = CreateAvaloniaBitmap(detailOverlay);

            adopted = true;
            _proxyImage = proxy;
            _renderedImage = null;
            _previewStrokePlan = null;
            _previewBrushSettings = null;
            _sourcePreviewBitmap = preview;
            _detailOverlayPreviewBitmap = overlayPreview;
            _semanticOverlayPreviewBitmap = null;
            _resultPreviewBitmap = null;
            _automaticDetailMap = automaticDetailMap;
            _composedDetailMap = composedDetailMap;
            _semanticAnalysisResult = semanticAnalysis;
            _activeDetailAnalysisSettings = detailSettings;
            _activeSemanticAnalysisSettings = semanticSettings;

            UpdateSourcePreviewSelection();
            _resultImageView.Source = null;
            _resultInfoText.Text = "Not rendered";
            _saveButton.IsEnabled = false;
            _exportFinalButton.IsEnabled = false;
            if (_sourceImage is not null)
            {
                _sourceInfoText.Text = $"{_sourceImage.Size.Width:N0} × {_sourceImage.Size.Height:N0} → {proxy.Size.Width:N0} × {proxy.Size.Height:N0}";
            }

            UpdateFinalOutputEstimate();
            ApplySynchronizedViewportTransforms();
            RefreshSemanticRegions();
            RefreshRegionVisuals();
        }
        finally
        {
            if (adopted)
            {
                previousProxy?.Dispose();
                previousRendered?.Dispose();
                previousSourcePreview?.Dispose();
                previousOverlayPreview?.Dispose();
                previousSemanticOverlayPreview?.Dispose();
                previousResultPreview?.Dispose();
            }
            else
            {
                preview?.Dispose();
                overlayPreview?.Dispose();
            }
        }
    }

    private void ReplaceDetailVisualization(
        DetailMap composedDetailMap,
        SkiaImage detailOverlay)
    {
        Bitmap preview = CreateAvaloniaBitmap(detailOverlay);
        Bitmap? previousPreview = _detailOverlayPreviewBitmap;
        bool adopted = false;

        try
        {
            adopted = true;
            _detailOverlayPreviewBitmap = preview;
            _composedDetailMap = composedDetailMap;
            InvalidateRenderedPreview();
            UpdateSourcePreviewSelection();
            RefreshRegionVisuals();
        }
        finally
        {
            if (adopted)
            {
                previousPreview?.Dispose();
            }
            else
            {
                preview.Dispose();
            }
        }
    }

    private void ReplaceSemanticOverlayPreview(SkiaImage semanticOverlay)
    {
        Bitmap preview = CreateAvaloniaBitmap(semanticOverlay);
        Bitmap? previousPreview = _semanticOverlayPreviewBitmap;
        bool adopted = false;

        try
        {
            adopted = true;
            _semanticOverlayPreviewBitmap = preview;
            UpdateSourcePreviewSelection();
        }
        finally
        {
            if (adopted)
            {
                previousPreview?.Dispose();
            }
            else
            {
                preview.Dispose();
            }
        }
    }

    private void ClearSemanticOverlayPreview()
    {
        Bitmap? previousPreview = _semanticOverlayPreviewBitmap;
        _semanticOverlayPreviewBitmap = null;
        previousPreview?.Dispose();
        UpdateSourcePreviewSelection();
    }

    private void ReplaceRendered(SkiaImage rendered, StrokePlan plan, BrushSettings brush)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(brush);
        Bitmap preview = CreateAvaloniaBitmap(rendered);
        SkiaImage? previousRendered = _renderedImage;
        Bitmap? previousPreview = _resultPreviewBitmap;
        bool adopted = false;

        try
        {
            adopted = true;
            _renderedImage = rendered;
            _previewStrokePlan = plan;
            _previewBrushSettings = brush;
            _resultPreviewBitmap = preview;
            _resultImageView.Source = preview;
            UpdateFinalOutputEstimate();
            ApplySynchronizedViewportTransforms();
        }
        finally
        {
            if (adopted)
            {
                previousRendered?.Dispose();
                previousPreview?.Dispose();
            }
            else
            {
                preview.Dispose();
            }
        }
    }

    private void InvalidateRenderedPreview()
    {
        SkiaImage? rendered = _renderedImage;
        Bitmap? preview = _resultPreviewBitmap;
        _renderedImage = null;
        _previewStrokePlan = null;
        _previewBrushSettings = null;
        _resultPreviewBitmap = null;
        _resultImageView.Source = null;
        _resultInfoText.Text = "Not rendered";
        _saveButton.IsEnabled = false;
        _exportFinalButton.IsEnabled = false;
        rendered?.Dispose();
        preview?.Dispose();
        UpdateFinalOutputEstimate();
    }

    private void UpdateSourcePreviewSelection()
    {
        if (_showDetailOverlayCheckBox.IsChecked != true)
        {
            _sourceImageView.Source = _sourcePreviewBitmap;
            return;
        }

        SemanticOverlayMode mode = ReadSelectedSemanticOverlayMode();
        _sourceImageView.Source = mode == SemanticOverlayMode.CombinedDetail
            ? _detailOverlayPreviewBitmap ?? _sourcePreviewBitmap
            : _semanticOverlayPreviewBitmap ?? _detailOverlayPreviewBitmap ?? _sourcePreviewBitmap;
    }

    private void RefreshRegionVisuals(string? selectedRegionId = null)
    {
        selectedRegionId ??= GetSelectedRegion()?.Id;
        _selectionCanvas.Children.Clear();
        UniformImageViewport? viewport = CreateSourceViewport();
        if (viewport is not null)
        {
            if (_showDetailOverlayCheckBox.IsChecked == true
                && _semanticAnalysisResult is not null)
            {
                foreach (SemanticRegion semanticRegion in _semanticAnalysisResult.Regions)
                {
                    AddSemanticRegionRectangle(viewport, semanticRegion);
                }
            }

            foreach (DetailRegion region in _workspace.Regions.Regions)
            {
                AddRegionRectangle(viewport, region.Bounds, region.Intent, active: false);
            }

            if (_selectionStart is NormalizedPoint start
                && _selectionCurrent is NormalizedPoint current
                && Math.Abs(current.X - start.X) >= MinimumManualRegionSize
                && Math.Abs(current.Y - start.Y) >= MinimumManualRegionSize)
            {
                AddRegionRectangle(
                    viewport,
                    NormalizedRect.FromCorners(start, current),
                    ReadSelectedDetailIntentOrDefault(),
                    active: true);
            }
        }

        _regionCountText.Text = _workspace.Regions.Count == 1
            ? "1 manual region"
            : $"{_workspace.Regions.Count:N0} manual regions";
        RefreshRegionList(selectedRegionId);
    }

    private void RefreshSemanticRegions()
    {
        SemanticRegion[] regions = _semanticAnalysisResult?.Regions.ToArray() ?? [];
        SemanticRegionDisplayItem[] items = regions
            .Select(region => new SemanticRegionDisplayItem(
                region.Id,
                $"{region.Label ?? region.Id} · {region.Role} · {region.Confidence:P0}"))
            .ToArray();
        _semanticRegionListBox.ItemsSource = items;
        _semanticRegionListBox.SelectedIndex = items.Length > 0 ? 0 : -1;
        _semanticRegionCountText.Text = items.Length switch
        {
            0 => "No semantic subjects detected",
            1 => "1 semantic region detected",
            _ => $"{items.Length:N0} semantic regions detected"
        };
    }

    private SemanticRegion? GetSelectedSemanticRegion()
    {
        if (_semanticRegionListBox.SelectedItem is not SemanticRegionDisplayItem item
            || _semanticAnalysisResult is null)
        {
            return null;
        }

        return _semanticAnalysisResult.Regions.FirstOrDefault(region => string.Equals(
            region.Id,
            item.Id,
            StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshRegionList(string? selectedRegionId)
    {
        RegionDisplayItem[] items = _workspace.Regions.Regions
            .Select(region => new RegionDisplayItem(
                region.Id,
                $"{region.Label ?? region.Id} · {region.Intent}"))
            .ToArray();
        int selectedIndex = selectedRegionId is null
            ? -1
            : Array.FindIndex(items, item => string.Equals(
                item.Id,
                selectedRegionId,
                StringComparison.OrdinalIgnoreCase));

        _suppressRegionSelectionChange = true;
        try
        {
            _regionListBox.ItemsSource = items;
            _regionListBox.SelectedIndex = selectedIndex;
        }
        finally
        {
            _suppressRegionSelectionChange = false;
        }

        DetailRegion? selected = selectedIndex >= 0
            ? _workspace.Regions.Get(items[selectedIndex].Id)
            : null;
        PopulateRegionEditorControls(selected);
    }

    private DetailRegion? GetSelectedRegion()
    {
        if (_regionListBox.SelectedItem is not RegionDisplayItem item)
        {
            return null;
        }

        return _workspace.Regions.Regions.FirstOrDefault(region => string.Equals(
            region.Id,
            item.Id,
            StringComparison.OrdinalIgnoreCase));
    }

    private void PopulateRegionEditorControls(DetailRegion? region)
    {
        if (region is null)
        {
            ClearRegionEditorControls();
            return;
        }

        _regionLabelTextBox.Text = region.Label ?? string.Empty;
        _regionLeftTextBox.Text = FormatDouble(region.Bounds.Left * 100d);
        _regionTopTextBox.Text = FormatDouble(region.Bounds.Top * 100d);
        _regionWidthTextBox.Text = FormatDouble(region.Bounds.Width * 100d);
        _regionHeightTextBox.Text = FormatDouble(region.Bounds.Height * 100d);
        _regionStrengthTextBox.Text = FormatDouble(region.Strength * 100d);
        _detailIntentComboBox.SelectedItem = region.Intent;
    }

    private void ClearRegionEditorControls()
    {
        _regionLabelTextBox.Text = string.Empty;
        _regionLeftTextBox.Text = string.Empty;
        _regionTopTextBox.Text = string.Empty;
        _regionWidthTextBox.Text = string.Empty;
        _regionHeightTextBox.Text = string.Empty;
    }

    private void AddSemanticRegionRectangle(
        UniformImageViewport viewport,
        SemanticRegion region)
    {
        ViewportRect mapped = viewport.MapToViewport(region.Bounds);
        Color color = region.Role switch
        {
            SemanticRegionRole.Subject => Color.FromRgb(255, 196, 64),
            SemanticRegionRole.FocalArea => Color.FromRgb(255, 102, 180),
            SemanticRegionRole.CriticalDetail => Color.FromRgb(255, 64, 64),
            SemanticRegionRole.SupportingArea => Color.FromRgb(130, 180, 255),
            SemanticRegionRole.Background => Color.FromRgb(90, 130, 170),
            SemanticRegionRole.Ignore => Color.FromRgb(140, 140, 140),
            _ => Color.FromRgb(255, 196, 64)
        };
        Rectangle rectangle = new()
        {
            Width = mapped.Width,
            Height = mapped.Height,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 1.5d / _imageViewportState.Zoom,
            Fill = new SolidColorBrush(Color.FromArgb(18, color.R, color.G, color.B)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rectangle, mapped.X);
        Canvas.SetTop(rectangle, mapped.Y);
        _selectionCanvas.Children.Add(rectangle);
    }

    private void AddRegionRectangle(
        UniformImageViewport viewport,
        NormalizedRect bounds,
        DetailRegionIntent intent,
        bool active)
    {
        ViewportRect mapped = viewport.MapToViewport(bounds);
        Color color = intent == DetailRegionIntent.IncreaseDetail
            ? Color.FromRgb(255, 102, 51)
            : Color.FromRgb(38, 184, 224);
        Rectangle rectangle = new()
        {
            Width = mapped.Width,
            Height = mapped.Height,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = (active ? 3d : 2d) / _imageViewportState.Zoom,
            Fill = new SolidColorBrush(Color.FromArgb(active ? (byte)55 : (byte)32, color.R, color.G, color.B)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rectangle, mapped.X);
        Canvas.SetTop(rectangle, mapped.Y);
        _selectionCanvas.Children.Add(rectangle);
    }

    private UniformImageViewport? CreateSourceViewport()
    {
        return CreateViewport(_sourceViewportHost);
    }

    private UniformImageViewport? CreateViewport(Grid viewportHost)
    {
        SkiaImage? proxy = _proxyImage;
        double width = viewportHost.Bounds.Width;
        double height = viewportHost.Bounds.Height;
        if (proxy is null || width <= 0d || height <= 0d)
        {
            return null;
        }

        return new UniformImageViewport(proxy.Size, width, height);
    }

    private void ApplySynchronizedViewportTransforms()
    {
        ApplyViewportTransform(_sourceViewportHost, _sourceViewportTransform);
        ApplyViewportTransform(_resultViewportHost, _resultViewportTransform);
    }

    private void ApplyViewportTransform(
        Grid viewportHost,
        MatrixTransform transform)
    {
        UniformImageViewport? viewport = CreateViewport(viewportHost);
        if (viewport is null)
        {
            transform.Matrix = Matrix.Identity;
            return;
        }

        ImageViewportTransform viewportTransform = _imageViewportState.GetTransform(viewport);
        transform.Matrix = new Matrix(
            viewportTransform.Scale,
            0d,
            0d,
            viewportTransform.Scale,
            viewportTransform.TranslationX,
            viewportTransform.TranslationY);
    }

    private DetailRegionIntent ReadSelectedDetailIntentOrDefault()
    {
        return _detailIntentComboBox.SelectedItem is DetailRegionIntent intent
            ? intent
            : DetailRegionIntent.IncreaseDetail;
    }


    private SemanticOverlayMode ReadSelectedSemanticOverlayMode()
    {
        return _semanticOverlayModeComboBox.SelectedItem is SemanticOverlayMode mode
            ? mode
            : SemanticOverlayMode.CombinedDetail;
    }

    private static DetailMap SelectSemanticOverlayMap(
        SemanticAnalysisResult semantic,
        SemanticOverlayMode mode)
    {
        return mode switch
        {
            SemanticOverlayMode.SemanticImportance => semantic.ImportanceMap,
            SemanticOverlayMode.Saliency => semantic.SaliencyMap,
            SemanticOverlayMode.Subjects => semantic.SubjectMap,
            SemanticOverlayMode.Silhouettes => semantic.SilhouetteMap,
            SemanticOverlayMode.FocalAreas => semantic.FocalMap,
            SemanticOverlayMode.CombinedDetail => semantic.ImportanceMap,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown semantic overlay mode.")
        };
    }

    private static bool DetailAnalysisSettingsEqual(
        DetailAnalysisSettings? first,
        DetailAnalysisSettings second)
    {
        return first is not null
            && first.BaseDetail == second.BaseDetail
            && first.EdgeWeight == second.EdgeWeight
            && first.ContrastWeight == second.ContrastWeight
            && first.SmoothingRadius == second.SmoothingRadius;
    }


    private static bool SemanticAnalysisSettingsEqual(
        SemanticAnalysisSettings? first,
        SemanticAnalysisSettings second)
    {
        return first is not null
            && first.Enabled == second.Enabled
            && first.OverallInfluence == second.OverallInfluence
            && first.SaliencyWeight == second.SaliencyWeight
            && first.SubjectWeight == second.SubjectWeight
            && first.SilhouetteWeight == second.SilhouetteWeight
            && first.FocalWeight == second.FocalWeight
            && first.SubjectThreshold == second.SubjectThreshold
            && first.MinimumSubjectAreaRatio == second.MinimumSubjectAreaRatio
            && first.MaximumSubjects == second.MaximumSubjects
            && first.CenterBias == second.CenterBias
            && first.SmoothingRadius == second.SmoothingRadius
            && first.BoundaryRadius == second.BoundaryRadius;
    }

    private static Bitmap CreateAvaloniaBitmap(SkiaImage image)
    {
        using MemoryStream stream = new(image.EncodePng(), writable: false);
        return new Bitmap(stream);
    }

    private static int ParseInteger(TextBox textBox, string displayName)
    {
        string text = textBox.Text?.Trim() ?? string.Empty;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value)
            && !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            throw new FormatException($"{displayName} must be a valid integer.");
        }

        return value;
    }

    private static ulong ParseUnsignedInteger(TextBox textBox, string displayName)
    {
        if (!TryParseUnsignedInteger(textBox.Text, out ulong value))
        {
            throw new FormatException($"{displayName} must be an integer between 0 and {ulong.MaxValue:N0}.");
        }

        return value;
    }

    private static bool TryParseUnsignedInteger(string? text, out ulong value)
    {
        string normalized = text?.Trim() ?? string.Empty;
        return ulong.TryParse(normalized, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
            || ulong.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static double ParseDouble(TextBox textBox, string displayName)
    {
        string text = textBox.Text?.Trim() ?? string.Empty;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value)
            && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            throw new FormatException($"{displayName} must be a valid number.");
        }

        return value;
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.#######", CultureInfo.CurrentCulture);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180d / Math.PI;
    }

    private sealed record RegionDisplayItem(string Id, string DisplayText)
    {
        public override string ToString()
        {
            return DisplayText;
        }
    }


    private sealed record SemanticRegionDisplayItem(string Id, string DisplayText)
    {
        public override string ToString()
        {
            return DisplayText;
        }
    }

    private sealed record AutomaticAnalysisMaps(
        DetailMap Structural,
        SemanticAnalysisResult Semantic,
        DetailMap Automatic);

    private T FindRequiredControl<T>(string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException($"Required control '{name}' was not found.");
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        _operationCancellation?.Cancel();
        if (_operationCancellation is null)
        {
            DisposeOwnedImages();
        }
    }

    private void DisposeOwnedImages()
    {
        _sourceImage?.Dispose();
        _proxyImage?.Dispose();
        _renderedImage?.Dispose();
        _sourcePreviewBitmap?.Dispose();
        _detailOverlayPreviewBitmap?.Dispose();
        _semanticOverlayPreviewBitmap?.Dispose();
        _resultPreviewBitmap?.Dispose();

        _sourceImage = null;
        _proxyImage = null;
        _renderedImage = null;
        _sourcePreviewBitmap = null;
        _detailOverlayPreviewBitmap = null;
        _semanticOverlayPreviewBitmap = null;
        _resultPreviewBitmap = null;
        _automaticDetailMap = null;
        _composedDetailMap = null;
        _semanticAnalysisResult = null;
        _previewStrokePlan = null;
        _previewBrushSettings = null;
        _activeDetailAnalysisSettings = null;
        _activeSemanticAnalysisSettings = null;
        _imageViewportState.Reset();
    }
}
