namespace Darp.Ble.Data.AssignedNumbers;

/// <summary> These are the assigned ranges of the appearance categories </summary>
public enum AppearanceValues
{
    /// <summary> Unknown </summary>
    /// <value> Category: 0x000 </value>
    Unknown = 0x0000,

    /// <summary> Phone </summary>
    /// <value> Category: 0x001 </value>
    Phone = 0x0040,

    /// <summary> Computer </summary>
    /// <value> Category: 0x002 </value>
    Computer = 0x0080,

    /// <summary> Desktop Workstation </summary>
    /// <value> Category: 0x002, Subcategory: 0x01 </value>
    DesktopWorkstation = 0x0081,
}
