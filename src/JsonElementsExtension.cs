using System;
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
        if (TryGetInt32(element, out var value))
            return value;

        ThrowFormat("int", element);
        return default;
    }

    /// <summary>
    /// Fast bool conversion. Supports JSON booleans and "true"/"false" strings (case-insensitive).
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ToBool(this JsonElement element)
    {
        if (TryGetBoolean(element, out var value))
            return value;

        ThrowFormat("bool", element);
        return default;
    }

    /// <summary>
    /// Fast Guid conversion. Supports JSON string GUIDs.
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid ToGuid(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetGuid(out var guid))
            return guid;

        // Also accept raw string if someone encoded weirdly (still no allocation).
        if (element.ValueKind == JsonValueKind.String)
        {
            var s = element.GetString();
            if (s is not null && Guid.TryParse(s, out var parsed))
                return parsed;
        }

        ThrowFormat("Guid", element);
        return default;
    }

    /// <summary>
    /// Fast DateTime conversion. Supports JSON string values (ISO 8601 preferred).
    /// Throws on invalid input.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTime(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetDateTime(out var dt))
            return dt;

        // Fallback (still may allocate because GetString() returns a string from the document)
        if (element.ValueKind == JsonValueKind.String)
        {
            var s = element.GetString();
            if (s is not null && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
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
        if (element.ValueKind == JsonValueKind.String && element.TryGetDateTimeOffset(out var dto))
            return dto;

        if (element.ValueKind == JsonValueKind.String)
        {
            var s = element.GetString();
            if (s is not null && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
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
            var s = element.GetString();
            if (s is not null && Guid.TryParse(s, out value))
                return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetInt32(in JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        if (element.ValueKind == JsonValueKind.String)
        {
            var s = element.GetString();
            if (s is not null)
                return int.TryParse(s, _intStyles, CultureInfo.InvariantCulture, out value);
        }

        value = default;
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
            var s = element.GetString();
            if (s is not null)
                return bool.TryParse(s, out value);
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFormat(string target, JsonElement element)
        => throw new FormatException($"JsonElement could not be converted to {target}. ValueKind={element.ValueKind}.");
}
