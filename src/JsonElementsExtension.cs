using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Soenneker.Extensions.JsonElements;

/// <summary>
/// Provides a set of high-performance, convenience extension methods for working with <see cref="JsonElement"/> values,
/// offering safe and efficient conversion to common .NET types.
/// </summary>
public static class JsonElementsExtension
{
    /// <summary>
    /// Attempts to extract an <see cref="int"/> value from the <see cref="JsonElement"/>.
    /// Supports elements of kind <see cref="JsonValueKind.Number"/> and <see cref="JsonValueKind.String"/> containing a valid integer.
    /// </summary>
    [Pure]
    public static int ToInt(this JsonElement element)
    {
        return element.GetInt32();
    }

    /// <summary>
    /// Attempts to extract a <see cref="bool"/> value from the <see cref="JsonElement"/>.
    /// Supports boolean values as well as strings that can be parsed as boolean (e.g. "true", "false").
    /// </summary>
    [Pure]
    public static bool ToBool(this JsonElement element)
    {
        return element.GetBoolean();
    }

    /// <summary>
    /// Determines whether the <see cref="JsonElement"/> is null or undefined.
    /// </summary>
    /// <param name="element">The <see cref="JsonElement"/> to inspect.</param>
    /// <returns><c>true</c> if the element is <see cref="JsonValueKind.Null"/> or <see cref="JsonValueKind.Undefined"/>; otherwise, <c>false</c>.</returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrUndefined(this JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;
    }

    /// <summary>
    /// Attempts to extract a <see cref="Guid"/> value from the <see cref="JsonElement"/>.
    /// Supports string values containing a valid GUID.
    /// </summary>
    [Pure]
    public static Guid ToGuid(this JsonElement element)
    {
        return element.GetGuid();
    }

    /// <summary>
    /// Attempts to extract a <see cref="DateTime"/> value from the <see cref="JsonElement"/>.
    /// Supports string values containing a valid ISO 8601 date-time format.
    /// </summary>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTime(this JsonElement element)
    {
        return element.GetDateTime();
    }

    /// <summary>
    /// Converts the specified JSON element to a <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="element">The <see cref="JsonElement"/> containing a date and time value to convert.</param>
    /// <returns>A <see cref="DateTimeOffset"/> representation of the date and time value contained in the JSON element.</returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ToDateTimeOffset(this JsonElement element)
    {
        return element.GetDateTimeOffset();
    }

    /// <summary>
    /// Extracts the string value from the <see cref="JsonElement"/>.
    /// If the element is of kind <see cref="JsonValueKind.String"/>, the raw string is returned.
    /// Otherwise, the element is converted to a string using its default serialization.
    /// </summary>
    /// <param name="element">The <see cref="JsonElement"/> to extract the string from.</param>
    /// <returns>The string representation of the element.</returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStr(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString()!;

        return element.ToString();
    }

    /// <summary>
    /// Deserializes the <see cref="JsonElement"/> into an instance of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="element">The <see cref="JsonElement"/> to deserialize.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/> or <c>null</c> if the element represents null.</returns>
    [Pure]
    public static T? To<T>(this JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return default;

        return element.Deserialize<T>(JsonSerializerOptions.Web);
    }
}