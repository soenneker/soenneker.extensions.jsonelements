using System;
using System.Text.Json;
using Soenneker.Tests.Unit;

namespace Soenneker.Extensions.JsonElements.Tests;

public sealed class JsonElementExtensionTests : UnitTest
{
    [Test]
    public async System.Threading.Tasks.Task ToStr_preserves_high_precision_number_text()
    {
        const string number = "0.1234567890123456789012345678";
        using JsonDocument document = JsonDocument.Parse(number);

        await Assert.That(document.RootElement.ToStr()).IsEqualTo(number);
    }

    [Test]
    public async System.Threading.Tasks.Task Object_conversion_uses_decimal_before_double()
    {
        using JsonDocument document = JsonDocument.Parse("0.1234567890123456789012345678");

        object? value = document.RootElement.JsonElementToObject();

        await Assert.That(value).IsTypeOf<decimal>();
    }
}
