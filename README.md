[![](https://img.shields.io/nuget/v/soenneker.extensions.jsonelements.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.jsonelements/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.jsonelements/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.jsonelements/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.jsonelements.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.jsonelements/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.jsonelements/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.jsonelements/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.JsonElements
Strict and non-throwing primitive conversion, string projection, typed deserialization, and recursive object conversion for `JsonElement`.

## Installation

```bash
dotnet add package Soenneker.Extensions.JsonElements
```

## Read primitive values

```csharp
using Soenneker.Extensions.JsonElements;

using JsonDocument document = JsonDocument.Parse(json);
JsonElement root = document.RootElement;

int count = root.GetProperty("count").ToInt();
bool enabled = root.GetProperty("enabled").ToBool();
Guid id = root.GetProperty("id").ToGuid();
```

The strict converters accept these JSON forms:

- `ToInt()` accepts a JSON integer or an invariant-culture integer string within the `Int32` range.
- `ToBool()` accepts JSON `true`/`false` or a case-insensitive Boolean string.
- `ToGuid()` accepts a JSON string that `Guid` can parse.
- `ToDateTime()` and `ToDateTimeOffset()` accept JSON strings, preferring the `System.Text.Json` ISO 8601 parser and then invariant round-trip parsing.

An incompatible kind, malformed value, or out-of-range number throws `FormatException`. Use `TryToInt()`, `TryToBool()`, or `TryToGuid()` when invalid input is expected:

```csharp
if (root.GetProperty("count").TryToInt(out int count))
{
    Process(count);
}
```

`IsNullOrUndefined()` distinguishes JSON null/missing-style default elements before conversion.

## Convert to text

```csharp
string text = element.ToStr();
```

`ToStr()` returns the decoded value for a JSON string, invariant text for an `Int64`, lowercase text for a Boolean, and an empty string for null or undefined. Decimal/exponent numbers retain their raw JSON token so precision is not lost. Objects and arrays also return their compact raw JSON.

## Deserialize a structured value

```csharp
OrderDto? order = element.To<OrderDto>();
```

`To<T>()` uses `JsonSerializerOptions.Web`. Null and undefined return `default`; serialization failures propagate. Prefer the primitive methods above when a complete object deserialization is unnecessary.

## Build ordinary .NET collections

```csharp
object? value = element.JsonElementToObject();
```

`JsonElementToObject()` recursively maps objects to `Dictionary<string, object?>`, arrays to `List<object?>`, strings to `string`, Booleans to `bool`, and null/undefined to `null`. Numbers become `long`, then `decimal`, then `double` according to the first representation that fits; a number outside all three ranges is preserved as raw JSON text. When an object contains duplicate property names, the last value wins.

All methods read from the existing `JsonElement`. Keep its owning `JsonDocument` alive for the duration of the call unless the element was cloned.
