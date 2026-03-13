namespace Concept2.Protocol.Csafe;

/// <summary>
/// Contains all CSAFE protocol constants including frame flags, maximum frame size,
/// and status byte masks.
/// </summary>
public static class CsafeConstants
{
    /// <summary>Start flag for extended CSAFE frames.</summary>
    public const byte Extended_Frame_Start_Flag = 0xF0;

    /// <summary>Start flag for standard CSAFE frames.</summary>
    public const byte Standard_Frame_Start_Flag = 0xF1;

    /// <summary>Stop flag indicating end of a CSAFE frame.</summary>
    public const byte Stop_Frame_Flag = 0xF2;

    /// <summary>Byte-stuffing escape flag for data bytes in the range 0xF0–0xF3.</summary>
    public const byte Byte_Stuffing_Flag = 0xF3;

    /// <summary>Maximum CSAFE frame size in bytes.</summary>
    public const int MaxFrameSize = 96;

    /// <summary>Mask to extract the frame count from the status byte.</summary>
    public const byte StatusFrameCountMask = 0x03;

    /// <summary>Mask to extract the previous frame status from the status byte.</summary>
    public const byte StatusPreviousFrameMask = 0x30;

    /// <summary>Mask to extract the machine state from the status byte.</summary>
    public const byte StatusMachineStateMask = 0x0F;

    /// <summary>Bit shift for the previous-frame status field.</summary>
    public const int StatusPreviousFrameShift = 4;

    /// <summary>Mask for extracting the lower 2 bits during byte stuffing.</summary>
    public const byte StuffingMask = 0x03;

    /// <summary>Threshold value that separates long and short commands (0x80 = 128).</summary>
    /// <remarks>Commands below this threshold are long commands, at or above are short commands.</remarks>
    public const byte LongCommandThreshold = 0x80;

    /// <summary>
    /// Determines whether a command byte represents a long command.
    /// Long commands have their length byte before the actual command data.
    /// </summary>
    /// <param name="commandByte">The command byte to check.</param>
    /// <returns><c>true</c> if the command is a long command (less than 0x80); otherwise, <c>false</c>.</returns>
    public static bool IsLongCommand(byte commandByte) => commandByte < LongCommandThreshold;
}
