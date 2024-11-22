namespace Darp.Ble.Hci.Payload;

/// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 1, Part F, 1 OVERVIEW OF ERROR CODES </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/architecture,-change-history,-and-conventions/controller-error-codes.html"/>
public enum HciCommandStatus : byte
{
    /// <summary> The command succeeded </summary>
    Success = 0x00,
    /// <summary>
    /// The Unknown HCI Command error code indicates that the Controller does not
    /// understand the HCI Command packet opcode that the Host sent. The opcode
    /// given might not correspond to any of the opcodes specified in this document,
    /// or any vendor-specific opcodes, or the command may have not been implemented.
    /// </summary>
    UnknownHciCommand = 0x01,
    /// <summary>
    /// The Unknown Connection Identifier error code indicates that a command was
    /// sent from the Host that should identify a connection, but that connection does
    /// not exist or does not identify the correct type of connection.
    /// </summary>
    UnknownConnectionIdentifier = 0x02,
    /// <summary>
    /// The Page Timeout error code indicates that a page timed out because of the Page Timeout configuration parameter. This error code shall only be used with the HCI_Remote_Name_Request and HCI_Create_Connection commands or with equivalent mechanisms when HCI is not supported
    /// </summary>
    PageTimeout = 0x04,
    /// <summary>
    /// The Authentication Failure error code indicates that pairing or authentication failed due to incorrect results
    /// in the pairing or authentication procedure. This could be due to an incorrect PIN or Link Key.
    /// </summary>
    AuthenticationFailure = 0x05,
    /// <summary>
    /// The Connection Already Exists error code indicates that an attempt was made to create a new Connection
    /// to a device when there is already a connection to this device and multiple connections
    /// to the same device are not permitted.
    /// </summary>
    ConnectionAlreadyExists = 0x0B,
    /// <summary>
    /// The Command Disallowed error code indicates that the command requested
    /// cannot be executed because the Controller is in a state where it cannot
    /// process this command at this time. This error shall not be used for command
    /// opcodes where the error code Unknown HCI Command is valid.
    /// </summary>
    CommandDisallowed = 0x0C,
    /// <summary>
    /// The Unsupported Feature Or Parameter Value error code indicates that a
    /// feature or parameter value in the HCI command is not supported. This error
    /// code shall not be used in an LMP PDU.
    /// </summary>
    UnsupportedFeatureOrParameterValue = 0x11,
    /// <summary>
    /// The Invalid HCI Command Parameters error code indicates that at least one of
    /// the HCI command parameters is invalid.
    /// This shall be used when:
    /// <list type="bullet">
    /// <item>the parameter total length is invalid.</item>
    /// <item> a command parameter is an invalid type. </item>
    /// <item> a connection identifier does not match the corresponding event. </item>
    /// <item> a parameter is odd when it is required to be even. </item>
    /// <item> a parameter is outside of the specified range. </item>
    /// <item> two or more parameter values have inconsistent values. </item>
    /// </list>
    /// Note: An invalid type can be, for example, when a SCO Connection_Handle is
    /// used where an ACL Connection_Handle is required.
    /// </summary>
    InvalidHciCommandParameters = 0x12,
    /// <summary>
    /// The Remote User Terminated Connection error code indicates that the user on the remote device either
    /// terminated the connection or stopped broadcasting packets.
    /// </summary>
    RemoteUserTerminatedConnection = 0x13,
    /// <summary>
    /// The Remote Device Terminated Connection due to Low Resources error code indicates that the remote device
    /// terminated the connection because of low resources.
    /// </summary>
    RemoteDeviceTerminatedConnectionDueToLowResources = 0x14,
    /// <summary>
    /// The Remote Device Terminated Connection due to Power Off error code indicates that the remote device
    /// terminated the connection because the device is about to power off.
    /// </summary>
    RemoteDeviceTerminatedConnectionDueToPowerOff = 0x15,
    /// <summary>
    /// The Unsupported Remote Feature error code indicates that the remote device does not support the feature
    /// associated with the issued command, LMP PDU, or Link Layer Control PDU.
    /// </summary>
    UnsupportedRemoteFeature = 0x1A,
    /// <summary>
    /// The Unspecified Error error code indicates that no other error code specified is appropriate to use.
    /// </summary>
    UnspecifiedError = 0x1F,
    /// <summary>
    /// Unsupported LMP (Link Manager Protocol) Parameter Value / Unsupported LL (Link Layer) Parameter Value
    /// </summary>
    UnsupportedParameterValue = 0x20,
    /// <summary>
    /// The Pairing With Unit Key Not Supported error code indicates that it was not possible to pair
    /// as a unit key was requested and it is not supported.
    /// </summary>
    PairingWithUnitKeyNotSupported = 0x29,
    /// <summary>
    /// The Unacceptable Connection Parameters error code indicates that the remote device either terminated
    /// the connection or rejected a request because of one or more unacceptable connection parameters.
    /// </summary>
    UnacceptableConnectionParameters = 0x3B,
}