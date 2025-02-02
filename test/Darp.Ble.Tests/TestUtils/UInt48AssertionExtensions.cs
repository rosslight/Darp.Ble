using Darp.Ble.Data;
using FluentAssertions.Numeric;

namespace Darp.Ble.Tests.TestUtils;

public static class UInt48AssertionExtensions
{
    public static NumericAssertions<UInt48> Should(this UInt48 actualValue) =>
        new UInt48Assertions(actualValue);
}
