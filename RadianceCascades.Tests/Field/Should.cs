using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace RadianceCascades.Tests.Field;

// The assertions the plan's tests make, in the shape of the FluentAssertions calls (a package on
// nuget.org) they were first written with, so each test reads as it did, since the module takes no
// package past its test framework. A subject is told apart by its static type, as that library's
// overloads tell it apart, a bool, an int or a float, another value, a value that may be null, a
// collection or any other object. Each failure throws xunit's exception with the subject's
// expression, what was expected of it, the reason given and what was found.
internal static class ShouldExtensions
{
    public static BooleanAssertions Should(this bool subject, [CallerArgumentExpression(nameof(subject))] string text = "") => new(subject, text);

    public static NumericAssertions<int> Should(this int subject, [CallerArgumentExpression(nameof(subject))] string text = "") => new(subject, text);

    public static NumericAssertions<float> Should(this float subject, [CallerArgumentExpression(nameof(subject))] string text = "") => new(subject, text);

    public static ValueAssertions<T> Should<T>(this T subject, [CallerArgumentExpression(nameof(subject))] string text = "")
        where T : struct => new(subject, text);

    public static NullableAssertions<T> Should<T>(this T? subject, [CallerArgumentExpression(nameof(subject))] string text = "")
        where T : struct => new(subject, text);

    public static CollectionAssertions<T> Should<T>(this IEnumerable<T>? subject, [CallerArgumentExpression(nameof(subject))] string text = "") =>
        new(subject, text);

    public static ObjectAssertions Should(this object? subject, [CallerArgumentExpression(nameof(subject))] string text = "") => new(subject, text);
}

// The failures of the assertions, each worded "Expected <subject> <expectation> because <reason>,
// but <what was found>."
internal static class Failure
{
    // How many items of a collection a failure lists before it counts the rest.
    private const int Listed = 32;

    public static XunitException Of(string subject, string expectation, string because, string found) =>
        new($"Expected {subject} {expectation}{Reason(because)}, but {found}.");

    // The reason given, with "because" set before a phrase that does not open with it.
    private static string Reason(string because)
    {
        if (string.IsNullOrWhiteSpace(because)) return "";
        return because.TrimStart().StartsWith("because", StringComparison.Ordinal) ? " " + because.Trim() : " because " + because.Trim();
    }

    // A value as a failure shows it, a collection by its first items and the count of the rest.
    public static string Show(object? value)
    {
        switch (value)
        {
            case null:
                return "<null>";
            case string text:
                return $"\"{text}\"";
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            case IEnumerable items:
                var all = items.Cast<object?>().ToList();
                var listed = string.Join(", ", all.Take(Listed).Select(Show));
                return all.Count > Listed ? $"{{{listed}, and {all.Count - Listed} more}}" : $"{{{listed}}}";
            default:
                return value.ToString() ?? "";
        }
    }
}

// What an assertion that held hands on, the assertions to chain the next from, or the one item a
// collection was found to hold.
internal sealed class AndConstraint<TAssertions>(TAssertions and)
{
    public TAssertions And { get; } = and;
}

internal sealed class AndWhichConstraint<TItem>(TItem which)
{
    public TItem Which { get; } = which;

    public TItem Subject => Which;
}

internal sealed class BooleanAssertions(bool subject, string text)
{
    public AndConstraint<BooleanAssertions> Be(bool expected, string because = "")
    {
        if (subject != expected) throw Failure.Of(text, $"to be {expected}", because, $"found {subject}");
        return new(this);
    }

    public AndConstraint<BooleanAssertions> BeTrue(string because = "") => Be(true, because);

    public AndConstraint<BooleanAssertions> BeFalse(string because = "") => Be(false, because);
}

// A number compared by its own operators, so a float that is not a number fails every comparison.
internal sealed class NumericAssertions<T>(T subject, string text) where T : struct, INumber<T>
{
    public AndConstraint<NumericAssertions<T>> Be(T expected, string because = "") =>
        Check(subject == expected, "to be", expected, because);

    public AndConstraint<NumericAssertions<T>> BeGreaterThan(T expected, string because = "") =>
        Check(subject > expected, "to be greater than", expected, because);

    public AndConstraint<NumericAssertions<T>> BeGreaterThanOrEqualTo(T expected, string because = "") =>
        Check(subject >= expected, "to be greater than or equal to", expected, because);

    public AndConstraint<NumericAssertions<T>> BeLessThan(T expected, string because = "") =>
        Check(subject < expected, "to be less than", expected, because);

    public AndConstraint<NumericAssertions<T>> BeLessThanOrEqualTo(T expected, string because = "") =>
        Check(subject <= expected, "to be less than or equal to", expected, because);

    public AndConstraint<NumericAssertions<T>> BeInRange(T minimum, T maximum, string because = "")
    {
        if (!(subject >= minimum && subject <= maximum))
            throw Failure.Of(text, $"to be between {Failure.Show(minimum)} and {Failure.Show(maximum)}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }

    // Within the precision either side of the value expected, as the library has it, so a number
    // that is not one fails.
    public AndConstraint<NumericAssertions<T>> BeApproximately(T expected, T precision, string because = "")
    {
        if (!(T.Abs(subject - expected) <= precision))
            throw Failure.Of(text, $"to approximate {Failure.Show(expected)} +/- {Failure.Show(precision)}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }

    private AndConstraint<NumericAssertions<T>> Check(bool holds, string relation, T expected, string because)
    {
        if (!holds) throw Failure.Of(text, $"{relation} {Failure.Show(expected)}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }
}

internal sealed class ValueAssertions<T>(T subject, string text) where T : struct
{
    public AndConstraint<ValueAssertions<T>> Be(T expected, string because = "")
    {
        if (!EqualityComparer<T>.Default.Equals(subject, expected))
            throw Failure.Of(text, $"to be {Failure.Show(expected)}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }
}

internal sealed class NullableAssertions<T>(T? subject, string text) where T : struct
{
    public AndConstraint<NullableAssertions<T>> Be(T? expected, string because = "")
    {
        if (!Nullable.Equals(subject, expected))
            throw Failure.Of(text, $"to be {Failure.Show(expected)}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }

    public AndConstraint<NullableAssertions<T>> BeNull(string because = "")
    {
        if (subject is not null) throw Failure.Of(text, "to be <null>", because, $"found {Failure.Show(subject)}");
        return new(this);
    }

    public AndConstraint<NullableAssertions<T>> NotBeNull(string because = "")
    {
        if (subject is null) throw Failure.Of(text, "not to be <null>", because, "found <null>");
        return new(this);
    }
}

internal sealed class ObjectAssertions(object? subject, string text)
{
    public AndConstraint<ObjectAssertions> BeSameAs(object? expected, string because = "")
    {
        if (!ReferenceEquals(subject, expected))
            throw Failure.Of(text, $"to be the same object as {Failure.Show(expected)}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }

    public AndConstraint<ObjectAssertions> Match<TMatch>(Func<TMatch, bool> predicate, string because = "",
        [CallerArgumentExpression(nameof(predicate))] string condition = "")
    {
        if (subject is not TMatch match || !predicate(match))
            throw Failure.Of(text, $"to match {condition}", because, $"found {Failure.Show(subject)}");
        return new(this);
    }
}

// A collection read once, as it stands when the assertion is made, its items compared by their
// own equality.
internal sealed class CollectionAssertions<T>(IEnumerable<T>? subject, string text)
{
    private readonly List<T>? _items = subject?.ToList();

    public AndConstraint<CollectionAssertions<T>> BeNull(string because = "")
    {
        if (_items is not null) throw Failure.Of(text, "to be <null>", because, $"found {Failure.Show(_items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> NotBeNull(string because = "")
    {
        if (_items is null) throw Failure.Of(text, "not to be <null>", because, "found <null>");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> BeEmpty(string because = "")
    {
        var items = Items(because);
        if (items.Count > 0) throw Failure.Of(text, "to be empty", because, $"found {Found(items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> NotBeEmpty(string because = "")
    {
        if (Items(because).Count == 0) throw Failure.Of(text, "not to be empty", because, "found it empty");
        return new(this);
    }

    public AndWhichConstraint<T> ContainSingle(string because = "")
    {
        var items = Items(because);
        if (items.Count != 1) throw Failure.Of(text, "to contain a single item", because, $"found {Found(items)}");
        return new(items[0]);
    }

    public AndWhichConstraint<T> ContainSingle(Func<T, bool> predicate, string because = "",
        [CallerArgumentExpression(nameof(predicate))] string condition = "")
    {
        var items = Items(because);
        var matching = items.Where(predicate).ToList();
        if (matching.Count != 1)
            throw Failure.Of(text, $"to contain a single item matching {condition}", because, $"found {matching.Count} matching of {Found(items)}");
        return new(matching[0]);
    }

    public AndConstraint<CollectionAssertions<T>> HaveCount(int expected, string because = "")
    {
        var items = Items(because);
        if (items.Count != expected) throw Failure.Of(text, $"to hold {expected} items", because, $"found {Found(items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> HaveCountGreaterThan(int expected, string because = "")
    {
        var items = Items(because);
        if (items.Count <= expected) throw Failure.Of(text, $"to hold more than {expected} items", because, $"found {Found(items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> Contain(T expected, string because = "")
    {
        var items = Items(because);
        if (!items.Contains(expected)) throw Failure.Of(text, $"to contain {Failure.Show(expected)}", because, $"found {Found(items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> Contain(Func<T, bool> predicate, string because = "",
        [CallerArgumentExpression(nameof(predicate))] string condition = "")
    {
        var items = Items(because);
        if (!items.Any(predicate)) throw Failure.Of(text, $"to contain an item matching {condition}", because, $"found {Found(items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> NotContain(T unexpected, string because = "")
    {
        var items = Items(because);
        if (items.Contains(unexpected)) throw Failure.Of(text, $"not to contain {Failure.Show(unexpected)}", because, $"found {Found(items)}");
        return new(this);
    }

    // As the library has it, a collection with no items holds only items that match.
    public AndConstraint<CollectionAssertions<T>> OnlyContain(Func<T, bool> predicate, string because = "",
        [CallerArgumentExpression(nameof(predicate))] string condition = "")
    {
        var items = Items(because);
        var mismatched = items.Where(item => !predicate(item)).ToList();
        if (mismatched.Count > 0)
            throw Failure.Of(text, $"to contain only items matching {condition}", because, $"found {Failure.Show(mismatched)} not matching, of {Found(items)}");
        return new(this);
    }

    public AndConstraint<CollectionAssertions<T>> Equal(params T[] expected) => Equal((IEnumerable<T>)expected, "");

    public AndConstraint<CollectionAssertions<T>> Equal(IEnumerable<T> expected, string because = "")
    {
        var items = Items(because);
        var wanted = expected.ToList();
        if (!items.SequenceEqual(wanted)) throw Failure.Of(text, $"to equal {Found(wanted)} in order", because, $"found {Found(items)}");
        return new(this);
    }

    // The same items as many times each in any order, as the library matches each item expected
    // to one found.
    public AndConstraint<CollectionAssertions<T>> BeEquivalentTo(IEnumerable<T> expected, string because = "")
    {
        var items = Items(because);
        var wanted = expected.ToList();
        var left = new Dictionary<Key, int>();
        foreach (var item in items) left[new Key(item)] = left.GetValueOrDefault(new Key(item)) + 1;
        var missing = new List<T>();
        foreach (var item in wanted)
        {
            if (left.TryGetValue(new Key(item), out var count) && count > 0) left[new Key(item)] = count - 1;
            else missing.Add(item);
        }
        var extra = left.Where(entry => entry.Value > 0).SelectMany(entry => Enumerable.Repeat(entry.Key.Item, entry.Value)).ToList();
        if (missing.Count > 0 || extra.Count > 0)
            throw Failure.Of(text, $"to be equivalent to {Found(wanted)}", because,
                $"found {Found(items)}, without {Failure.Show(missing)} and with {Failure.Show(extra)} besides");
        return new(this);
    }

    private List<T> Items(string because) => _items ?? throw Failure.Of(text, "to be a collection", because, "found <null>");

    private static string Found(List<T> items) => $"{Failure.Show(items)} ({items.Count} items)";

    // An item as a key, which a null item may be.
    private readonly record struct Key(T Item);
}
