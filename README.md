[![](https://img.shields.io/nuget/v/soenneker.extensions.jsonelements.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.jsonelements/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.jsonelements/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.jsonelements/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.jsonelements.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.jsonelements/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.jsonelements/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.jsonelements/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.JsonElements
A collection of helpful JsonElement extension methods.

## Installation

```bash
dotnet add package Soenneker.Extensions.JsonElements
```

## Quick start

```csharp
using Soenneker.Extensions.JsonElements;

// Given an existing JsonElement named element:
var result = element.IsNullOrUndefined();
```

## Common operations

- `IsNullOrUndefined()` - True if element is Null or Undefined.
- `ToInt()` - Fast int conversion. Supports JSON numbers and numeric strings. Throws on invalid input (keeps the "ToX" semantics).
- `ToBool()` - Fast bool conversion. Supports JSON booleans and "true"/"false" strings (case-insensitive). Throws on invalid input.
- `ToGuid()` - Fast Guid conversion. Supports JSON string GUIDs. Throws on invalid input.
- `ToDateTime()` - Fast DateTime conversion. Supports JSON string values (ISO 8601 preferred). Throws on invalid input.
- `ToDateTimeOffset()` - Fast DateTimeOffset conversion. Supports JSON string values (ISO 8601 preferred). Throws on invalid input.
- `ToStr()` - Returns a string view of the element with minimal work/allocations. - String: returns the JSON string value. - Number: uses TryGetInt64/TryGetDouble to avoid serializing the element. - True/False: returns "true"/"false". - Null/Undefined: returns "" (or change to null if you prefer). - Object/Array: returns raw JSON via GetRawText() (allocates a string, but avoids formatting).
- `To()` - Deserializes the element to `T` using Web defaults. Note: this can be expensive because it deserializes from the element (often via raw text). Prefer explicit getters / TryGet methods for primitives.
- `TryToInt()` - Attempts to read the JSON value as an `int`; returns `false` instead of throwing when it is null or incompatible.
- `TryToBool()` - Attempts to read the JSON value as a `bool`; returns `false` instead of throwing when it is null or incompatible.
- `TryToGuid()` - Attempts to read the JSON value as a `Guid`; returns `false` instead of throwing when it is null or malformed.
- `JsonElementToObject()` - Converts a JSON element to a corresponding .NET object representation. Returns a .NET object representing the JSON value. Returns a dictionary for JSON objects, a list for arrays, a string for string values, a numeric type for numbers, a Boolean for true or false, or null for null or undefined values.
