using Darp.Ble.Data;
using FluentAssertions.Numeric;

namespace Darp.Ble.Tests.TestUtils;

public sealed class UInt48Assertions(UInt48 value) : NumericAssertions<UInt48>(value);
