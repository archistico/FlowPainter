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
using FlowPainter.Application.Background;
using FlowPainter.Application.Boundaries;
using FlowPainter.Application.Detail;
using FlowPainter.Application.FlowPainting.Fields;
using FlowPainter.Application.FlowPainting.Planning;
using FlowPainter.Application.FlowPainting.Presets;
using FlowPainter.Application.Interaction;
using FlowPainter.Application.Images;
using FlowPainter.Application.Hybrid;
using FlowPainter.Application.PrimitiveGeneration;
using FlowPainter.Application.Projects;
using FlowPainter.Application.Semantics;
using FlowPainter.Application.Workflow;
using FlowPainter.Application.Workloads;
using FlowPainter.Domain.Boundaries;
using FlowPainter.Domain.Brushes;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.FlowFields;
using FlowPainter.Domain.Generation;
using FlowPainter.Domain.Geometry;
using FlowPainter.Domain.Images;
using FlowPainter.Domain.Hybrid;
using FlowPainter.Domain.Primitives;
using FlowPainter.Domain.Strokes;
using FlowPainter.Domain.Semantics;
using FlowPainter.Imaging.Skia.Images;
using FlowPainter.Rendering.Skia.Boundaries;
using FlowPainter.Rendering.Skia.Detail;
using FlowPainter.Rendering.Skia.Hybrid;
using FlowPainter.Rendering.Skia.Primitives;
using FlowPainter.Rendering.Skia.Strokes;

namespace FlowPainter.App;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Avalonia owns the Window lifetime; native resources are released after the Closed event and any active operation has stopped.")]
public partial class MainWindow : Window
{
    private const ulong InitialSeed = 0xF10A_2026UL;
    private const double MinimumManualRegionSize = 0.002d;
    private const double RegionSelectionDragThresholdPixels = 6d;

    private readonly SkiaImageLoader _imageLoader = new();
    private readonly SkiaImageProxyGenerator _proxyGenerator = new();
    private readonly SkiaStrokePlanRenderer _renderer = new();
    private readonly PrimitivePlanOptimizer _primitiveOptimizer = new();
    private readonly SkiaPrimitivePlanRenderer _primitiveRenderer = new();
    private readonly HybridPlanComposer _hybridComposer = new();
    private readonly SkiaHybridPlanRenderer _hybridRenderer = new();
    private readonly SkiaPngEncoder _pngEncoder = new();
    private readonly SkiaImageEncoder _imageEncoder = new();
    private readonly ImageDetailAnalyzer _detailAnalyzer = new();
    private readonly HeuristicSemanticImportanceAnalyzer _semanticAnalyzer = new();
    private readonly HeuristicSceneBoundaryAnalyzer _boundaryAnalyzer = new();
    private readonly DetailMapOverlayRenderer _detailOverlayRenderer = new();
    private readonly BoundaryDirectionOverlayRenderer _boundaryDirectionOverlayRenderer = new();
    private readonly FlowPainterPlanner _planner = new(new DefaultFlowFieldFactory());
    private readonly FlowPainterWorkspace _workspace = new(InitialSeed, BuiltInFlowPainterPresets.All[0].Settings);
    private readonly ProjectSessionController _projectSessionController;
    private readonly RecentPathList _recentProjects = new();
    private readonly RecentPathList _recentPresets = new();

    private readonly Button _openButton;
    private readonly Button _openProjectButton;
    private readonly Button _saveProjectButton;
    private readonly Button _renderButton;
    private readonly Button _cancelButton;
    private readonly Button _saveButton;
    private readonly Button _exportFinalButton;
    private readonly Button _exportSvgButton;
    private readonly StackPanel _settingsPanel;
    private readonly ComboBox _presetComboBox;
    private readonly ComboBox _generativeModeComboBox;
    private readonly ComboBox _primitiveKindsComboBox;
    private readonly ComboBox _hybridInfluenceKindComboBox;
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
    private readonly TextBox _primitiveCountTextBox;
    private readonly TextBox _primitiveCandidatesTextBox;
    private readonly TextBox _primitiveMutationsTextBox;
    private readonly TextBox _primitiveMinimumSizeTextBox;
    private readonly TextBox _primitiveMaximumSizeTextBox;
    private readonly TextBox _primitiveOpacityTextBox;
    private readonly TextBox _primitiveDetailSizeTextBox;
    private readonly TextBox _primitivePlacementBiasTextBox;
    private readonly TextBox _primitiveErrorWeightTextBox;
    private readonly TextBox _primitiveSearchInfluenceTextBox;
    private readonly TextBox _hybridPrimitiveBudgetTextBox;
    private readonly TextBox _hybridFlowBudgetTextBox;
    private readonly TextBox _hybridRefinementBudgetTextBox;
    private readonly TextBox _hybridInfluenceStrengthTextBox;
    private readonly TextBox _hybridInfluenceRadiusTextBox;
    private readonly TextBox _hybridMaximumInfluencesTextBox;
    private readonly TextBox _hybridRefinementDetailBiasTextBox;
    private readonly TextBox _hybridRefinementLengthTextBox;
    private readonly TextBox _hybridRefinementWidthTextBox;
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
    private readonly TextBox _regionTransitionWidthTextBox;
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
    private readonly TextBox _boundaryLuminanceWeightTextBox;
    private readonly TextBox _boundaryColorWeightTextBox;
    private readonly TextBox _boundaryMultiscaleWeightTextBox;
    private readonly TextBox _boundaryContinuityWeightTextBox;
    private readonly TextBox _boundarySemanticWeightTextBox;
    private readonly TextBox _boundaryTextureSuppressionTextBox;
    private readonly TextBox _boundaryEdgeThresholdTextBox;
    private readonly TextBox _boundaryImportantThresholdTextBox;
    private readonly TextBox _boundaryCoarseRadiusTextBox;
    private readonly TextBox _boundarySmoothingRadiusTextBox;
    private readonly TextBox _boundaryProtectionRadiusTextBox;
    private readonly TextBox _boundaryTangentAlignmentTextBox;
    private readonly TextBox _boundaryAlignmentRadiusTextBox;
    private readonly TextBox _boundaryCrossingPenaltyTextBox;
    private readonly TextBox _boundaryHardThresholdTextBox;
    private readonly TextBox _boundaryTerminationStrengthTextBox;
    private readonly TextBox _boundaryInternalInfluenceTextBox;
    private readonly TextBox _boundaryTextureInfluenceTextBox;
    private readonly TextBox _boundaryContourReinforcementTextBox;
    private readonly TextBox _boundaryCornerPreservationTextBox;
    private readonly TextBox _backgroundSuppressionStrengthTextBox;
    private readonly TextBox _backgroundDetailFloorTextBox;
    private readonly TextBox _backgroundUncertaintyProtectionTextBox;
    private readonly TextBox _backgroundSilhouetteProtectionTextBox;
    private readonly TextBox _backgroundTransitionSoftnessTextBox;
    private readonly TextBox _backgroundPlacementWeightTextBox;
    private readonly TextBox _backgroundSuppressionLengthTextBox;
    private readonly TextBox _backgroundSuppressionWidthTextBox;
    private readonly TextBox _backgroundSegmentMultiplierTextBox;
    private readonly TextBox _backgroundCurveFreedomTextBox;
    private readonly TextBox _backgroundColorSimplificationTextBox;
    private readonly ListBox _regionListBox;
    private readonly ListBox _semanticRegionListBox;
    private readonly ListBox _semanticCorrectionListBox;
    private readonly TextBox _regionLabelTextBox;
    private readonly TextBox _regionLeftTextBox;
    private readonly TextBox _regionTopTextBox;
    private readonly TextBox _regionWidthTextBox;
    private readonly TextBox _regionHeightTextBox;
    private readonly CheckBox _showDetailOverlayCheckBox;
    private readonly CheckBox _enableSemanticAnalysisCheckBox;
    private readonly CheckBox _enableBoundaryAnalysisCheckBox;
    private readonly CheckBox _enableBoundaryPaintingCheckBox;
    private readonly CheckBox _enableBackgroundSuppressionCheckBox;
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
    private readonly TextBlock _semanticCorrectionCountText;
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
    private SceneBoundaryAnalysisResult? _sceneBoundaryAnalysisResult;
    private BackgroundSuppressionResult? _backgroundSuppressionResult;
    private StrokePlan? _previewStrokePlan;
    private PrimitivePlan? _previewPrimitivePlan;
    private HybridPlan? _previewHybridPlan;
    private BrushSettings? _previewBrushSettings;
    private GenerativeMode _previewGenerativeMode = GenerativeMode.FlowPainting;
    private DetailAnalysisSettings? _activeDetailAnalysisSettings;
    private double _activeDetailRegionTransitionWidth = double.NaN;
    private long _activeDetailRegionRevision = -1L;
    private long _activeSemanticCorrectionRevision = -1L;
    private SemanticAnalysisSettings? _activeSemanticAnalysisSettings;
    private SceneBoundaryAnalysisSettings? _activeBoundaryAnalysisSettings;
    private BackgroundSuppressionSettings? _activeBackgroundSuppressionSettings;
    private readonly SynchronizedImageViewportState _imageViewportState = new();
    private NormalizedPoint? _selectionStart;
    private NormalizedPoint? _selectionCurrent;
    private Point? _selectionPointerStartPosition;
    private Grid? _panViewportHost;
    private Point _panLastPosition;
    private CancellationTokenSource? _operationCancellation;
    private string? _currentSourcePath;
    private string? _currentProjectPath;
    private bool _suppressRegionSelectionChange;
    private bool _suppressSemanticRegionSelectionChange;
    private bool _suppressSemanticCorrectionSelectionChange;
    private bool _suppressDirtyTracking;
    private bool _allowClose;
    private bool _closeGuardRunning;
    private bool _isClosed;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _projectSessionController = new ProjectSessionController(_workspace);
        _openButton = FindRequiredControl<Button>("OpenButton");
        _openProjectButton = FindRequiredControl<Button>("OpenProjectButton");
        _saveProjectButton = FindRequiredControl<Button>("SaveProjectButton");
        _renderButton = FindRequiredControl<Button>("RenderButton");
        _cancelButton = FindRequiredControl<Button>("CancelButton");
        _saveButton = FindRequiredControl<Button>("SaveButton");
        _exportFinalButton = FindRequiredControl<Button>("ExportFinalButton");
        _exportSvgButton = FindRequiredControl<Button>("ExportSvgButton");
        _settingsPanel = FindRequiredControl<StackPanel>("SettingsPanel");
        _presetComboBox = FindRequiredControl<ComboBox>("PresetComboBox");
        _generativeModeComboBox = FindRequiredControl<ComboBox>("GenerativeModeComboBox");
        _primitiveKindsComboBox = FindRequiredControl<ComboBox>("PrimitiveKindsComboBox");
        _hybridInfluenceKindComboBox = FindRequiredControl<ComboBox>("HybridInfluenceKindComboBox");
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
        _primitiveCountTextBox = FindRequiredControl<TextBox>("PrimitiveCountTextBox");
        _primitiveCandidatesTextBox = FindRequiredControl<TextBox>("PrimitiveCandidatesTextBox");
        _primitiveMutationsTextBox = FindRequiredControl<TextBox>("PrimitiveMutationsTextBox");
        _primitiveMinimumSizeTextBox = FindRequiredControl<TextBox>("PrimitiveMinimumSizeTextBox");
        _primitiveMaximumSizeTextBox = FindRequiredControl<TextBox>("PrimitiveMaximumSizeTextBox");
        _primitiveOpacityTextBox = FindRequiredControl<TextBox>("PrimitiveOpacityTextBox");
        _primitiveDetailSizeTextBox = FindRequiredControl<TextBox>("PrimitiveDetailSizeTextBox");
        _primitivePlacementBiasTextBox = FindRequiredControl<TextBox>("PrimitivePlacementBiasTextBox");
        _primitiveErrorWeightTextBox = FindRequiredControl<TextBox>("PrimitiveErrorWeightTextBox");
        _primitiveSearchInfluenceTextBox = FindRequiredControl<TextBox>("PrimitiveSearchInfluenceTextBox");
        _hybridPrimitiveBudgetTextBox = FindRequiredControl<TextBox>("HybridPrimitiveBudgetTextBox");
        _hybridFlowBudgetTextBox = FindRequiredControl<TextBox>("HybridFlowBudgetTextBox");
        _hybridRefinementBudgetTextBox = FindRequiredControl<TextBox>("HybridRefinementBudgetTextBox");
        _hybridInfluenceStrengthTextBox = FindRequiredControl<TextBox>("HybridInfluenceStrengthTextBox");
        _hybridInfluenceRadiusTextBox = FindRequiredControl<TextBox>("HybridInfluenceRadiusTextBox");
        _hybridMaximumInfluencesTextBox = FindRequiredControl<TextBox>("HybridMaximumInfluencesTextBox");
        _hybridRefinementDetailBiasTextBox = FindRequiredControl<TextBox>("HybridRefinementDetailBiasTextBox");
        _hybridRefinementLengthTextBox = FindRequiredControl<TextBox>("HybridRefinementLengthTextBox");
        _hybridRefinementWidthTextBox = FindRequiredControl<TextBox>("HybridRefinementWidthTextBox");
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
        _regionTransitionWidthTextBox = FindRequiredControl<TextBox>("RegionTransitionWidthTextBox");
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
        _boundaryLuminanceWeightTextBox = FindRequiredControl<TextBox>("BoundaryLuminanceWeightTextBox");
        _boundaryColorWeightTextBox = FindRequiredControl<TextBox>("BoundaryColorWeightTextBox");
        _boundaryMultiscaleWeightTextBox = FindRequiredControl<TextBox>("BoundaryMultiscaleWeightTextBox");
        _boundaryContinuityWeightTextBox = FindRequiredControl<TextBox>("BoundaryContinuityWeightTextBox");
        _boundarySemanticWeightTextBox = FindRequiredControl<TextBox>("BoundarySemanticWeightTextBox");
        _boundaryTextureSuppressionTextBox = FindRequiredControl<TextBox>("BoundaryTextureSuppressionTextBox");
        _boundaryEdgeThresholdTextBox = FindRequiredControl<TextBox>("BoundaryEdgeThresholdTextBox");
        _boundaryImportantThresholdTextBox = FindRequiredControl<TextBox>("BoundaryImportantThresholdTextBox");
        _boundaryCoarseRadiusTextBox = FindRequiredControl<TextBox>("BoundaryCoarseRadiusTextBox");
        _boundarySmoothingRadiusTextBox = FindRequiredControl<TextBox>("BoundarySmoothingRadiusTextBox");
        _boundaryProtectionRadiusTextBox = FindRequiredControl<TextBox>("BoundaryProtectionRadiusTextBox");
        _boundaryTangentAlignmentTextBox = FindRequiredControl<TextBox>("BoundaryTangentAlignmentTextBox");
        _boundaryAlignmentRadiusTextBox = FindRequiredControl<TextBox>("BoundaryAlignmentRadiusTextBox");
        _boundaryCrossingPenaltyTextBox = FindRequiredControl<TextBox>("BoundaryCrossingPenaltyTextBox");
        _boundaryHardThresholdTextBox = FindRequiredControl<TextBox>("BoundaryHardThresholdTextBox");
        _boundaryTerminationStrengthTextBox = FindRequiredControl<TextBox>("BoundaryTerminationStrengthTextBox");
        _boundaryInternalInfluenceTextBox = FindRequiredControl<TextBox>("BoundaryInternalInfluenceTextBox");
        _boundaryTextureInfluenceTextBox = FindRequiredControl<TextBox>("BoundaryTextureInfluenceTextBox");
        _boundaryContourReinforcementTextBox = FindRequiredControl<TextBox>("BoundaryContourReinforcementTextBox");
        _boundaryCornerPreservationTextBox = FindRequiredControl<TextBox>("BoundaryCornerPreservationTextBox");
        _backgroundSuppressionStrengthTextBox = FindRequiredControl<TextBox>("BackgroundSuppressionStrengthTextBox");
        _backgroundDetailFloorTextBox = FindRequiredControl<TextBox>("BackgroundDetailFloorTextBox");
        _backgroundUncertaintyProtectionTextBox = FindRequiredControl<TextBox>("BackgroundUncertaintyProtectionTextBox");
        _backgroundSilhouetteProtectionTextBox = FindRequiredControl<TextBox>("BackgroundSilhouetteProtectionTextBox");
        _backgroundTransitionSoftnessTextBox = FindRequiredControl<TextBox>("BackgroundTransitionSoftnessTextBox");
        _backgroundPlacementWeightTextBox = FindRequiredControl<TextBox>("BackgroundPlacementWeightTextBox");
        _backgroundSuppressionLengthTextBox = FindRequiredControl<TextBox>("BackgroundSuppressionLengthTextBox");
        _backgroundSuppressionWidthTextBox = FindRequiredControl<TextBox>("BackgroundSuppressionWidthTextBox");
        _backgroundSegmentMultiplierTextBox = FindRequiredControl<TextBox>("BackgroundSegmentMultiplierTextBox");
        _backgroundCurveFreedomTextBox = FindRequiredControl<TextBox>("BackgroundCurveFreedomTextBox");
        _backgroundColorSimplificationTextBox = FindRequiredControl<TextBox>("BackgroundColorSimplificationTextBox");
        _regionListBox = FindRequiredControl<ListBox>("RegionListBox");
        _semanticRegionListBox = FindRequiredControl<ListBox>("SemanticRegionListBox");
        _semanticCorrectionListBox = FindRequiredControl<ListBox>("SemanticCorrectionListBox");
        _regionLabelTextBox = FindRequiredControl<TextBox>("RegionLabelTextBox");
        _regionLeftTextBox = FindRequiredControl<TextBox>("RegionLeftTextBox");
        _regionTopTextBox = FindRequiredControl<TextBox>("RegionTopTextBox");
        _regionWidthTextBox = FindRequiredControl<TextBox>("RegionWidthTextBox");
        _regionHeightTextBox = FindRequiredControl<TextBox>("RegionHeightTextBox");
        _showDetailOverlayCheckBox = FindRequiredControl<CheckBox>("ShowDetailOverlayCheckBox");
        _enableSemanticAnalysisCheckBox = FindRequiredControl<CheckBox>("EnableSemanticAnalysisCheckBox");
        _enableBoundaryAnalysisCheckBox = FindRequiredControl<CheckBox>("EnableBoundaryAnalysisCheckBox");
        _enableBoundaryPaintingCheckBox = FindRequiredControl<CheckBox>("EnableBoundaryPaintingCheckBox");
        _enableBackgroundSuppressionCheckBox = FindRequiredControl<CheckBox>("EnableBackgroundSuppressionCheckBox");
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
        _semanticCorrectionCountText = FindRequiredControl<TextBlock>("SemanticCorrectionCountText");
        _statusText = FindRequiredControl<TextBlock>("StatusText");
        _operationProgressBar = FindRequiredControl<ProgressBar>("OperationProgressBar");

        _presetComboBox.ItemsSource = BuiltInFlowPainterPresets.All;
        _generativeModeComboBox.ItemsSource = new[]
        {
            GenerativeMode.FlowPainting,
            GenerativeMode.GeometricPrimitives,
            GenerativeMode.Hybrid
        };
        _generativeModeComboBox.SelectedItem = GenerativeMode.FlowPainting;
        _primitiveKindsComboBox.ItemsSource = Enumerable
            .Range(1, (int)PrimitiveKindSet.All)
            .Select(value => (PrimitiveKindSet)value)
            .OrderByDescending(value => value == PrimitiveKindSet.All)
            .ThenBy(value => CountEnabledPrimitiveKinds(value))
            .ThenBy(value => (int)value)
            .ToArray();
        ApplyPrimitiveGenerationSettings(new PrimitiveGenerationSettings());
        _hybridInfluenceKindComboBox.ItemsSource = Enum.GetValues<PrimitiveFlowInfluenceKind>();
        ApplyHybridGenerationSettings(new HybridGenerationSettings());
        _previewQualityComboBox.ItemsSource = Enum.GetValues<PreviewQuality>();
        _previewQualityComboBox.SelectedItem = PreviewQuality.Standard;
        _finalFormatComboBox.ItemsSource = Enum.GetValues<RasterImageFormat>();
        ApplyFinalRenderSettings(new FinalRenderSettings());
        _flowFieldComboBox.ItemsSource = Enum.GetValues<FlowFieldKind>();
        _backgroundComboBox.ItemsSource = Enum.GetValues<StrokePlanBackgroundMode>();
        _brushKindComboBox.ItemsSource = Enum.GetValues<BrushKind>();
        _detailIntentComboBox.ItemsSource = Enum.GetValues<DetailRegionIntent>();
        _semanticOverlayModeComboBox.ItemsSource = Enum.GetValues<SceneBoundaryOverlayMode>();
        _semanticOverlayModeComboBox.SelectedItem = SceneBoundaryOverlayMode.CombinedDetail;
        _detailIntentComboBox.SelectedItem = DetailRegionIntent.IncreaseDetail;
        _regionStrengthTextBox.Text = "80";
        _projectNameTextBox.Text = "Untitled project";
        LoadRecentItemsAtStartup();
        _presetComboBox.SelectionChanged += (_, _) => ApplySelectedBuiltInPreset();
        ApplyPreset(BuiltInFlowPainterPresets.All[0], InitialSeed);
        _presetComboBox.SelectedIndex = 0;
        AttachProjectDirtyTracking();
        KeyDown += WindowKeyDown;
        Closing += WindowClosing;
        Closed += WindowClosed;
    }

    private async void OpenProjectClick(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmSessionReplacementAsync().ConfigureAwait(true))
        {
            return;
        }

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

        if (!await ConfirmSessionReplacementAsync().ConfigureAwait(true))
        {
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

        FlowPainterProject resolvedProject = new(
            project.Name,
            sourcePath,
            project.Seed,
            project.Settings,
            project.Preview,
            project.DetailRegions,
            project.FinalRender,
            project.Mode,
            project.PrimitiveGeneration,
            project.HybridGeneration,
            project.SemanticCorrections);
        WorkspaceProjectCandidate workspaceCandidate = FlowPainterWorkspace.PrepareProjectLoad(
            resolvedProject,
            projectPath);

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
            EnsureAnalysisMemoryBudget(loaded.Size, previewMaximumDimension);
            proxy = await _proxyGenerator.CreateProxyAsync(
                loaded,
                previewMaximumDimension,
                previewMaximumDimension,
                progress,
                cancellationToken).ConfigureAwait(true);
            AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                proxy,
                project.Settings.DetailAnalysis,
                project.Settings.DetailInfluence,
                project.Settings.SemanticAnalysis,
                project.Settings.BoundaryAnalysis,
                project.SemanticCorrections,
                cancellationToken).ConfigureAwait(true);
            DetailMap manuallyComposedMap = DetailMapComposer.ApplyRegions(
                maps.Automatic,
                project.DetailRegions,
                project.Settings.DetailInfluence.RegionTransitionWidth,
                cancellationToken);
            BackgroundSuppressionResult backgroundSuppression = ComposeBackgroundSuppression(
                maps,
                manuallyComposedMap,
                project.Settings.BackgroundSuppression,
                cancellationToken);
            overlay = await _detailOverlayRenderer.RenderAsync(
                proxy,
                backgroundSuppression.EffectiveDetailMap,
                cancellationToken: cancellationToken).ConfigureAwait(true);

            cancellationToken.ThrowIfCancellationRequested();
            ReplaceSource(
                loaded,
                proxy,
                maps.Automatic,
                backgroundSuppression.EffectiveDetailMap,
                maps.Semantic,
                maps.Boundary,
                backgroundSuppression,
                project.Settings.DetailAnalysis,
                project.Settings.DetailInfluence,
                project.Settings.SemanticAnalysis,
                project.Settings.BoundaryAnalysis,
                project.Settings.BackgroundSuppression,
                overlay);
            adopted = true;

            _currentSourcePath = sourcePath;
            _currentProjectPath = projectPath;
            _workspace.LoadProject(workspaceCandidate);
            RefreshDirtyIndicator();
            CaptureActiveWorkspaceRevisions();
            ApplyProjectControls(project);
            _recentProjects.Add(projectPath);
            await PersistRecentItemsBestEffortAsync(cancellationToken).ConfigureAwait(true);
            RefreshRecentItemControls();
            _saveProjectButton.IsEnabled = true;
            _sourceInfoText.Text = $"{loaded.Size.Width:N0} × {loaded.Size.Height:N0} → {proxy.Size.Width:N0} × {proxy.Size.Height:N0}";
            UpdateFinalOutputEstimate();
            _statusText.Text = $"Loaded project '{project.Name}' with {project.DetailRegions.Count:N0} detail regions and {project.SemanticCorrections.Count:N0} semantic corrections.";
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
        await SaveProjectAsync().ConfigureAwait(true);
    }

    private async Task<bool> SaveProjectAsync()
    {
        string? currentSourcePath = _currentSourcePath;
        if (string.IsNullOrWhiteSpace(currentSourcePath))
        {
            _statusText.Text = "Open an image before saving a project.";
            return false;
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
            _workspace.SetGenerativeMode(ReadSelectedGenerativeMode());
            _workspace.SetPrimitiveGeneration(ReadPrimitiveGenerationSettings());
            _workspace.SetHybridGeneration(ReadHybridGenerationSettings());
            project = _workspace.CreateProject(name);
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return false;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return false;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return false;
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
            return false;
        }

        bool saved = await RunSavingProjectAsync(async cancellationToken =>
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
                project.FinalRender,
                project.Mode,
                project.PrimitiveGeneration,
                project.HybridGeneration,
                project.SemanticCorrections);
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await FlowPainterProjectSerializer.SerializeAsync(
                persistedProject,
                output,
                cancellationToken).ConfigureAwait(true);
            _currentProjectPath = projectPath;
            _workspace.MarkSaved(projectPath, project.Name);
            RefreshDirtyIndicator();
            _recentProjects.Add(projectPath);
            await PersistRecentItemsBestEffortAsync(cancellationToken).ConfigureAwait(true);
            RefreshRecentItemControls();
            _statusText.Text = $"Saved project '{project.Name}'.";
        }).ConfigureAwait(true);

        return saved;
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
        DetailInfluenceSettings detailInfluenceSettings;
        PreviewSettings previewSettings;
        try
        {
            detailSettings = ReadDetailAnalysisSettings();
            detailInfluenceSettings = ReadDetailInfluenceSettings();
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
                EnsureAnalysisMemoryBudget(source.Size, previewSettings.MaximumDimension);
                proxy = await _proxyGenerator.CreateProxyAsync(
                    source,
                    previewSettings.MaximumDimension,
                    previewSettings.MaximumDimension,
                    progress,
                    cancellationToken).ConfigureAwait(true);
                SemanticAnalysisSettings semanticSettings = ReadSemanticAnalysisSettings();
                SceneBoundaryAnalysisSettings boundarySettings = ReadBoundaryAnalysisSettings();
                BackgroundSuppressionSettings backgroundSettings = ReadBackgroundSuppressionSettings();
                AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                    proxy,
                    detailSettings,
                    detailInfluenceSettings,
                    semanticSettings,
                    boundarySettings,
                    _workspace.SemanticCorrections.Regions,
                    cancellationToken).ConfigureAwait(true);
                DetailMap manuallyComposedMap = DetailMapComposer.ApplyRegions(
                    maps.Automatic,
                    _workspace.Regions.Regions,
                    detailInfluenceSettings.RegionTransitionWidth,
                    cancellationToken);
                BackgroundSuppressionResult backgroundSuppression = ComposeBackgroundSuppression(
                    maps,
                    manuallyComposedMap,
                    backgroundSettings,
                    cancellationToken);
                overlay = await _detailOverlayRenderer.RenderAsync(
                    proxy,
                    backgroundSuppression.EffectiveDetailMap,
                    cancellationToken: cancellationToken).ConfigureAwait(true);
                ReplaceProxy(
                    proxy,
                    maps.Automatic,
                    backgroundSuppression.EffectiveDetailMap,
                    maps.Semantic,
                    maps.Boundary,
                    backgroundSuppression,
                    detailSettings,
                    detailInfluenceSettings,
                    semanticSettings,
                    boundarySettings,
                    backgroundSettings,
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
        if (!await ConfirmSessionReplacementAsync().ConfigureAwait(true))
        {
            return;
        }

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
                EnsureAnalysisMemoryBudget(loaded.Size, previewSettings.MaximumDimension);
                proxy = await _proxyGenerator.CreateProxyAsync(
                    loaded,
                    previewSettings.MaximumDimension,
                    previewSettings.MaximumDimension,
                    progress,
                    cancellationToken).ConfigureAwait(true);
                AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                    proxy,
                    detailSettings,
                    settings.DetailInfluence,
                    settings.SemanticAnalysis,
                    settings.BoundaryAnalysis,
                    Array.Empty<SemanticCorrectionRegion>(),
                    cancellationToken).ConfigureAwait(true);
                BackgroundSuppressionResult backgroundSuppression = ComposeBackgroundSuppression(
                    maps,
                    maps.Automatic,
                    settings.BackgroundSuppression,
                    cancellationToken);
                overlay = await _detailOverlayRenderer.RenderAsync(
                    proxy,
                    backgroundSuppression.EffectiveDetailMap,
                    cancellationToken: cancellationToken).ConfigureAwait(true);

                ReplaceSource(
                    loaded,
                    proxy,
                    maps.Automatic,
                    backgroundSuppression.EffectiveDetailMap,
                    maps.Semantic,
                    maps.Boundary,
                    backgroundSuppression,
                    detailSettings,
                    settings.DetailInfluence,
                    settings.SemanticAnalysis,
                    settings.BoundaryAnalysis,
                    settings.BackgroundSuppression,
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
                RefreshDirtyIndicator();
                CaptureActiveWorkspaceRevisions();
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
        PrimitiveGenerationSettings primitiveSettings;
        HybridGenerationSettings hybridSettings;
        GenerativeMode generativeMode;
        ulong seed;
        try
        {
            settings = ReadSettingsFromControls();
            generativeMode = ReadSelectedGenerativeMode();
            primitiveSettings = generativeMode == GenerativeMode.FlowPainting
                ? _workspace.PrimitiveGeneration
                : ReadPrimitiveGenerationSettings();
            hybridSettings = generativeMode == GenerativeMode.Hybrid
                ? ReadHybridGenerationSettings()
                : _workspace.HybridGeneration;
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

        if (generativeMode == GenerativeMode.GeometricPrimitives)
        {
            await RenderPrimitivePreviewAsync(
                proxyImage,
                settings,
                primitiveSettings,
                seed).ConfigureAwait(true);
            return;
        }

        if (generativeMode == GenerativeMode.Hybrid)
        {
            await RenderHybridPreviewAsync(
                proxyImage,
                settings,
                primitiveSettings,
                hybridSettings,
                seed).ConfigureAwait(true);
            return;
        }

        await RunRenderingPreviewAsync(async cancellationToken =>
        {
            DetailMap detailMap = await EnsureDetailMapAsync(
                proxyImage,
                settings.DetailAnalysis,
                settings.DetailInfluence,
                settings.SemanticAnalysis,
                settings.BoundaryAnalysis,
                settings.BackgroundSuppression,
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

            SceneBoundaryAnalysisResult boundaryAnalysis = _sceneBoundaryAnalysisResult
                ?? SceneBoundaryAnalysisResult.CreateEmpty(proxyImage.Size);
            BackgroundSuppressionResult backgroundSuppression = _backgroundSuppressionResult
                ?? BackgroundSuppressionResult.CreateDisabled(detailMap);
            StrokePlan plan = await Task.Run(
                () => settings.BackgroundSuppression.Enabled
                    ? _planner.CreatePlan(
                        proxyImage,
                        densityMap,
                        backgroundSuppression,
                        boundaryAnalysis,
                        seed,
                        settings,
                        planningProgress,
                        cancellationToken)
                    : settings.BoundaryPainting.Enabled
                        ? _planner.CreatePlan(
                            proxyImage,
                            densityMap,
                            detailMap,
                            boundaryAnalysis,
                            seed,
                            settings,
                            planningProgress,
                            cancellationToken)
                        : _planner.CreatePlan(
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
                _workspace.SetGenerativeMode(GenerativeMode.FlowPainting);
                _workspace.SetSettings(settings);

                _resultInfoText.Text = $"{rendered.Size.Width:N0} × {rendered.Size.Height:N0} · {plan.Strokes.Count:N0} strokes · {settings.Brush.Kind} · {_workspace.Regions.Count:N0} regions";
                _statusText.Text = settings.BackgroundSuppression.Enabled
                    ? $"Preview rendered with {settings.Brush.Kind}, protected silhouettes and painterly background suppression."
                    : settings.BoundaryPainting.Enabled
                        ? $"Preview rendered with {settings.Brush.Kind}, detail guidance and boundary-aware flow."
                        : $"Preview rendered with the {settings.Brush.Kind} brush and detail guidance.";
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

    private async Task RenderPrimitivePreviewAsync(
        SkiaImage proxyImage,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        ulong seed)
    {
        await RunRenderingPreviewAsync(async cancellationToken =>
        {
            DetailMap detailMap = await EnsureDetailMapAsync(
                proxyImage,
                flowSettings.DetailAnalysis,
                flowSettings.DetailInfluence,
                flowSettings.SemanticAnalysis,
                flowSettings.BoundaryAnalysis,
                flowSettings.BackgroundSuppression,
                cancellationToken).ConfigureAwait(true);
            Progress<PrimitiveGenerationProgress> planningProgress = new(value =>
            {
                string message = value.Stage == PrimitiveGenerationStage.Searching
                    ? $"Optimizing primitives {value.CompletedPrimitives:N0} / {value.RequestedPrimitives:N0}"
                    : $"Primitive planning: {value.Stage}";
                ReportOperationProgress(value.Fraction * 0.82d, message);
            });
            PrimitivePlan plan = await Task.Run(
                () => _primitiveOptimizer.CreatePlan(
                    proxyImage,
                    detailMap,
                    seed,
                    primitiveSettings,
                    planningProgress,
                    cancellationToken),
                cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();

            Progress<PrimitiveRenderProgress> renderProgress = new(value =>
                ReportOperationProgress(
                    0.82d + (value.Fraction * 0.18d),
                    value.Stage == PrimitiveRenderStage.DrawingPrimitives
                        ? $"Rendering primitives {value.CompletedPrimitives:N0} / {value.TotalPrimitives:N0}"
                        : $"Primitive rendering: {value.Stage}"));
            SkiaImage rendered = await _primitiveRenderer.RenderAsync(
                plan,
                proxyImage.Size,
                renderProgress,
                cancellationToken).ConfigureAwait(true);
            bool adopted = false;

            try
            {
                ReplaceRenderedPrimitive(rendered, plan);
                adopted = true;
                _workspace.SetGenerativeMode(GenerativeMode.GeometricPrimitives);
                _workspace.SetPrimitiveGeneration(primitiveSettings);
                _resultInfoText.Text = $"{rendered.Size.Width:N0} × {rendered.Size.Height:N0} · {plan.Primitives.Count:N0} primitives · {_workspace.Regions.Count:N0} regions";
                _statusText.Text = "Primitive preview optimized from the current detail map.";
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

    private async Task RenderHybridPreviewAsync(
        SkiaImage proxyImage,
        FlowPainterSettings flowSettings,
        PrimitiveGenerationSettings primitiveSettings,
        HybridGenerationSettings hybridSettings,
        ulong seed)
    {
        await RunRenderingPreviewAsync(async cancellationToken =>
        {
            DetailMap detailMap = await EnsureDetailMapAsync(
                proxyImage,
                flowSettings.DetailAnalysis,
                flowSettings.DetailInfluence,
                flowSettings.SemanticAnalysis,
                flowSettings.BoundaryAnalysis,
                flowSettings.BackgroundSuppression,
                cancellationToken).ConfigureAwait(true);
            StrokeDensityMap densityMap = StrokeDensityMap.CreateUniform(
                proxyImage.Size,
                flowSettings.UniformDensity);
            Progress<HybridPlanningProgress> planningProgress = new(value =>
                ReportOperationProgress(value.Fraction * 0.72d, value.Message));
            SceneBoundaryAnalysisResult boundaryAnalysis = _sceneBoundaryAnalysisResult
                ?? SceneBoundaryAnalysisResult.CreateEmpty(proxyImage.Size);
            BackgroundSuppressionResult backgroundSuppression = _backgroundSuppressionResult
                ?? BackgroundSuppressionResult.CreateDisabled(detailMap);
            HybridPlan plan = await Task.Run(
                () => flowSettings.BackgroundSuppression.Enabled
                    ? _hybridComposer.CreatePlan(
                        proxyImage,
                        densityMap,
                        backgroundSuppression,
                        boundaryAnalysis,
                        seed,
                        flowSettings,
                        primitiveSettings,
                        hybridSettings,
                        planningProgress,
                        cancellationToken)
                    : flowSettings.BoundaryPainting.Enabled
                        ? _hybridComposer.CreatePlan(
                            proxyImage,
                            densityMap,
                            detailMap,
                            boundaryAnalysis,
                            seed,
                            flowSettings,
                            primitiveSettings,
                            hybridSettings,
                            planningProgress,
                            cancellationToken)
                        : _hybridComposer.CreatePlan(
                            proxyImage,
                            densityMap,
                            detailMap,
                            seed,
                            flowSettings,
                            primitiveSettings,
                            hybridSettings,
                            planningProgress,
                            cancellationToken),
                cancellationToken).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();

            Progress<HybridRenderProgress> renderProgress = new(value =>
                ReportOperationProgress(
                    0.72d + (value.Fraction * 0.28d),
                    value.Message));
            SkiaImage rendered = await _hybridRenderer.RenderAsync(
                plan,
                proxyImage.Size,
                flowSettings.Brush,
                flowSettings.Brush,
                renderProgress,
                cancellationToken).ConfigureAwait(true);
            bool adopted = false;

            try
            {
                ReplaceRenderedHybrid(rendered, plan, flowSettings.Brush);
                adopted = true;
                _workspace.SetGenerativeMode(GenerativeMode.Hybrid);
                _workspace.SetSettings(flowSettings);
                _workspace.SetPrimitiveGeneration(primitiveSettings);
                _workspace.SetHybridGeneration(hybridSettings);
                _resultInfoText.Text = $"{rendered.Size.Width:N0} × {rendered.Size.Height:N0} · "
                    + $"{plan.PrimitivePlan.Primitives.Count:N0} primitives + "
                    + $"{plan.FlowStrokePlan.Strokes.Count:N0} flow + "
                    + $"{plan.RefinementStrokePlan.Strokes.Count:N0} refinement strokes";
                _statusText.Text = flowSettings.BackgroundSuppression.Enabled
                    ? "Hybrid preview rendered: simplified background masses, protected boundary-aware flow and focal refinement."
                    : flowSettings.BoundaryPainting.Enabled
                        ? "Hybrid preview rendered: primitive masses, boundary-aware deformed flow and detail refinement."
                        : "Hybrid preview rendered: primitive masses, deformed flow and detail refinement.";
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
        if (sourceImage is null)
        {
            _statusText.Text = "Open an image before exporting the final image.";
            return;
        }

        if (_previewGenerativeMode == GenerativeMode.GeometricPrimitives)
        {
            PrimitivePlan? primitivePlan = _previewPrimitivePlan;
            if (primitivePlan is null)
            {
                _statusText.Text = "Render a primitive preview before exporting the final image.";
                return;
            }

            await ExportPrimitiveFinalAsync(sourceImage, primitivePlan).ConfigureAwait(true);
            return;
        }

        if (_previewGenerativeMode == GenerativeMode.Hybrid)
        {
            HybridPlan? hybridPlan = _previewHybridPlan;
            BrushSettings? hybridBrush = _previewBrushSettings;
            if (hybridPlan is null || hybridBrush is null)
            {
                _statusText.Text = "Render a hybrid preview before exporting the final image.";
                return;
            }

            await ExportHybridFinalAsync(sourceImage, hybridPlan, hybridBrush).ConfigureAwait(true);
            return;
        }

        StrokePlan? plan = _previewStrokePlan;
        BrushSettings? brush = _previewBrushSettings;
        if (plan is null || brush is null)
        {
            _statusText.Text = "Render a flow preview before exporting the final image.";
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
        if (!TryEnsureFinalRenderMemoryBudget(
                sourceImage,
                outputSize,
                GenerativeMode.FlowPainting))
        {
            return;
        }

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

    private async Task ExportHybridFinalAsync(
        SkiaImage sourceImage,
        HybridPlan plan,
        BrushSettings brush)
    {
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
        if (!TryEnsureFinalRenderMemoryBudget(
                sourceImage,
                outputSize,
                GenerativeMode.Hybrid))
        {
            return;
        }

        string extension = finalSettings.DefaultFileExtension;
        string suggestedBaseName = string.IsNullOrWhiteSpace(_projectNameTextBox.Text)
            ? "flowpainter-hybrid"
            : SanitizeFileName(_projectNameTextBox.Text.Trim());
        FilePickerFileType fileType = finalSettings.Format == RasterImageFormat.Png
            ? new FilePickerFileType("PNG image") { Patterns = ["*.png"] }
            : new FilePickerFileType("JPEG image") { Patterns = ["*.jpg", "*.jpeg"] };
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export final hybrid artwork",
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
            Progress<HybridRenderProgress> renderProgress = new(value =>
                ReportOperationProgress(value.Fraction * 0.88d, value.Message));
            using SkiaImage finalImage = await _hybridRenderer.RenderAsync(
                plan,
                outputSize,
                brush,
                brush,
                renderProgress,
                cancellationToken).ConfigureAwait(true);
            Progress<ImageOperationProgress> encodeProgress = new(value =>
                ReportOperationProgress(0.88d + (value.Fraction * 0.12d), value.Message));
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await _imageEncoder.EncodeAsync(
                finalImage,
                output,
                finalSettings.Format,
                finalSettings.JpegQuality,
                encodeProgress,
                cancellationToken).ConfigureAwait(true);
            _statusText.Text = $"Exported {file.Name} at {outputSize.Width:N0} × {outputSize.Height:N0} "
                + $"from {plan.PrimitivePlan.Primitives.Count:N0} primitives and "
                + $"{plan.FlowStrokePlan.Strokes.Count + plan.RefinementStrokePlan.Strokes.Count:N0} strokes.";
        }).ConfigureAwait(true);
    }

    private async Task ExportPrimitiveFinalAsync(
        SkiaImage sourceImage,
        PrimitivePlan plan)
    {
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
        if (!TryEnsureFinalRenderMemoryBudget(
                sourceImage,
                outputSize,
                GenerativeMode.GeometricPrimitives))
        {
            return;
        }

        string extension = finalSettings.DefaultFileExtension;
        string suggestedBaseName = string.IsNullOrWhiteSpace(_projectNameTextBox.Text)
            ? "flowpainter-primitives"
            : SanitizeFileName(_projectNameTextBox.Text.Trim());
        FilePickerFileType fileType = finalSettings.Format == RasterImageFormat.Png
            ? new FilePickerFileType("PNG image") { Patterns = ["*.png"] }
            : new FilePickerFileType("JPEG image") { Patterns = ["*.jpg", "*.jpeg"] };
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export final primitive artwork",
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
            Progress<PrimitiveRenderProgress> renderProgress = new(value =>
                ReportOperationProgress(
                    value.Fraction * 0.88d,
                    value.Stage == PrimitiveRenderStage.DrawingPrimitives
                        ? $"Final rendering {value.CompletedPrimitives:N0} / {value.TotalPrimitives:N0} primitives"
                        : $"Final primitive rendering: {value.Stage}"));
            using SkiaImage finalImage = await _primitiveRenderer.RenderAsync(
                plan,
                outputSize,
                renderProgress,
                cancellationToken).ConfigureAwait(true);
            Progress<ImageOperationProgress> encodeProgress = new(value =>
                ReportOperationProgress(0.88d + (value.Fraction * 0.12d), value.Message));
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await _imageEncoder.EncodeAsync(
                finalImage,
                output,
                finalSettings.Format,
                finalSettings.JpegQuality,
                encodeProgress,
                cancellationToken).ConfigureAwait(true);
            _statusText.Text = $"Exported {file.Name} at {outputSize.Width:N0} × {outputSize.Height:N0} from {plan.Primitives.Count:N0} primitives.";
        }).ConfigureAwait(true);
    }

    private async void ExportSvgClick(object? sender, RoutedEventArgs e)
    {
        SkiaImage? sourceImage = _sourceImage;
        PrimitivePlan? plan = _previewPrimitivePlan;
        if (sourceImage is null || plan is null || _previewGenerativeMode != GenerativeMode.GeometricPrimitives)
        {
            _statusText.Text = "Render a primitive preview before exporting SVG.";
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
        string suggestedBaseName = string.IsNullOrWhiteSpace(_projectNameTextBox.Text)
            ? "flowpainter-primitives"
            : SanitizeFileName(_projectNameTextBox.Text.Trim());
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export primitive artwork as SVG",
                SuggestedFileName = $"{suggestedBaseName}.svg",
                DefaultExtension = "svg",
                FileTypeChoices =
                [
                    new FilePickerFileType("SVG vector image") { Patterns = ["*.svg"] }
                ]
            }).ConfigureAwait(true);
        if (file is null)
        {
            return;
        }

        await RunExportingImageAsync(async cancellationToken =>
        {
            ReportOperationProgress(0.1d, "Writing SVG primitives");
            await using Stream output = await file.OpenWriteAsync().ConfigureAwait(true);
            await SvgPrimitivePlanExporter.ExportAsync(
                plan,
                outputSize,
                output,
                cancellationToken).ConfigureAwait(true);
            ReportOperationProgress(1d, "SVG export completed");
            _statusText.Text = $"Exported {file.Name} with {plan.Primitives.Count:N0} vector primitives.";
        }).ConfigureAwait(true);
    }

    private async void WindowKeyDown(object? sender, KeyEventArgs e)
    {
        _ = sender;
        if (e.Key != Key.Delete
            || e.KeyModifiers != KeyModifiers.None
            || e.Source is TextBox
            || _operationCancellation is not null)
        {
            return;
        }

        if (GetSelectedRegion() is not null)
        {
            e.Handled = true;
            await DeleteSelectedDetailRegionAsync().ConfigureAwait(true);
            return;
        }

        if (GetSelectedSemanticCorrection() is not null)
        {
            e.Handled = true;
            await DeleteSelectedSemanticCorrectionAsync().ConfigureAwait(true);
        }
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
        DetailInfluenceSettings detailInfluenceSettings;
        SemanticAnalysisSettings semanticSettings;
        SceneBoundaryAnalysisSettings boundarySettings;
        BackgroundSuppressionSettings backgroundSettings;
        try
        {
            detailSettings = ReadDetailAnalysisSettings();
            detailInfluenceSettings = ReadDetailInfluenceSettings();
            semanticSettings = ReadSemanticAnalysisSettings();
            boundarySettings = ReadBoundaryAnalysisSettings();
            backgroundSettings = ReadBackgroundSuppressionSettings();
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
                detailInfluenceSettings,
                semanticSettings,
                boundarySettings,
                _workspace.SemanticCorrections.Regions,
                cancellationToken).ConfigureAwait(true);
            await ReplaceAnalyzedDetailMapsAsync(
                maps,
                detailSettings,
                detailInfluenceSettings,
                semanticSettings,
                boundarySettings,
                backgroundSettings,
                cancellationToken).ConfigureAwait(true);
            _statusText.Text = semanticSettings.Enabled
                ? $"Structural, semantic and boundary maps updated. {maps.Semantic.Regions.Count:N0} semantic regions found."
                : "Structural and boundary maps updated; semantic analysis is disabled.";
        }).ConfigureAwait(true);
    }

    private async void RemoveLastRegionClick(object? sender, RoutedEventArgs e)
    {
        if (_workspace.Regions.Count == 0 || _automaticDetailMap is null)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        string? selectedRegionId = GetSelectedRegion()?.Id;
        _workspace.RemoveLastRegion();
        bool succeeded = await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            _statusText.Text = "Last manual detail region removed.";
        }).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selectedRegionId);
        }
    }

    private async void ClearRegionsClick(object? sender, RoutedEventArgs e)
    {
        if (_workspace.Regions.Count == 0 || _automaticDetailMap is null)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        string? selectedRegionId = GetSelectedRegion()?.Id;
        _workspace.ClearRegions();
        bool succeeded = await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            _statusText.Text = "All manual detail regions cleared.";
        }).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selectedRegionId);
        }
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
        SceneBoundaryOverlayMode mode = ReadSelectedAnalysisOverlayMode();
        if (mode == SceneBoundaryOverlayMode.CombinedDetail
            || _showDetailOverlayCheckBox.IsChecked != true)
        {
            UpdateSourcePreviewSelection();
            return;
        }

        SkiaImage? proxy = _proxyImage;
        SemanticAnalysisResult? semantic = _semanticAnalysisResult;
        SceneBoundaryAnalysisResult? boundary = _sceneBoundaryAnalysisResult;
        BackgroundSuppressionResult? backgroundSuppression = _backgroundSuppressionResult;
        if (proxy is null || semantic is null || boundary is null || _operationCancellation is not null)
        {
            UpdateSourcePreviewSelection();
            return;
        }

        await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            using SkiaImage overlay = mode == SceneBoundaryOverlayMode.EdgeDirections
                ? await _boundaryDirectionOverlayRenderer.RenderAsync(
                    proxy,
                    boundary.DirectionField,
                    boundary.EdgeImportanceMap,
                    cancellationToken: cancellationToken).ConfigureAwait(true)
                : await _detailOverlayRenderer.RenderAsync(
                    proxy,
                    SelectAnalysisOverlayMap(semantic, boundary, backgroundSuppression, mode),
                    cancellationToken: cancellationToken).ConfigureAwait(true);
            ReplaceSemanticOverlayPreview(overlay);
            _statusText.Text = $"Showing {mode} overlay from {boundary.ProviderId}.";
        }).ConfigureAwait(true);
    }

    private async void SetSemanticPrimarySubjectClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await AddSelectedSemanticCorrectionAsync(
            SemanticCorrectionKind.ForcePrimarySubject,
            "primary subject").ConfigureAwait(true);
    }

    private async void SetSemanticSubjectClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await AddSelectedSemanticCorrectionAsync(
            SemanticCorrectionKind.ForceSubject,
            "subject").ConfigureAwait(true);
    }

    private async void SetSemanticBackgroundClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await AddSelectedSemanticCorrectionAsync(
            SemanticCorrectionKind.ForceBackground,
            "background").ConfigureAwait(true);
    }

    private async void IgnoreSemanticDetectionClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await AddSelectedSemanticCorrectionAsync(
            SemanticCorrectionKind.IgnoreAutomaticDetection,
            "ignored detection").ConfigureAwait(true);
    }

    private async Task AddSelectedSemanticCorrectionAsync(
        SemanticCorrectionKind kind,
        string roleLabel)
    {
        SemanticRegion? region = GetSelectedSemanticRegion();
        if (region is null || _proxyImage is null)
        {
            _statusText.Text = "Select an analyzed semantic region first.";
            return;
        }

        SemanticCorrectionRegion correction;
        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        try
        {
            correction = _workspace.AddSemanticCorrection(
                region.Bounds,
                kind,
                $"{region.Label ?? region.Id} · {roleLabel}",
                region.Id);
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return;
        }

        ClearDetailAndAutomaticSemanticSelections();
        RefreshSemanticCorrections(correction.Id);
        bool succeeded = await ReanalyzeAfterSemanticCorrectionsAsync(
            $"Applied {roleLabel} correction to '{region.Label ?? region.Id}'.",
            correction.Id).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedSemanticRegionId: region.Id);
        }
    }

    private async void DeleteSelectedSemanticCorrectionClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await DeleteSelectedSemanticCorrectionAsync().ConfigureAwait(true);
    }

    private async Task DeleteSelectedSemanticCorrectionAsync()
    {
        SemanticCorrectionRegion? selected = GetSelectedSemanticCorrection();
        if (selected is null)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        if (!_workspace.RemoveSemanticCorrection(selected.Id))
        {
            return;
        }

        bool succeeded = await ReanalyzeAfterSemanticCorrectionsAsync(
            "Selected semantic correction deleted.").ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedCorrectionId: selected.Id);
        }
    }

    private async void ClearSemanticCorrectionsClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (_workspace.SemanticCorrections.Count == 0)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        string? selectedCorrectionId = GetSelectedSemanticCorrection()?.Id;
        _workspace.ClearSemanticCorrections();
        bool succeeded = await ReanalyzeAfterSemanticCorrectionsAsync(
            "All semantic corrections cleared.").ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedCorrectionId: selectedCorrectionId);
        }
    }

    private async Task<bool> ReanalyzeAfterSemanticCorrectionsAsync(
        string completionMessage,
        string? selectedCorrectionId = null)
    {
        SkiaImage? proxy = _proxyImage;
        if (proxy is null)
        {
            RefreshSemanticCorrections(selectedCorrectionId);
            RefreshRegionVisuals();
            return false;
        }

        DetailAnalysisSettings detailSettings;
        DetailInfluenceSettings detailInfluenceSettings;
        SemanticAnalysisSettings semanticSettings;
        SceneBoundaryAnalysisSettings boundarySettings;
        BackgroundSuppressionSettings backgroundSettings;
        try
        {
            detailSettings = ReadDetailAnalysisSettings();
            detailInfluenceSettings = ReadDetailInfluenceSettings();
            semanticSettings = ReadSemanticAnalysisSettings();
            boundarySettings = ReadBoundaryAnalysisSettings();
            backgroundSettings = ReadBackgroundSuppressionSettings();
        }
        catch (FormatException exception)
        {
            _statusText.Text = exception.Message;
            return false;
        }
        catch (ArgumentException exception)
        {
            _statusText.Text = exception.Message;
            return false;
        }

        return await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
                proxy,
                detailSettings,
                detailInfluenceSettings,
                semanticSettings,
                boundarySettings,
                _workspace.SemanticCorrections.Regions,
                cancellationToken).ConfigureAwait(true);
            await ReplaceAnalyzedDetailMapsAsync(
                maps,
                detailSettings,
                detailInfluenceSettings,
                semanticSettings,
                boundarySettings,
                backgroundSettings,
                cancellationToken).ConfigureAwait(true);
            RefreshSemanticCorrections(selectedCorrectionId);
            RefreshRegionVisuals();
            _statusText.Text = completionMessage;
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
        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        DetailRegion manual = _workspace.AddRegion(
            region.Bounds,
            strength,
            DetailRegionIntent.IncreaseDetail,
            label);
        ClearSemanticSelections();
        bool succeeded = await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            RefreshRegionVisuals(manual.Id);
            _statusText.Text = $"Promoted '{region.Label ?? region.Id}' to manual {roleLabel}.";
        }).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedSemanticRegionId: region.Id);
        }
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
        _selectionPointerStartPosition = position;
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
        Point? pointerStart = _selectionPointerStartPosition;
        Point pointerEnd = e.GetPosition(_sourceViewportHost);
        _selectionStart = null;
        _selectionCurrent = null;
        _selectionPointerStartPosition = null;
        e.Pointer.Capture(null);
        e.Handled = true;

        if (start is null || end is null)
        {
            RefreshRegionVisuals();
            return;
        }

        double dragDistance;
        if (pointerStart is Point pressed)
        {
            double horizontalDistance = pointerEnd.X - pressed.X;
            double verticalDistance = pointerEnd.Y - pressed.Y;
            dragDistance = Math.Sqrt(
                (horizontalDistance * horizontalDistance)
                + (verticalDistance * verticalDistance));
        }
        else
        {
            dragDistance = double.PositiveInfinity;
        }
        if (dragDistance < RegionSelectionDragThresholdPixels)
        {
            SelectOverlayRegionAt(end.Value);
            return;
        }

        double width = Math.Abs(end.Value.X - start.Value.X);
        double height = Math.Abs(end.Value.Y - start.Value.Y);
        if (width < MinimumManualRegionSize || height < MinimumManualRegionSize)
        {
            _statusText.Text = "The dragged area is too small.";
            RefreshRegionVisuals();
            return;
        }

        DetailRegionIntent intent;
        double strength;
        DetailRegion? added = null;
        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        string? selectedRegionId = GetSelectedRegion()?.Id;
        try
        {
            intent = ReadSelectedDetailIntent();
            strength = ParseDouble(_regionStrengthTextBox, "Region strength") / 100d;
            NormalizedRect bounds = NormalizedRect.FromCorners(start.Value, end.Value);
            added = _workspace.AddRegion(
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

        ClearSemanticSelections();
        RefreshRegionVisuals(added.Id);
        bool succeeded = await RunAnalyzingDetailAsync(async cancellationToken =>
        {
            await RecomposeDetailMapAsync(cancellationToken).ConfigureAwait(true);
            _statusText.Text = "Manual detail region added. Render the preview to apply it to the strokes.";
        }).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selectedRegionId);
        }
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

    private void SelectOverlayRegionAt(NormalizedPoint point)
    {
        DetailRegion? detail = OverlayRegionHitTester.SelectDetailRegion(
            _workspace.Regions.Regions,
            point,
            GetSelectedRegion()?.Id);
        if (detail is not null)
        {
            ClearSemanticSelections();
            RefreshRegionVisuals(detail.Id);
            _statusText.Text = $"Selected manual detail region '{detail.Label ?? detail.Id}'.";
            return;
        }

        SemanticCorrectionRegion? correction = OverlayRegionHitTester.SelectSemanticCorrection(
            _workspace.SemanticCorrections.Regions,
            point,
            GetSelectedSemanticCorrection()?.Id);
        if (correction is not null)
        {
            ClearDetailAndAutomaticSemanticSelections();
            RefreshSemanticCorrections(correction.Id);
            RefreshRegionVisuals();
            _statusText.Text = $"Selected semantic correction '{correction.Label ?? correction.Id}'.";
            return;
        }

        SemanticRegion? semantic = _showDetailOverlayCheckBox.IsChecked != true
            || _semanticAnalysisResult is null
                ? null
                : OverlayRegionHitTester.SelectAutomaticSemanticRegion(
                    _semanticAnalysisResult.Regions,
                    point,
                    GetSelectedSemanticRegion()?.Id);
        if (semantic is not null)
        {
            ClearDetailAndCorrectionSelections();
            SelectSemanticRegionInList(semantic.Id);
            RefreshRegionVisuals();
            _statusText.Text = $"Selected detected region '{semantic.Label ?? semantic.Id}'.";
            return;
        }

        ClearAllOverlaySelections();
        RefreshRegionVisuals();
        _statusText.Text = "No selectable region at this point.";
    }

    private void RegionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressRegionSelectionChange)
        {
            return;
        }

        if (_regionListBox.SelectedItem is not null)
        {
            ClearSemanticSelections();
        }

        PopulateRegionEditorControls(GetSelectedRegion());
        RefreshRegionVisuals(GetSelectedRegion()?.Id);
    }

    private void SemanticRegionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSemanticRegionSelectionChange)
        {
            return;
        }

        if (_semanticRegionListBox.SelectedItem is not null)
        {
            ClearDetailAndCorrectionSelections();
        }

        RefreshRegionVisuals();
    }

    private void SemanticCorrectionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSemanticCorrectionSelectionChange)
        {
            return;
        }

        if (_semanticCorrectionListBox.SelectedItem is not null)
        {
            ClearDetailAndAutomaticSemanticSelections();
        }

        RefreshRegionVisuals();
    }

    private async void ApplyRegionChangesClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null || _automaticDetailMap is null)
        {
            _statusText.Text = "Select a manual region before applying changes.";
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
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

        bool succeeded = await RecomposeRegionsFromEditorAsync(
            "Selected detail region updated.",
            selected.Id).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selected.Id);
        }
    }

    private async void MoveRegionEarlierClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        if (!_workspace.MoveRegionEarlier(selected.Id))
        {
            return;
        }

        bool succeeded = await RecomposeRegionsFromEditorAsync(
            "Selected detail region moved earlier in the composition order.",
            selected.Id).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selected.Id);
        }
    }

    private async void MoveRegionLaterClick(object? sender, RoutedEventArgs e)
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        if (!_workspace.MoveRegionLater(selected.Id))
        {
            return;
        }

        bool succeeded = await RecomposeRegionsFromEditorAsync(
            "Selected detail region moved later in the composition order.",
            selected.Id).ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selected.Id);
        }
    }

    private async void DeleteSelectedRegionClick(object? sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await DeleteSelectedDetailRegionAsync().ConfigureAwait(true);
    }

    private async Task DeleteSelectedDetailRegionAsync()
    {
        DetailRegion? selected = GetSelectedRegion();
        if (selected is null)
        {
            return;
        }

        WorkspaceEditSnapshot snapshot = _workspace.CaptureEditState();
        if (!_workspace.RemoveRegion(selected.Id))
        {
            return;
        }

        bool succeeded = await RecomposeRegionsFromEditorAsync(
            "Selected detail region deleted.").ConfigureAwait(true);
        if (!succeeded)
        {
            RestoreWorkspaceEdit(snapshot, selectedRegionId: selected.Id);
        }
    }

    private async Task<bool> RecomposeRegionsFromEditorAsync(string completionMessage, string? selectedRegionId = null)
    {
        if (_automaticDetailMap is null)
        {
            RefreshRegionVisuals(selectedRegionId);
            return true;
        }

        return await RunAnalyzingDetailAsync(async cancellationToken =>
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
        _regionTransitionWidthTextBox.Text = FormatDouble(settings.DetailInfluence.RegionTransitionWidth * 100d);
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
        ApplyBoundaryAnalysisSettings(settings.BoundaryAnalysis);
        ApplyBoundaryPaintingSettings(settings.BoundaryPainting);
        ApplyBackgroundSuppressionSettings(settings.BackgroundSuppression);
    }

    private void ApplyProjectControls(FlowPainterProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        bool previousSuppression = _suppressDirtyTracking;
        _suppressDirtyTracking = true;
        try
        {
            _previewQualityComboBox.SelectedItem = project.Preview.Quality;
            ApplyFinalRenderSettings(project.FinalRender);
            ApplyPreset(new FlowPainterPreset(project.Name, project.Settings), project.Seed);
            ApplyPrimitiveGenerationSettings(project.PrimitiveGeneration);
            ApplyHybridGenerationSettings(project.HybridGeneration);
            _generativeModeComboBox.SelectedItem = project.Mode;
            _projectNameTextBox.Text = project.Name;
        }
        finally
        {
            _suppressDirtyTracking = previousSuppression;
        }
    }

    private void ApplyBoundaryAnalysisSettings(SceneBoundaryAnalysisSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _enableBoundaryAnalysisCheckBox.IsChecked = settings.Enabled;
        _boundaryLuminanceWeightTextBox.Text = FormatDouble(settings.LuminanceWeight);
        _boundaryColorWeightTextBox.Text = FormatDouble(settings.ColorWeight);
        _boundaryMultiscaleWeightTextBox.Text = FormatDouble(settings.MultiscaleWeight);
        _boundaryContinuityWeightTextBox.Text = FormatDouble(settings.ContinuityWeight);
        _boundarySemanticWeightTextBox.Text = FormatDouble(settings.SemanticBoundaryWeight);
        _boundaryTextureSuppressionTextBox.Text = FormatDouble(settings.TextureSuppression * 100d);
        _boundaryEdgeThresholdTextBox.Text = FormatDouble(settings.EdgeThreshold * 100d);
        _boundaryImportantThresholdTextBox.Text = FormatDouble(settings.ImportantEdgeThreshold * 100d);
        _boundaryCoarseRadiusTextBox.Text = settings.CoarseRadius.ToString(CultureInfo.CurrentCulture);
        _boundarySmoothingRadiusTextBox.Text = settings.SmoothingRadius.ToString(CultureInfo.CurrentCulture);
        _boundaryProtectionRadiusTextBox.Text = settings.BoundaryProtectionRadius.ToString(CultureInfo.CurrentCulture);
    }

    private void ApplyBoundaryPaintingSettings(BoundaryPaintingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _enableBoundaryPaintingCheckBox.IsChecked = settings.Enabled;
        _boundaryTangentAlignmentTextBox.Text = FormatDouble(settings.TangentAlignment * 100d);
        _boundaryAlignmentRadiusTextBox.Text = settings.AlignmentRadius.ToString(CultureInfo.CurrentCulture);
        _boundaryCrossingPenaltyTextBox.Text = FormatDouble(settings.CrossingPenalty * 100d);
        _boundaryHardThresholdTextBox.Text = FormatDouble(settings.HardBoundaryThreshold * 100d);
        _boundaryTerminationStrengthTextBox.Text = FormatDouble(settings.TerminationStrength * 100d);
        _boundaryInternalInfluenceTextBox.Text = FormatDouble(settings.InternalEdgeInfluence * 100d);
        _boundaryTextureInfluenceTextBox.Text = FormatDouble(settings.TextureEdgeInfluence * 100d);
        _boundaryContourReinforcementTextBox.Text = FormatDouble(settings.ContourReinforcement);
        _boundaryCornerPreservationTextBox.Text = FormatDouble(settings.CornerPreservation * 100d);
    }

    private void ApplyBackgroundSuppressionSettings(BackgroundSuppressionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _enableBackgroundSuppressionCheckBox.IsChecked = settings.Enabled;
        _backgroundSuppressionStrengthTextBox.Text = FormatDouble(settings.OverallStrength * 100d);
        _backgroundDetailFloorTextBox.Text = FormatDouble(settings.DetailFloor * 100d);
        _backgroundUncertaintyProtectionTextBox.Text = FormatDouble(settings.UncertaintyProtection * 100d);
        _backgroundSilhouetteProtectionTextBox.Text = FormatDouble(settings.SilhouetteProtection * 100d);
        _backgroundTransitionSoftnessTextBox.Text = FormatDouble(settings.TransitionSoftness * 100d);
        _backgroundPlacementWeightTextBox.Text = FormatDouble(settings.BackgroundPlacementWeight * 100d);
        _backgroundSuppressionLengthTextBox.Text = FormatDouble(settings.StrokeLengthMultiplier * 100d);
        _backgroundSuppressionWidthTextBox.Text = FormatDouble(settings.StrokeWidthMultiplier * 100d);
        _backgroundSegmentMultiplierTextBox.Text = FormatDouble(settings.SegmentMultiplier * 100d);
        _backgroundCurveFreedomTextBox.Text = FormatDouble(settings.CurveFreedomMultiplier * 100d);
        _backgroundColorSimplificationTextBox.Text = FormatDouble(settings.ColorSimplification * 100d);
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

    private static void EnsureAnalysisMemoryBudget(
        ImageSize sourceSize,
        int previewMaximumDimension)
    {
        ImageSize proxySize = sourceSize.FitWithin(
            previewMaximumDimension,
            previewMaximumDimension);
        AnalysisMemoryEstimate estimate = AnalysisMemoryEstimator.Estimate(sourceSize, proxySize);
        WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
            estimate.KnownPeakBytes,
            "Image analysis");
    }

    private bool TryEnsureFinalRenderMemoryBudget(
        SkiaImage sourceImage,
        ImageSize outputSize,
        GenerativeMode mode)
    {
        try
        {
            ImageSize proxySize = _proxyImage?.Size
                ?? sourceImage.Size.FitWithin(
                    PreviewSettings.StandardMaximumDimension,
                    PreviewSettings.StandardMaximumDimension);
            ImageSize previewSize = _renderedImage?.Size ?? proxySize;
            FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
                sourceImage.Size,
                proxySize,
                previewSize,
                outputSize,
                mode,
                includeDetailOverlay: _detailOverlayPreviewBitmap is not null);
            WorkloadBudgetPolicy.EnsureMemoryWithinBudget(
                estimate.KnownPeakBytes,
                $"Final {mode} export");
            return true;
        }
        catch (InvalidOperationException exception)
        {
            _statusText.Text = exception.Message;
            return false;
        }
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
            bool hasApprovedPreviewPlan = _previewStrokePlan is not null
                || _previewPrimitivePlan is not null
                || _previewHybridPlan is not null;
            GenerativeMode mode = hasApprovedPreviewPlan
                ? _previewGenerativeMode
                : _generativeModeComboBox.SelectedItem is GenerativeMode selectedMode
                    ? selectedMode
                    : GenerativeMode.FlowPainting;
            FinalRenderMemoryEstimate estimate = FinalRenderMemoryEstimator.Estimate(
                source.Size,
                proxySize,
                previewSize,
                outputSize,
                mode,
                includeDetailOverlay: _detailOverlayPreviewBitmap is not null);
            bool withinBudget = WorkloadBudgetPolicy.IsMemoryWithinBudget(estimate.KnownPeakBytes);
            string budgetStatus = withinBudget ? "allowed" : "blocked";
            _finalOutputInfoText.Text = $"{outputSize.Width:N0} × {outputSize.Height:N0} · {settings.Format} · {mode}";
            _finalMemoryInfoText.Text = $"Estimated peak: {estimate.KnownPeakMebibytes:N0} MiB · "
                + $"{estimate.OutputBufferCount:N0} output buffers · {estimate.Risk} risk · {budgetStatus}";
            if (showValidationMessage)
            {
                _statusText.Text = withinBudget
                    ? $"Final output estimate updated: {estimate.KnownPeakMebibytes:N0} MiB estimated peak."
                    : $"Final export blocked: {estimate.KnownPeakMebibytes:N0} MiB exceeds the "
                        + $"{WorkloadBudgetPolicy.MaximumPeakWorkingSetMebibytes:N0} MiB budget.";
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

    private void GenerativeModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        GenerativeMode mode = _generativeModeComboBox.SelectedItem is GenerativeMode selected
            ? selected
            : GenerativeMode.FlowPainting;
        _exportSvgButton.IsEnabled = !_workspace.Operation.IsBusy
            && mode == GenerativeMode.GeometricPrimitives
            && _previewPrimitivePlan is not null;
        InvalidateRenderedPreview();
        _statusText.Text = mode switch
        {
            GenerativeMode.GeometricPrimitives => "Geometric primitive mode selected.",
            GenerativeMode.Hybrid => "Hybrid mode selected: primitive masses deform the flow field before refinement.",
            _ => "Flow painting mode selected."
        };
    }

    private static int CountEnabledPrimitiveKinds(PrimitiveKindSet value)
    {
        return System.Numerics.BitOperations.PopCount((uint)value);
    }

    private GenerativeMode ReadSelectedGenerativeMode()
    {
        return _generativeModeComboBox.SelectedItem is GenerativeMode mode
            ? mode
            : throw new InvalidOperationException("Select a generative engine.");
    }

    private PrimitiveGenerationSettings ReadPrimitiveGenerationSettings()
    {
        PrimitiveKindSet allowedKinds = _primitiveKindsComboBox.SelectedItem is PrimitiveKindSet selectedKinds
            ? selectedKinds
            : throw new InvalidOperationException("Select the primitive shapes to use.");
        return new PrimitiveGenerationSettings(
            ParseInteger(_primitiveCountTextBox, "Primitives"),
            ParseInteger(_primitiveCandidatesTextBox, "Primitive candidates"),
            ParseInteger(_primitiveMutationsTextBox, "Primitive mutations"),
            ParseDouble(_primitiveMinimumSizeTextBox, "Minimum primitive size") / 100d,
            ParseDouble(_primitiveMaximumSizeTextBox, "Maximum primitive size") / 100d,
            ParseDouble(_primitiveOpacityTextBox, "Primitive opacity") / 100d,
            ParseDouble(_primitiveDetailSizeTextBox, "Primitive detail size influence") / 100d,
            ParseDouble(_primitivePlacementBiasTextBox, "Primitive placement bias"),
            ParseDouble(_primitiveErrorWeightTextBox, "Primitive detail error weight"),
            ParseDouble(_primitiveSearchInfluenceTextBox, "Primitive detail search influence"),
            allowedKinds);
    }

    private void ApplyPrimitiveGenerationSettings(PrimitiveGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _primitiveCountTextBox.Text = settings.PrimitiveCount.ToString(CultureInfo.InvariantCulture);
        _primitiveCandidatesTextBox.Text = settings.CandidatesPerStep.ToString(CultureInfo.InvariantCulture);
        _primitiveMutationsTextBox.Text = settings.MutationIterations.ToString(CultureInfo.InvariantCulture);
        _primitiveMinimumSizeTextBox.Text = (settings.MinimumSize * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _primitiveMaximumSizeTextBox.Text = (settings.MaximumSize * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _primitiveOpacityTextBox.Text = (settings.Opacity * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _primitiveDetailSizeTextBox.Text = (settings.DetailSizeInfluence * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _primitivePlacementBiasTextBox.Text = settings.DetailPlacementBias.ToString("0.###", CultureInfo.InvariantCulture);
        _primitiveErrorWeightTextBox.Text = settings.DetailErrorWeight.ToString("0.###", CultureInfo.InvariantCulture);
        _primitiveSearchInfluenceTextBox.Text = settings.DetailSearchInfluence.ToString("0.###", CultureInfo.InvariantCulture);
        _primitiveKindsComboBox.SelectedItem = settings.AllowedKinds;
        if (_primitiveKindsComboBox.SelectedIndex < 0)
        {
            _primitiveKindsComboBox.SelectedItem = PrimitiveKindSet.All;
        }
    }

    private HybridGenerationSettings ReadHybridGenerationSettings()
    {
        PrimitiveFlowInfluenceKind influenceKind = _hybridInfluenceKindComboBox.SelectedItem is PrimitiveFlowInfluenceKind selected
            ? selected
            : throw new InvalidOperationException("Select a primitive flow influence.");
        return new HybridGenerationSettings(
            ParseDouble(_hybridPrimitiveBudgetTextBox, "Hybrid primitive budget") / 100d,
            ParseDouble(_hybridFlowBudgetTextBox, "Hybrid flow budget") / 100d,
            ParseDouble(_hybridRefinementBudgetTextBox, "Hybrid refinement budget") / 100d,
            influenceKind,
            ParseDouble(_hybridInfluenceStrengthTextBox, "Hybrid flow influence") / 100d,
            ParseDouble(_hybridInfluenceRadiusTextBox, "Hybrid influence radius"),
            ParseInteger(_hybridMaximumInfluencesTextBox, "Maximum nearby primitives"),
            ParseDouble(_hybridRefinementDetailBiasTextBox, "Refinement detail bias"),
            ParseDouble(_hybridRefinementLengthTextBox, "Refinement length") / 100d,
            ParseDouble(_hybridRefinementWidthTextBox, "Refinement width") / 100d);
    }

    private void ApplyHybridGenerationSettings(HybridGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _hybridPrimitiveBudgetTextBox.Text = (settings.PrimitiveBudgetFraction * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _hybridFlowBudgetTextBox.Text = (settings.FlowBudgetFraction * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _hybridRefinementBudgetTextBox.Text = (settings.RefinementBudgetFraction * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _hybridInfluenceKindComboBox.SelectedItem = settings.InfluenceKind;
        _hybridInfluenceStrengthTextBox.Text = (settings.InfluenceStrength * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _hybridInfluenceRadiusTextBox.Text = settings.InfluenceRadiusMultiplier.ToString("0.###", CultureInfo.InvariantCulture);
        _hybridMaximumInfluencesTextBox.Text = settings.MaximumInfluencesPerSample.ToString(CultureInfo.InvariantCulture);
        _hybridRefinementDetailBiasTextBox.Text = settings.RefinementDetailBias.ToString("0.###", CultureInfo.InvariantCulture);
        _hybridRefinementLengthTextBox.Text = (settings.RefinementLengthMultiplier * 100d).ToString("0.###", CultureInfo.InvariantCulture);
        _hybridRefinementWidthTextBox.Text = (settings.RefinementWidthMultiplier * 100d).ToString("0.###", CultureInfo.InvariantCulture);
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
            ReadSemanticAnalysisSettings(),
            ReadBoundaryAnalysisSettings(),
            ReadBoundaryPaintingSettings(),
            ReadBackgroundSuppressionSettings());
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
            ParseDouble(_backgroundWidthTextBox, "Background width") / 100d,
            ParseDouble(_regionTransitionWidthTextBox, "Region transition width") / 100d);
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

    private SceneBoundaryAnalysisSettings ReadBoundaryAnalysisSettings()
    {
        return new SceneBoundaryAnalysisSettings(
            _enableBoundaryAnalysisCheckBox.IsChecked == true,
            ParseDouble(_boundaryLuminanceWeightTextBox, "Boundary luminance weight"),
            ParseDouble(_boundaryColorWeightTextBox, "Boundary colour weight"),
            ParseDouble(_boundaryMultiscaleWeightTextBox, "Boundary multiscale weight"),
            ParseDouble(_boundaryContinuityWeightTextBox, "Boundary continuity weight"),
            ParseDouble(_boundarySemanticWeightTextBox, "Boundary silhouette weight"),
            ParseDouble(_boundaryTextureSuppressionTextBox, "Texture suppression") / 100d,
            ParseDouble(_boundaryEdgeThresholdTextBox, "Edge threshold") / 100d,
            ParseDouble(_boundaryImportantThresholdTextBox, "Important-edge threshold") / 100d,
            ParseInteger(_boundaryCoarseRadiusTextBox, "Boundary coarse radius"),
            ParseInteger(_boundarySmoothingRadiusTextBox, "Boundary smoothing"),
            ParseInteger(_boundaryProtectionRadiusTextBox, "Silhouette protection radius"));
    }

    private BoundaryPaintingSettings ReadBoundaryPaintingSettings()
    {
        return new BoundaryPaintingSettings(
            _enableBoundaryPaintingCheckBox.IsChecked == true,
            ParseDouble(_boundaryTangentAlignmentTextBox, "Tangent alignment") / 100d,
            ParseInteger(_boundaryAlignmentRadiusTextBox, "Boundary alignment radius"),
            ParseDouble(_boundaryCrossingPenaltyTextBox, "Boundary crossing penalty") / 100d,
            ParseDouble(_boundaryHardThresholdTextBox, "Hard boundary threshold") / 100d,
            ParseDouble(_boundaryTerminationStrengthTextBox, "Boundary termination strength") / 100d,
            ParseDouble(_boundaryInternalInfluenceTextBox, "Internal edge influence") / 100d,
            ParseDouble(_boundaryTextureInfluenceTextBox, "Texture edge influence") / 100d,
            ParseDouble(_boundaryContourReinforcementTextBox, "Contour reinforcement"),
            ParseDouble(_boundaryCornerPreservationTextBox, "Corner preservation") / 100d);
    }

    private BackgroundSuppressionSettings ReadBackgroundSuppressionSettings()
    {
        return new BackgroundSuppressionSettings(
            _enableBackgroundSuppressionCheckBox.IsChecked == true,
            ParseDouble(_backgroundSuppressionStrengthTextBox, "Background suppression strength") / 100d,
            ParseDouble(_backgroundDetailFloorTextBox, "Background detail floor") / 100d,
            ParseDouble(_backgroundUncertaintyProtectionTextBox, "Background uncertainty protection") / 100d,
            ParseDouble(_backgroundSilhouetteProtectionTextBox, "Background silhouette protection") / 100d,
            ParseDouble(_backgroundTransitionSoftnessTextBox, "Background transition softness") / 100d,
            ParseDouble(_backgroundPlacementWeightTextBox, "Background placement floor") / 100d,
            ParseDouble(_backgroundSuppressionLengthTextBox, "Background stroke length") / 100d,
            ParseDouble(_backgroundSuppressionWidthTextBox, "Background stroke width") / 100d,
            ParseDouble(_backgroundSegmentMultiplierTextBox, "Background retained segments") / 100d,
            ParseDouble(_backgroundCurveFreedomTextBox, "Background curve freedom") / 100d,
            ParseDouble(_backgroundColorSimplificationTextBox, "Background colour simplification") / 100d);
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
            ReportOperationProgress(value.Fraction * 0.32d, message);
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
            ReportOperationProgress(0.34d + (value.Fraction * 0.30d), message);
        });

        return await _semanticAnalyzer.AnalyzeAsync(
            proxy,
            settings,
            progress,
            cancellationToken).ConfigureAwait(true);
    }

    private async Task<SceneBoundaryAnalysisResult> AnalyzeSceneBoundariesAsync(
        SkiaImage proxy,
        SemanticAnalysisResult semanticAnalysis,
        SceneBoundaryAnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        Progress<SceneBoundaryAnalysisProgress> progress = new(value =>
        {
            string message = value.Stage switch
            {
                SceneBoundaryAnalysisStage.Preparing => "Preparing scene-boundary analysis...",
                SceneBoundaryAnalysisStage.ComputingMultiscaleEdges => $"Computing multiscale edges {value.CompletedRows:N0} / {value.TotalRows:N0}",
                SceneBoundaryAnalysisStage.LinkingContours => $"Linking continuous contours {value.CompletedRows:N0} / {value.TotalRows:N0}",
                SceneBoundaryAnalysisStage.ClassifyingBoundaries => $"Classifying important boundaries {value.CompletedRows:N0} / {value.TotalRows:N0}",
                SceneBoundaryAnalysisStage.EstimatingBackground => $"Estimating background confidence {value.CompletedRows:N0} / {value.TotalRows:N0}",
                SceneBoundaryAnalysisStage.SmoothingMaps => "Smoothing boundary maps...",
                SceneBoundaryAnalysisStage.Completed => "Scene-boundary analysis completed.",
                _ => "Analyzing scene boundaries..."
            };
            ReportOperationProgress(0.64d + (value.Fraction * 0.32d), message);
        });

        return await _boundaryAnalyzer.AnalyzeAsync(
            proxy,
            semanticAnalysis,
            settings,
            progress,
            cancellationToken).ConfigureAwait(true);
    }

    private async Task<AutomaticAnalysisMaps> AnalyzeAutomaticMapsAsync(
        SkiaImage proxy,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        IReadOnlyList<SemanticCorrectionRegion> semanticCorrections,
        CancellationToken cancellationToken)
    {
        DetailMap structural = await AnalyzeDetailMapAsync(
            proxy,
            detailSettings,
            cancellationToken).ConfigureAwait(true);
        SemanticAnalysisResult automaticSemantic = await AnalyzeSemanticImportanceAsync(
            proxy,
            semanticSettings,
            cancellationToken).ConfigureAwait(true);
        SemanticAnalysisResult semantic = SemanticCorrectionComposer.Apply(
            automaticSemantic,
            semanticCorrections,
            detailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);
        SceneBoundaryAnalysisResult boundary = await AnalyzeSceneBoundariesAsync(
            proxy,
            semantic,
            boundarySettings,
            cancellationToken).ConfigureAwait(true);
        DetailMap automatic = SemanticDetailMapComposer.Combine(
            structural,
            semantic,
            semanticSettings,
            cancellationToken);
        ReportOperationProgress(0.97d, "Automatic structural, semantic and boundary maps completed.");
        return new AutomaticAnalysisMaps(structural, semantic, boundary, automatic);
    }

    private BackgroundSuppressionResult ComposeBackgroundSuppression(
        AutomaticAnalysisMaps maps,
        DetailMap manuallyComposedMap,
        BackgroundSuppressionSettings settings,
        CancellationToken cancellationToken)
    {
        Progress<BackgroundSuppressionProgress> progress = new(value =>
        {
            string message = value.Stage switch
            {
                BackgroundSuppressionStage.Preparing => "Preparing background suppression...",
                BackgroundSuppressionStage.BuildingProtection => "Protecting subjects, silhouettes and uncertain areas...",
                BackgroundSuppressionStage.EstimatingSuppression => "Estimating low-importance background...",
                BackgroundSuppressionStage.SmoothingTransitions => "Smoothing subject-background transitions...",
                BackgroundSuppressionStage.CombiningDetail => "Building the artistic detail field...",
                BackgroundSuppressionStage.Completed => "Background suppression completed.",
                _ => "Composing background suppression..."
            };
            ReportOperationProgress(0.97d + (value.Fraction * 0.025d), message);
        });

        return BackgroundSuppressionComposer.Compose(
            maps.Automatic,
            manuallyComposedMap,
            maps.Semantic,
            maps.Boundary,
            settings,
            progress,
            cancellationToken);
    }

    private async Task<DetailMap> EnsureDetailMapAsync(
        SkiaImage proxy,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        BackgroundSuppressionSettings backgroundSettings,
        CancellationToken cancellationToken)
    {
        if (_automaticDetailMap is not null
            && _composedDetailMap is not null
            && DetailAnalysisSettingsEqual(_activeDetailAnalysisSettings, detailSettings)
            && _activeDetailRegionTransitionWidth == detailInfluenceSettings.RegionTransitionWidth
            && _activeDetailRegionRevision == _workspace.DetailRegionRevision
            && _activeSemanticCorrectionRevision == _workspace.SemanticCorrectionRevision
            && SemanticAnalysisSettingsEqual(_activeSemanticAnalysisSettings, semanticSettings)
            && BoundaryAnalysisSettingsEqual(_activeBoundaryAnalysisSettings, boundarySettings)
            && BackgroundSuppressionSettingsEqual(_activeBackgroundSuppressionSettings, backgroundSettings))
        {
            return _composedDetailMap;
        }

        AutomaticAnalysisMaps maps = await AnalyzeAutomaticMapsAsync(
            proxy,
            detailSettings,
            detailInfluenceSettings,
            semanticSettings,
            boundarySettings,
            _workspace.SemanticCorrections.Regions,
            cancellationToken).ConfigureAwait(true);
        await ReplaceAnalyzedDetailMapsAsync(
            maps,
            detailSettings,
            detailInfluenceSettings,
            semanticSettings,
            boundarySettings,
            backgroundSettings,
            cancellationToken).ConfigureAwait(true);
        return _composedDetailMap
            ?? throw new InvalidOperationException("The composed detail map was not created.");
    }

    private async Task ReplaceAnalyzedDetailMapsAsync(
        AutomaticAnalysisMaps maps,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        BackgroundSuppressionSettings backgroundSettings,
        CancellationToken cancellationToken)
    {
        DetailMap manuallyComposed = DetailMapComposer.ApplyRegions(
            maps.Automatic,
            _workspace.Regions.Regions,
            detailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);
        BackgroundSuppressionResult backgroundSuppression = ComposeBackgroundSuppression(
            maps,
            manuallyComposed,
            backgroundSettings,
            cancellationToken);
        SkiaImage proxy = _proxyImage
            ?? throw new InvalidOperationException("The analysis proxy is not available.");
        using SkiaImage overlay = await _detailOverlayRenderer.RenderAsync(
            proxy,
            backgroundSuppression.EffectiveDetailMap,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        ReplaceDetailVisualization(backgroundSuppression.EffectiveDetailMap, overlay);
        _automaticDetailMap = maps.Automatic;
        _semanticAnalysisResult = maps.Semantic;
        _sceneBoundaryAnalysisResult = maps.Boundary;
        _backgroundSuppressionResult = backgroundSuppression;
        _activeDetailAnalysisSettings = detailSettings;
        _activeDetailRegionTransitionWidth = detailInfluenceSettings.RegionTransitionWidth;
        CaptureActiveWorkspaceRevisions();
        _activeSemanticAnalysisSettings = semanticSettings;
        _activeBoundaryAnalysisSettings = boundarySettings;
        _activeBackgroundSuppressionSettings = backgroundSettings;
        ClearSemanticOverlayPreview();
        RefreshSemanticRegions();
    }

    private async Task RecomposeDetailMapAsync(CancellationToken cancellationToken)
    {
        DetailMap automatic = _automaticDetailMap
            ?? throw new InvalidOperationException("The automatic detail map is not available.");
        DetailInfluenceSettings detailInfluenceSettings = ReadDetailInfluenceSettings();
        if (_workspace.SemanticCorrections.Count > 0
            && _activeDetailRegionTransitionWidth != detailInfluenceSettings.RegionTransitionWidth)
        {
            DetailAnalysisSettings detailSettings = ReadDetailAnalysisSettings();
            SemanticAnalysisSettings semanticSettings = ReadSemanticAnalysisSettings();
            SceneBoundaryAnalysisSettings boundarySettings = ReadBoundaryAnalysisSettings();
            BackgroundSuppressionSettings backgroundSettings = ReadBackgroundSuppressionSettings();
            SkiaImage currentProxy = _proxyImage
                ?? throw new InvalidOperationException("The analysis proxy is not available.");
            AutomaticAnalysisMaps reanalyzedMaps = await AnalyzeAutomaticMapsAsync(
                currentProxy,
                detailSettings,
                detailInfluenceSettings,
                semanticSettings,
                boundarySettings,
                _workspace.SemanticCorrections.Regions,
                cancellationToken).ConfigureAwait(true);
            await ReplaceAnalyzedDetailMapsAsync(
                reanalyzedMaps,
                detailSettings,
                detailInfluenceSettings,
                semanticSettings,
                boundarySettings,
                backgroundSettings,
                cancellationToken).ConfigureAwait(true);
            return;
        }

        DetailMap manuallyComposed = DetailMapComposer.ApplyRegions(
            automatic,
            _workspace.Regions.Regions,
            detailInfluenceSettings.RegionTransitionWidth,
            cancellationToken);
        SemanticAnalysisResult semantic = _semanticAnalysisResult
            ?? throw new InvalidOperationException("The semantic analysis is not available.");
        SceneBoundaryAnalysisResult boundary = _sceneBoundaryAnalysisResult
            ?? throw new InvalidOperationException("The boundary analysis is not available.");
        BackgroundSuppressionSettings settings = ReadBackgroundSuppressionSettings();
        AutomaticAnalysisMaps maps = new(automatic, semantic, boundary, automatic);
        BackgroundSuppressionResult backgroundSuppression = ComposeBackgroundSuppression(
            maps,
            manuallyComposed,
            settings,
            cancellationToken);
        SkiaImage proxy = _proxyImage
            ?? throw new InvalidOperationException("The analysis proxy is not available.");
        using SkiaImage overlay = await _detailOverlayRenderer.RenderAsync(
            proxy,
            backgroundSuppression.EffectiveDetailMap,
            cancellationToken: cancellationToken).ConfigureAwait(true);
        _backgroundSuppressionResult = backgroundSuppression;
        _activeDetailRegionTransitionWidth = detailInfluenceSettings.RegionTransitionWidth;
        _activeBackgroundSuppressionSettings = settings;
        CaptureActiveWorkspaceRevisions();
        ReplaceDetailVisualization(backgroundSuppression.EffectiveDetailMap, overlay);
    }

    private Task<bool> RunLoadingImageAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.LoadingImage, "Loading image...", operation);
    }

    private Task<bool> RunLoadingProjectAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.LoadingProject, "Loading project...", operation);
    }

    private Task<bool> RunLoadingPresetAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.LoadingPreset, "Loading preset...", operation);
    }

    private Task<bool> RunRebuildingPreviewAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.RebuildingPreview, "Rebuilding preview...", operation);
    }

    private Task<bool> RunAnalyzingDetailAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.AnalyzingDetail, "Updating detail map...", operation);
    }

    private Task<bool> RunRenderingPreviewAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.RenderingPreview, "Rendering preview...", operation);
    }

    private Task<bool> RunSavingProjectAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.SavingProject, "Saving project...", operation);
    }

    private Task<bool> RunSavingPresetAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.SavingPreset, "Saving preset...", operation);
    }

    private Task<bool> RunExportingImageAsync(Func<CancellationToken, Task> operation)
    {
        return RunOperationAsync(WorkspaceOperationKind.ExportingImage, "Exporting image...", operation);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The desktop UI boundary converts unexpected operation failures into structured validation and a visible status message.")]
    private async Task<bool> RunOperationAsync(
        WorkspaceOperationKind kind,
        string initialMessage,
        Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (_operationCancellation is not null || _isClosed)
        {
            return false;
        }

        using CancellationTokenSource cancellation = new();
        _operationCancellation = cancellation;
        _workspace.BeginOperation(kind, initialMessage);
        _workspace.ClearValidation();
        _statusText.Text = initialMessage;
        SetOperationState(true);
        bool succeeded = false;

        try
        {
            await operation(cancellation.Token).ConfigureAwait(true);
            string completionMessage = string.IsNullOrWhiteSpace(_statusText.Text)
                ? "Operation completed."
                : _statusText.Text;
            _workspace.CompleteOperation(completionMessage);
            succeeded = true;
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

        return succeeded;
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
        _exportFinalButton.IsEnabled = !running
            && _sourceImage is not null
            && (_previewStrokePlan is not null || _previewPrimitivePlan is not null || _previewHybridPlan is not null);
        _exportSvgButton.IsEnabled = !running
            && _previewGenerativeMode == GenerativeMode.GeometricPrimitives
            && _previewPrimitivePlan is not null;
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
        SceneBoundaryAnalysisResult boundaryAnalysis,
        BackgroundSuppressionResult backgroundSuppression,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        BackgroundSuppressionSettings backgroundSettings,
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
            _previewPrimitivePlan = null;
            _previewHybridPlan = null;
            _previewBrushSettings = null;
            _sourcePreviewBitmap = preview;
            _detailOverlayPreviewBitmap = overlayPreview;
            _semanticOverlayPreviewBitmap = null;
            _resultPreviewBitmap = null;
            _automaticDetailMap = automaticDetailMap;
            _composedDetailMap = composedDetailMap;
            _semanticAnalysisResult = semanticAnalysis;
            _sceneBoundaryAnalysisResult = boundaryAnalysis;
            _backgroundSuppressionResult = backgroundSuppression;
            _activeDetailAnalysisSettings = detailSettings;
            _activeDetailRegionTransitionWidth = detailInfluenceSettings.RegionTransitionWidth;
            _activeDetailRegionRevision = -1L;
            _activeSemanticCorrectionRevision = -1L;
            _activeSemanticAnalysisSettings = semanticSettings;
            _activeBoundaryAnalysisSettings = boundarySettings;
            _activeBackgroundSuppressionSettings = backgroundSettings;
            _workspace.ClearRegions();
            _workspace.ClearSemanticCorrections();
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
        SceneBoundaryAnalysisResult boundaryAnalysis,
        BackgroundSuppressionResult backgroundSuppression,
        DetailAnalysisSettings detailSettings,
        DetailInfluenceSettings detailInfluenceSettings,
        SemanticAnalysisSettings semanticSettings,
        SceneBoundaryAnalysisSettings boundarySettings,
        BackgroundSuppressionSettings backgroundSettings,
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
            _previewPrimitivePlan = null;
            _previewHybridPlan = null;
            _previewBrushSettings = null;
            _sourcePreviewBitmap = preview;
            _detailOverlayPreviewBitmap = overlayPreview;
            _semanticOverlayPreviewBitmap = null;
            _resultPreviewBitmap = null;
            _automaticDetailMap = automaticDetailMap;
            _composedDetailMap = composedDetailMap;
            _semanticAnalysisResult = semanticAnalysis;
            _sceneBoundaryAnalysisResult = boundaryAnalysis;
            _backgroundSuppressionResult = backgroundSuppression;
            _activeDetailAnalysisSettings = detailSettings;
            _activeDetailRegionTransitionWidth = detailInfluenceSettings.RegionTransitionWidth;
            CaptureActiveWorkspaceRevisions();
            _activeSemanticAnalysisSettings = semanticSettings;
            _activeBoundaryAnalysisSettings = boundarySettings;
            _activeBackgroundSuppressionSettings = backgroundSettings;

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
            _previewPrimitivePlan = null;
            _previewHybridPlan = null;
            _previewBrushSettings = brush;
            _previewGenerativeMode = GenerativeMode.FlowPainting;
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

    private void ReplaceRenderedPrimitive(SkiaImage rendered, PrimitivePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Bitmap preview = CreateAvaloniaBitmap(rendered);
        SkiaImage? previousRendered = _renderedImage;
        Bitmap? previousPreview = _resultPreviewBitmap;
        bool adopted = false;

        try
        {
            adopted = true;
            _renderedImage = rendered;
            _previewStrokePlan = null;
            _previewPrimitivePlan = plan;
            _previewHybridPlan = null;
            _previewBrushSettings = null;
            _previewGenerativeMode = GenerativeMode.GeometricPrimitives;
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

    private void ReplaceRenderedHybrid(SkiaImage rendered, HybridPlan plan, BrushSettings brush)
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
            _previewStrokePlan = null;
            _previewPrimitivePlan = null;
            _previewHybridPlan = plan;
            _previewBrushSettings = brush;
            _previewGenerativeMode = GenerativeMode.Hybrid;
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
        _previewPrimitivePlan = null;
        _previewHybridPlan = null;
        _previewBrushSettings = null;
        _resultPreviewBitmap = null;
        _resultImageView.Source = null;
        _resultInfoText.Text = "Not rendered";
        _saveButton.IsEnabled = false;
        _exportFinalButton.IsEnabled = false;
        _exportSvgButton.IsEnabled = false;
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

        SceneBoundaryOverlayMode mode = ReadSelectedAnalysisOverlayMode();
        _sourceImageView.Source = mode == SceneBoundaryOverlayMode.CombinedDetail
            ? _detailOverlayPreviewBitmap ?? _sourcePreviewBitmap
            : _semanticOverlayPreviewBitmap ?? _detailOverlayPreviewBitmap ?? _sourcePreviewBitmap;
    }

    private void RefreshRegionVisuals(string? selectedRegionId = null)
    {
        selectedRegionId ??= GetSelectedRegion()?.Id;
        string? selectedSemanticRegionId = GetSelectedSemanticRegion()?.Id;
        string? selectedCorrectionId = GetSelectedSemanticCorrection()?.Id;
        _selectionCanvas.Children.Clear();
        UniformImageViewport? viewport = CreateSourceViewport();
        if (viewport is not null)
        {
            if (_showDetailOverlayCheckBox.IsChecked == true
                && _semanticAnalysisResult is not null)
            {
                foreach (SemanticRegion semanticRegion in _semanticAnalysisResult.Regions)
                {
                    AddSemanticRegionRectangle(
                        viewport,
                        semanticRegion,
                        string.Equals(
                            semanticRegion.Id,
                            selectedSemanticRegionId,
                            StringComparison.OrdinalIgnoreCase));
                }
            }

            foreach (SemanticCorrectionRegion correction in _workspace.SemanticCorrections.Regions)
            {
                AddSemanticCorrectionRectangle(
                    viewport,
                    correction,
                    string.Equals(
                        correction.Id,
                        selectedCorrectionId,
                        StringComparison.OrdinalIgnoreCase));
            }

            foreach (DetailRegion region in _workspace.Regions.Regions)
            {
                AddRegionRectangle(
                    viewport,
                    region.Bounds,
                    region.Intent,
                    string.Equals(
                        region.Id,
                        selectedRegionId,
                        StringComparison.OrdinalIgnoreCase));
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
        RefreshSemanticCorrections(selectedCorrectionId);
        RefreshDirtyIndicator();
    }

    private void RefreshSemanticRegions(string? selectedRegionId = null)
    {
        selectedRegionId ??= GetSelectedSemanticRegion()?.Id;
        SemanticRegion[] regions = _semanticAnalysisResult?.Regions.ToArray() ?? [];
        SemanticRegionDisplayItem[] items = regions
            .Select(region => new SemanticRegionDisplayItem(
                region.Id,
                $"{region.Label ?? region.Id} · {region.Role} · {region.Confidence:P0}"))
            .ToArray();
        int selectedIndex = selectedRegionId is null
            ? -1
            : Array.FindIndex(items, item => string.Equals(
                item.Id,
                selectedRegionId,
                StringComparison.OrdinalIgnoreCase));

        _suppressSemanticRegionSelectionChange = true;
        try
        {
            _semanticRegionListBox.ItemsSource = items;
            _semanticRegionListBox.SelectedIndex = selectedIndex;
        }
        finally
        {
            _suppressSemanticRegionSelectionChange = false;
        }

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

    private SemanticCorrectionRegion? GetSelectedSemanticCorrection()
    {
        if (_semanticCorrectionListBox.SelectedItem is not SemanticCorrectionDisplayItem item)
        {
            return null;
        }

        return _workspace.SemanticCorrections.Regions.FirstOrDefault(correction => string.Equals(
            correction.Id,
            item.Id,
            StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshSemanticCorrections(string? selectedCorrectionId = null)
    {
        selectedCorrectionId ??= GetSelectedSemanticCorrection()?.Id;
        SemanticCorrectionDisplayItem[] items = _workspace.SemanticCorrections.Regions
            .Select(correction => new SemanticCorrectionDisplayItem(
                correction.Id,
                $"{correction.Label ?? correction.Id} · {correction.Kind}"))
            .ToArray();
        int selectedIndex = selectedCorrectionId is null
            ? -1
            : Array.FindIndex(items, item => string.Equals(
                item.Id,
                selectedCorrectionId,
                StringComparison.OrdinalIgnoreCase));

        _suppressSemanticCorrectionSelectionChange = true;
        try
        {
            _semanticCorrectionListBox.ItemsSource = items;
            _semanticCorrectionListBox.SelectedIndex = selectedIndex;
        }
        finally
        {
            _suppressSemanticCorrectionSelectionChange = false;
        }

        _semanticCorrectionCountText.Text = items.Length == 1
            ? "1 semantic correction"
            : $"{items.Length:N0} semantic corrections";
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

    private void SelectSemanticRegionInList(string id)
    {
        if (_semanticRegionListBox.ItemsSource is not IEnumerable<SemanticRegionDisplayItem> source)
        {
            return;
        }

        SemanticRegionDisplayItem[] items = source.ToArray();
        int index = Array.FindIndex(items, item => string.Equals(
            item.Id,
            id,
            StringComparison.OrdinalIgnoreCase));
        _suppressSemanticRegionSelectionChange = true;
        try
        {
            _semanticRegionListBox.SelectedIndex = index;
        }
        finally
        {
            _suppressSemanticRegionSelectionChange = false;
        }
    }

    private void ClearSemanticSelections()
    {
        _suppressSemanticRegionSelectionChange = true;
        _suppressSemanticCorrectionSelectionChange = true;
        try
        {
            _semanticRegionListBox.SelectedIndex = -1;
            _semanticCorrectionListBox.SelectedIndex = -1;
        }
        finally
        {
            _suppressSemanticRegionSelectionChange = false;
            _suppressSemanticCorrectionSelectionChange = false;
        }
    }

    private void ClearDetailAndAutomaticSemanticSelections()
    {
        _suppressRegionSelectionChange = true;
        _suppressSemanticRegionSelectionChange = true;
        try
        {
            _regionListBox.SelectedIndex = -1;
            _semanticRegionListBox.SelectedIndex = -1;
        }
        finally
        {
            _suppressRegionSelectionChange = false;
            _suppressSemanticRegionSelectionChange = false;
        }

        PopulateRegionEditorControls(null);
    }

    private void ClearDetailAndCorrectionSelections()
    {
        _suppressRegionSelectionChange = true;
        _suppressSemanticCorrectionSelectionChange = true;
        try
        {
            _regionListBox.SelectedIndex = -1;
            _semanticCorrectionListBox.SelectedIndex = -1;
        }
        finally
        {
            _suppressRegionSelectionChange = false;
            _suppressSemanticCorrectionSelectionChange = false;
        }

        PopulateRegionEditorControls(null);
    }

    private void ClearAllOverlaySelections()
    {
        _suppressRegionSelectionChange = true;
        _suppressSemanticRegionSelectionChange = true;
        _suppressSemanticCorrectionSelectionChange = true;
        try
        {
            _regionListBox.SelectedIndex = -1;
            _semanticRegionListBox.SelectedIndex = -1;
            _semanticCorrectionListBox.SelectedIndex = -1;
        }
        finally
        {
            _suppressRegionSelectionChange = false;
            _suppressSemanticRegionSelectionChange = false;
            _suppressSemanticCorrectionSelectionChange = false;
        }

        PopulateRegionEditorControls(null);
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
        SemanticRegion region,
        bool active)
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
            StrokeThickness = (active ? 3d : 1.5d) / _imageViewportState.Zoom,
            Fill = new SolidColorBrush(Color.FromArgb(active ? (byte)48 : (byte)18, color.R, color.G, color.B)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rectangle, mapped.X);
        Canvas.SetTop(rectangle, mapped.Y);
        _selectionCanvas.Children.Add(rectangle);
    }

    private void AddSemanticCorrectionRectangle(
        UniformImageViewport viewport,
        SemanticCorrectionRegion correction,
        bool active)
    {
        ViewportRect mapped = viewport.MapToViewport(correction.Bounds);
        Color color = correction.Kind switch
        {
            SemanticCorrectionKind.ForcePrimarySubject => Color.FromRgb(90, 255, 130),
            SemanticCorrectionKind.ForceSubject => Color.FromRgb(255, 220, 80),
            SemanticCorrectionKind.ForceBackground => Color.FromRgb(70, 150, 255),
            SemanticCorrectionKind.IgnoreAutomaticDetection => Color.FromRgb(185, 185, 185),
            _ => Color.FromRgb(255, 255, 255)
        };
        Rectangle rectangle = new()
        {
            Width = mapped.Width,
            Height = mapped.Height,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = (active ? 4d : 2.5d) / _imageViewportState.Zoom,
            Fill = new SolidColorBrush(Color.FromArgb(active ? (byte)62 : (byte)34, color.R, color.G, color.B)),
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


    private SceneBoundaryOverlayMode ReadSelectedAnalysisOverlayMode()
    {
        return _semanticOverlayModeComboBox.SelectedItem is SceneBoundaryOverlayMode mode
            ? mode
            : SceneBoundaryOverlayMode.CombinedDetail;
    }

    private static DetailMap SelectAnalysisOverlayMap(
        SemanticAnalysisResult semantic,
        SceneBoundaryAnalysisResult boundary,
        BackgroundSuppressionResult? backgroundSuppression,
        SceneBoundaryOverlayMode mode)
    {
        return mode switch
        {
            SceneBoundaryOverlayMode.SemanticImportance => semantic.ImportanceMap,
            SceneBoundaryOverlayMode.Saliency => semantic.SaliencyMap,
            SceneBoundaryOverlayMode.Subjects => semantic.SubjectMap,
            SceneBoundaryOverlayMode.Silhouettes => semantic.SilhouetteMap,
            SceneBoundaryOverlayMode.FocalAreas => semantic.FocalMap,
            SceneBoundaryOverlayMode.EdgeStrength => boundary.EdgeStrengthMap,
            SceneBoundaryOverlayMode.ImportantEdges => boundary.EdgeImportanceMap,
            SceneBoundaryOverlayMode.SubjectBoundaries => boundary.SubjectBoundaryMap,
            SceneBoundaryOverlayMode.InternalStructure => boundary.InternalStructureMap,
            SceneBoundaryOverlayMode.TextureEdges => boundary.TextureEdgeMap,
            SceneBoundaryOverlayMode.BackgroundConfidence => boundary.BackgroundConfidenceMap,
            SceneBoundaryOverlayMode.Uncertainty => boundary.UncertaintyMap,
            SceneBoundaryOverlayMode.CombinedDetail => semantic.ImportanceMap,
            SceneBoundaryOverlayMode.EdgeDirections => boundary.EdgeImportanceMap,
            SceneBoundaryOverlayMode.BackgroundSuppression => backgroundSuppression?.SuppressionMap
                ?? DetailMap.CreateUniform(boundary.BackgroundConfidenceMap.Size, 0f),
            SceneBoundaryOverlayMode.BackgroundProtection => backgroundSuppression?.ProtectionMap
                ?? DetailMap.CreateUniform(boundary.BackgroundConfidenceMap.Size, 0f),
            SceneBoundaryOverlayMode.ArtisticDetail => backgroundSuppression?.EffectiveDetailMap
                ?? semantic.ImportanceMap,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown analysis overlay mode.")
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

    private void CaptureActiveWorkspaceRevisions()
    {
        _activeDetailRegionRevision = _workspace.DetailRegionRevision;
        _activeSemanticCorrectionRevision = _workspace.SemanticCorrectionRevision;
    }

    private void RestoreWorkspaceEdit(
        WorkspaceEditSnapshot snapshot,
        string? selectedRegionId = null,
        string? selectedCorrectionId = null,
        string? selectedSemanticRegionId = null)
    {
        _workspace.RestoreEditState(snapshot);
        CaptureActiveWorkspaceRevisions();
        RefreshSemanticRegions(selectedSemanticRegionId);
        RefreshSemanticCorrections(selectedCorrectionId);
        RefreshRegionVisuals(selectedRegionId);
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

    private static bool BoundaryAnalysisSettingsEqual(
        SceneBoundaryAnalysisSettings? first,
        SceneBoundaryAnalysisSettings second)
    {
        return first is not null
            && first.Enabled == second.Enabled
            && first.LuminanceWeight == second.LuminanceWeight
            && first.ColorWeight == second.ColorWeight
            && first.MultiscaleWeight == second.MultiscaleWeight
            && first.ContinuityWeight == second.ContinuityWeight
            && first.SemanticBoundaryWeight == second.SemanticBoundaryWeight
            && first.TextureSuppression == second.TextureSuppression
            && first.EdgeThreshold == second.EdgeThreshold
            && first.ImportantEdgeThreshold == second.ImportantEdgeThreshold
            && first.CoarseRadius == second.CoarseRadius
            && first.SmoothingRadius == second.SmoothingRadius
            && first.BoundaryProtectionRadius == second.BoundaryProtectionRadius;
    }

    private static bool BackgroundSuppressionSettingsEqual(
        BackgroundSuppressionSettings? first,
        BackgroundSuppressionSettings second)
    {
        return first is not null
            && first.Enabled == second.Enabled
            && first.OverallStrength == second.OverallStrength
            && first.DetailFloor == second.DetailFloor
            && first.UncertaintyProtection == second.UncertaintyProtection
            && first.SilhouetteProtection == second.SilhouetteProtection
            && first.TransitionSoftness == second.TransitionSoftness
            && first.BackgroundPlacementWeight == second.BackgroundPlacementWeight
            && first.StrokeLengthMultiplier == second.StrokeLengthMultiplier
            && first.StrokeWidthMultiplier == second.StrokeWidthMultiplier
            && first.SegmentMultiplier == second.SegmentMultiplier
            && first.CurveFreedomMultiplier == second.CurveFreedomMultiplier
            && first.ColorSimplification == second.ColorSimplification;
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

    private void AttachProjectDirtyTracking()
    {
        TextBox[] textBoxes =
        [
            _projectNameTextBox,
            _primitiveCountTextBox,
            _primitiveCandidatesTextBox,
            _primitiveMutationsTextBox,
            _primitiveMinimumSizeTextBox,
            _primitiveMaximumSizeTextBox,
            _primitiveOpacityTextBox,
            _primitiveDetailSizeTextBox,
            _primitivePlacementBiasTextBox,
            _primitiveErrorWeightTextBox,
            _primitiveSearchInfluenceTextBox,
            _hybridPrimitiveBudgetTextBox,
            _hybridFlowBudgetTextBox,
            _hybridRefinementBudgetTextBox,
            _hybridInfluenceStrengthTextBox,
            _hybridInfluenceRadiusTextBox,
            _hybridMaximumInfluencesTextBox,
            _hybridRefinementDetailBiasTextBox,
            _hybridRefinementLengthTextBox,
            _hybridRefinementWidthTextBox,
            _finalMaximumDimensionTextBox,
            _jpegQualityTextBox,
            _seedTextBox,
            _strokeCountTextBox,
            _segmentCountTextBox,
            _fieldScaleTextBox,
            _octavesTextBox,
            _persistenceTextBox,
            _lacunarityTextBox,
            _angleOffsetTextBox,
            _densityTextBox,
            _lengthScaleTextBox,
            _maximumCurveTextBox,
            _minimumWidthTextBox,
            _maximumWidthTextBox,
            _opacityTextBox,
            _brushHardnessTextBox,
            _brushSizeJitterTextBox,
            _brushOpacityJitterTextBox,
            _brushBristleCountTextBox,
            _brushBristleSpreadTextBox,
            _baseDetailTextBox,
            _edgeWeightTextBox,
            _contrastWeightTextBox,
            _smoothingRadiusTextBox,
            _placementBiasTextBox,
            _detailedLengthTextBox,
            _backgroundLengthTextBox,
            _detailedWidthTextBox,
            _backgroundWidthTextBox,
            _regionTransitionWidthTextBox,
            _semanticInfluenceTextBox,
            _semanticSaliencyWeightTextBox,
            _semanticSubjectWeightTextBox,
            _semanticSilhouetteWeightTextBox,
            _semanticFocalWeightTextBox,
            _semanticThresholdTextBox,
            _semanticMinimumAreaTextBox,
            _semanticMaximumSubjectsTextBox,
            _semanticCenterBiasTextBox,
            _semanticSmoothingRadiusTextBox,
            _semanticBoundaryRadiusTextBox,
            _boundaryLuminanceWeightTextBox,
            _boundaryColorWeightTextBox,
            _boundaryMultiscaleWeightTextBox,
            _boundaryContinuityWeightTextBox,
            _boundarySemanticWeightTextBox,
            _boundaryTextureSuppressionTextBox,
            _boundaryEdgeThresholdTextBox,
            _boundaryImportantThresholdTextBox,
            _boundaryCoarseRadiusTextBox,
            _boundarySmoothingRadiusTextBox,
            _boundaryProtectionRadiusTextBox,
            _boundaryTangentAlignmentTextBox,
            _boundaryAlignmentRadiusTextBox,
            _boundaryCrossingPenaltyTextBox,
            _boundaryHardThresholdTextBox,
            _boundaryTerminationStrengthTextBox,
            _boundaryInternalInfluenceTextBox,
            _boundaryTextureInfluenceTextBox,
            _boundaryContourReinforcementTextBox,
            _boundaryCornerPreservationTextBox,
            _backgroundSuppressionStrengthTextBox,
            _backgroundDetailFloorTextBox,
            _backgroundUncertaintyProtectionTextBox,
            _backgroundSilhouetteProtectionTextBox,
            _backgroundTransitionSoftnessTextBox,
            _backgroundPlacementWeightTextBox,
            _backgroundSuppressionLengthTextBox,
            _backgroundSuppressionWidthTextBox,
            _backgroundSegmentMultiplierTextBox,
            _backgroundCurveFreedomTextBox,
            _backgroundColorSimplificationTextBox
        ];
        foreach (TextBox textBox in textBoxes)
        {
            textBox.TextChanged += ProjectTextChanged;
        }

        ComboBox[] comboBoxes =
        [
            _generativeModeComboBox,
            _primitiveKindsComboBox,
            _hybridInfluenceKindComboBox,
            _previewQualityComboBox,
            _finalFormatComboBox,
            _flowFieldComboBox,
            _backgroundComboBox,
            _brushKindComboBox
        ];
        foreach (ComboBox comboBox in comboBoxes)
        {
            comboBox.SelectionChanged += ProjectSelectionChanged;
        }

        CheckBox[] checkBoxes =
        [
            _enableSemanticAnalysisCheckBox,
            _enableBoundaryAnalysisCheckBox,
            _enableBoundaryPaintingCheckBox,
            _enableBackgroundSuppressionCheckBox
        ];
        foreach (CheckBox checkBox in checkBoxes)
        {
            checkBox.IsCheckedChanged += ProjectCheckBoxChanged;
        }
    }

    private void ProjectTextChanged(object? sender, TextChangedEventArgs e)
    {
        MarkPresentationDirty();
    }

    private void ProjectSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        MarkPresentationDirty();
    }

    private void ProjectCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        MarkPresentationDirty();
    }

    private void MarkPresentationDirty()
    {
        if (_suppressDirtyTracking)
        {
            return;
        }

        _projectSessionController.NotifyProjectEdited();
        RefreshDirtyIndicator();
    }

    private void RefreshDirtyIndicator()
    {
        Title = _projectSessionController.HasUnsavedChanges
            ? "FlowPainter *"
            : "FlowPainter";
    }

    private Task<bool> ConfirmSessionReplacementAsync()
    {
        return _projectSessionController.ConfirmDestructiveActionAsync(
            _ => ShowUnsavedChangesDialogAsync(),
            _ => SaveProjectAsync());
    }

    private Task<UnsavedChangesDecision> ShowUnsavedChangesDialogAsync()
    {
        string projectName = _projectNameTextBox.Text?.Trim() ?? string.Empty;
        UnsavedChangesDialog dialog = new(projectName);
        return dialog.ShowDialog<UnsavedChangesDecision>(this);
    }

    private async void WindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _isClosed)
        {
            return;
        }

        e.Cancel = true;
        if (_closeGuardRunning)
        {
            return;
        }

        if (_operationCancellation is not null)
        {
            _operationCancellation.Cancel();
            _statusText.Text = "Operation cancellation requested. Close again after it stops.";
            return;
        }

        _closeGuardRunning = true;
        try
        {
            if (await ConfirmSessionReplacementAsync().ConfigureAwait(true))
            {
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _closeGuardRunning = false;
        }
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


    private sealed record SemanticCorrectionDisplayItem(string Id, string DisplayText)
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
        SceneBoundaryAnalysisResult Boundary,
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
        _sceneBoundaryAnalysisResult = null;
        _backgroundSuppressionResult = null;
        _previewStrokePlan = null;
        _previewPrimitivePlan = null;
        _previewHybridPlan = null;
        _previewBrushSettings = null;
        _activeDetailAnalysisSettings = null;
        _activeDetailRegionTransitionWidth = double.NaN;
        _activeDetailRegionRevision = -1L;
        _activeSemanticCorrectionRevision = -1L;
        _activeSemanticAnalysisSettings = null;
        _activeBoundaryAnalysisSettings = null;
        _activeBackgroundSuppressionSettings = null;
        _selectionStart = null;
        _selectionCurrent = null;
        _selectionPointerStartPosition = null;
        _imageViewportState.Reset();
    }
}
