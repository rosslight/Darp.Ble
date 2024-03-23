using Darp.Ble.Data;
using FluentAssertions.Numeric;

namespace Darp.Ble.Tests;

public sealed class UInt48Assertions : NumericAssertions<UInt48>
{
    public UInt48Assertions(UInt48 value) : base(value)
    {
    }
}

public static class UInt48AssertionExtensions
{
    public static NumericAssertions<UInt48> Should(this UInt48 actualValue)
    {
        return new UInt48Assertions(actualValue);
    }
}