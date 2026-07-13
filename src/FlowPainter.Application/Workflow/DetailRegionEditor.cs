using System.Globalization;
using FlowPainter.Domain.Detail;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Workflow;

public sealed class DetailRegionEditor
{
    private const double MinimumRegionSize = 0.002d;
    private readonly List<DetailRegion> _regions = [];
    private readonly IReadOnlyList<DetailRegion> _readOnlyRegions;
    private int _nextRegionNumber = 1;

    public DetailRegionEditor()
    {
        _readOnlyRegions = _regions.AsReadOnly();
    }

    public IReadOnlyList<DetailRegion> Regions => _readOnlyRegions;

    public int Count => _regions.Count;

    public DetailRegion Add(
        NormalizedRect bounds,
        double strength,
        DetailRegionIntent intent,
        string? label = null)
    {
        ValidateRegionSize(bounds);
        int regionNumber = _nextRegionNumber++;
        string resolvedLabel = string.IsNullOrWhiteSpace(label)
            ? intent == DetailRegionIntent.IncreaseDetail
                ? $"Focus {regionNumber}"
                : $"Background {regionNumber}"
            : label.Trim();
        DetailRegion region = new(
            $"manual-{regionNumber:D4}",
            bounds,
            strength,
            DetailRegionOrigin.Manual,
            intent,
            resolvedLabel);
        _regions.Add(region);
        return region;
    }

    public void ReplaceAll(IEnumerable<DetailRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        DetailRegion[] replacement = regions.ToArray();
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        foreach (DetailRegion region in replacement)
        {
            ValidateRegionSize(region.Bounds);
            if (!identifiers.Add(region.Id))
            {
                throw new ArgumentException(
                    $"Duplicate detail-region identifier '{region.Id}'.",
                    nameof(regions));
            }
        }

        _regions.Clear();
        _regions.AddRange(replacement);
        _nextRegionNumber = FindNextRegionNumber(replacement);
    }

    public DetailRegion Update(
        string id,
        NormalizedRect bounds,
        double strength,
        DetailRegionIntent intent,
        string? label)
    {
        ValidateRegionSize(bounds);
        int index = FindIndex(id);
        DetailRegion current = _regions[index];
        DetailRegion updated = new(
            current.Id,
            bounds,
            strength,
            current.Origin,
            intent,
            label);
        _regions[index] = updated;
        return updated;
    }

    public DetailRegion Move(string id, double deltaX, double deltaY)
    {
        if (!double.IsFinite(deltaX))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaX), deltaX, "Horizontal movement must be finite.");
        }

        if (!double.IsFinite(deltaY))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaY), deltaY, "Vertical movement must be finite.");
        }

        DetailRegion region = Get(id);
        double left = Math.Clamp(region.Bounds.Left + deltaX, 0d, 1d - region.Bounds.Width);
        double top = Math.Clamp(region.Bounds.Top + deltaY, 0d, 1d - region.Bounds.Height);
        NormalizedRect bounds = new(
            left,
            top,
            left + region.Bounds.Width,
            top + region.Bounds.Height);
        return Update(region.Id, bounds, region.Strength, region.Intent, region.Label);
    }

    public DetailRegion Resize(
        string id,
        double left,
        double top,
        double width,
        double height)
    {
        ValidateFiniteUnit(left, nameof(left));
        ValidateFiniteUnit(top, nameof(top));
        ValidateSize(width, nameof(width));
        ValidateSize(height, nameof(height));

        if (left + width > 1d || top + height > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "The resized region must remain inside the normalized image bounds.");
        }

        DetailRegion region = Get(id);
        NormalizedRect bounds = new(left, top, left + width, top + height);
        return Update(region.Id, bounds, region.Strength, region.Intent, region.Label);
    }

    public bool MoveEarlier(string id)
    {
        int index = FindIndex(id);
        if (index == 0)
        {
            return false;
        }

        (_regions[index - 1], _regions[index]) = (_regions[index], _regions[index - 1]);
        return true;
    }

    public bool MoveLater(string id)
    {
        int index = FindIndex(id);
        if (index == _regions.Count - 1)
        {
            return false;
        }

        (_regions[index + 1], _regions[index]) = (_regions[index], _regions[index + 1]);
        return true;
    }

    public DetailRegion Get(string id)
    {
        return _regions[FindIndex(id)];
    }

    public bool Remove(string id)
    {
        int index = _regions.FindIndex(region => string.Equals(region.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        _regions.RemoveAt(index);
        return true;
    }

    public DetailRegion? RemoveLast()
    {
        if (_regions.Count == 0)
        {
            return null;
        }

        DetailRegion removed = _regions[^1];
        _regions.RemoveAt(_regions.Count - 1);
        return removed;
    }

    public void Clear()
    {
        _regions.Clear();
    }

    private int FindIndex(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A region identifier is required.", nameof(id));
        }

        int index = _regions.FindIndex(region => string.Equals(region.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        return index >= 0
            ? index
            : throw new KeyNotFoundException($"Detail region '{id}' was not found.");
    }

    private static int FindNextRegionNumber(IEnumerable<DetailRegion> regions)
    {
        int maximum = 0;
        foreach (DetailRegion region in regions)
        {
            const string Prefix = "manual-";
            if (region.Id.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(
                    region.Id.AsSpan(Prefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int number))
            {
                maximum = Math.Max(maximum, number);
            }
        }

        return maximum + 1;
    }

    private static void ValidateRegionSize(NormalizedRect bounds)
    {
        if (bounds.Width < MinimumRegionSize || bounds.Height < MinimumRegionSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds),
                bounds,
                $"The region width and height must each be at least {MinimumRegionSize}.");
        }
    }

    private static void ValidateFiniteUnit(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and between 0 and 1.");
        }
    }

    private static void ValidateSize(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < MinimumRegionSize || value > 1d)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The region size must be finite and between {MinimumRegionSize} and 1.");
        }
    }
}
