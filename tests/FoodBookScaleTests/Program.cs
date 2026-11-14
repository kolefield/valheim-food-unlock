using System;
using FoodUnlock;

var cases = new (string Name, float Requested, float Width, float Height, float Expected)[]
{
    ("Default with spare room", 1f, 3840f, 2160f, 1f),
    ("2x with spare room", 2f, 3840f, 2160f, 2f),
    ("Fractional request", 1.5f, 2560f, 1440f, 1.5f),
    ("Exact fit including margins", 2f, 1632f, 1552f, 2f),
    ("Height-limited", 2f, 2560f, 1440f, 1408f / 760f),
    ("Width-limited", 2f, 1232f, 2000f, 1.5f),
    ("Default shrinks on a small canvas", 1f, 672f, 720f, 0.8f),
    ("Very small canvas", 2f, 72f, 70f, 0.05f),
    ("Canvas awaiting layout", 2f, 0f, 0f, 0f),
    ("No room between margins", 2f, 32f, 760f, 0f),
    ("Below configured range", 0f, 3840f, 2160f, 1f),
    ("Above configured range", 3f, 3840f, 2160f, 2f),
    ("Invalid numeric multiplier", float.NaN, 3840f, 2160f, 1f),
    ("Infinite multiplier", float.PositiveInfinity, 3840f, 2160f, 2f)
};

const float tolerance = 0.0001f;
foreach (var test in cases)
{
    var actual = FoodBookScale.Fit(test.Requested, test.Width, test.Height);
    if (!(Math.Abs(actual - test.Expected) < tolerance))
        throw new InvalidOperationException($"{test.Name}: expected {test.Expected}, got {actual}.");

    if (actual > 0f && (actual * FoodBookScale.Width > test.Width - 32f + tolerance
        || actual * FoodBookScale.Height > test.Height - 32f + tolerance))
        throw new InvalidOperationException($"{test.Name}: the scaled panel exceeds its bounds.");
}

Console.WriteLine($"Passed {cases.Length} food book scaling checks.");
