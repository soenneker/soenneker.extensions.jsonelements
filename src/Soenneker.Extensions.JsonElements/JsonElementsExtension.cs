using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Soenneker.Extensions.JsonElements;

/// <summary>
/// High-performance, convenience extension methods for <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementsExtension
{
    private const NumberStyles _intStyles = NumberStyles.Integer;

    /// <summary>True if element is Null or Undefined.</summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrUndefined(this JsonElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

    /// <summary>
    /// Fast int conversion. Supports JSON numbers and numeric strings.
    /// Throws on invalid input (keeps the "ToX" semantics).
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt(this JsonElement element)
    {
        if (TryGetInt32(element, out int value))
            return value;

        ThrowFormat("int", element);
        return 0;
    }

    /// <summary>
    /// Fast bool conversion. Supports JSON booleans and "true"/"false" strings (case-insensitive).
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToBool(this JsonElement element)
    {
        if (TryGetBoolean(element, out bool value))
            return value;

        ThrowFormat("bool", element);
        return false;
    }

    /// <summary>
    /// Fast Guid conversion. Supports JSON string GUIDs.
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid ToGuid(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetGuid(out Guid guid))
            return guid;

        // Also accept raw string if someone encoded weirdly (still no allocation).
        if (element.ValueKind == JsonValueKind.String)
        {
            string? s = element.GetString();
            if (s is not null && Guid.TryParse(s, out Guid parsed))
                return parsed;
        }

        ThrowFormat("Guid", element);
        return Guid.Empty;
    }

    /// <summary>
    /// Fast DateTime conversion. Supports JSON string values (ISO 8601 preferred).
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTime(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetDateTime(out DateTime dt))
            return dt;

        // Fallback (still may allocate because GetString() returns a string from the document)
        if (element.ValueKind == JsonValueKind.String)
        {
            string? s = element.GetString();
            if (s is not null && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
                return parsed;
        }

        ThrowFormat("DateTime", element);
        return default;
    }

    /// <summary>
    /// Fast DateTimeOffset conversion. Supports JSON string values (ISO 8601 preferred).
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToDateTimeOffset(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetDateTimeOffset(out DateTimeOffset dto))
            return dto;

        if (element.ValueKind == JsonValueKind.String)
        {
            string? s = element.GetString();
            if (s is not null && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed))
                return parsed;
        }

        ThrowFormat("DateTimeOffset", element);
        return default;
    }

    /// <summary>
    /// Returns a string view of the element with minimal work/allocations.
    /// - String: returns the JSON string value.
    /// - Number: uses TryGetInt64/TryGetDouble to avoid serializing the element.
    /// - True/False: returns "true"/"false".
    /// - Null/Undefined: returns "" (or change to null if you prefer).
    /// - Object/Array: returns raw JSON via GetRawText() (allocates a string, but avoids formatting).
    /// </summary>
    [Pure]
    public static string ToStr(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString()!;

            case JsonValueKind.Number:
                // Prefer integer path when possible (fewer chars, faster).
                if (element.TryGetInt64(out long l))
                    return l.ToString(CultureInfo.InvariantCulture);
                if (element.TryGetDouble(out double d))
                    return d.ToString("R", CultureInfo.InvariantCulture);
                // As a last resort, raw json token
                return element.GetRawText();

            case JsonValueKind.True:
                return "true";
            case JsonValueKind.False:
                return "false";

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return string.Empty;

            default:
                // Object/Array: raw JSON (still allocates, but avoids serializer formatting work).
                return element.GetRawText();
        }
    }

    /// <summary>
    /// Deserializes the element to <typeparamref name="T"/> using Web defaults.
    /// Note: this can be expensive because it deserializes from the element (often via raw text).
    /// Prefer explicit getters / TryGet methods for primitives.
    /// </summary>
    [Pure]
    public static T? To<T>(this JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return default;

        // In .NET 8+ this uses JsonTypeInfo if provided; otherwise it may go through raw text.
        return element.Deserialize<T>(JsonSerializerOptions.Web);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryToInt(this JsonElement element, out int value)
        => TryGetInt32(element, out value);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryToBool(this JsonElement element, out bool value)
        => TryGetBoolean(element, out value);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryToGuid(this JsonElement element, out Guid value)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetGuid(out value))
            return true;

        if (element.ValueKind == JsonValueKind.String)
        {
            string? s = element.GetString();
            if (s is not null && Guid.TryParse(s, out value))
                return true;
        }

        value = Guid.Empty;
        return false;
    }

    /// <summary>
    /// Converts a JSON element to a corresponding .NET object representation.
    /// </summary>
    /// <remarks>This method recursively converts the structure of the JSON element. Property names are
    /// preserved as dictionary keys, and array elements are converted to a list. Numeric values are returned as Int64
    /// when possible, otherwise as Double.</remarks>
    /// <param name="element">The JSON element to convert.</param>
    /// <returns>A .NET object representing the JSON value. Returns a dictionary for JSON objects, a list for arrays, a string
    /// for string values, a numeric type for numbers, a Boolean for true or false, or null for null or undefined
    /// values.</returns>
    [Pure]
    public static object? JsonElementToObject(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                // JsonElement doesn't expose property-count cheaply.
                // Still: avoid LINQ and ToDictionary allocations.
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);

                foreach (JsonProperty p in element.EnumerateObject())
                    dict[p.Name] = p.Value.JsonElementToObject();

                return dict;
            }

            case JsonValueKind.Array:
            {
                int len = element.GetArrayLength();
                var list = new List<object?>(len);

                foreach (JsonElement item in element.EnumerateArray())
                    list.Add(item.JsonElementToObject());

                return list;
            }

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                return element.TryGetInt64(out long i) ? i : element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetInt32(in JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        if (element.ValueKind == JsonValueKind.String)
        {
            string? s = element.GetString();
            if (s is not null)
                return int.TryParse(s, _intStyles, CultureInfo.InvariantCulture, out value);
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetBoolean(in JsonElement element, out bool value)
    {
        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            string? s = element.GetString();
            if (s is not null)
                return bool.TryParse(s, out value);
        }

        value = false;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFormat(string target, JsonElement element)
        => throw new FormatException($"JsonElement could not be converted to {target}. ValueKind={element.ValueKind}.");
}
