using System.Numerics.Tensors;
using System.Security.Cryptography;

namespace Darp.Ble;

/// <summary> The AES-CMAC encryption algorithm </summary>
public sealed class AesCmac : IDisposable
{
    private readonly Aes _aes;
    private static readonly byte[] Zero = new byte[16];
    private static readonly byte[] Rb = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x87];

    /// <summary> Initializes a new encryption provider with a given key </summary>
    /// <param name="key"> The key </param>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if the key is not of length 16, 24, 32 </exception>
    public AesCmac(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentOutOfRangeException(nameof(key));
        _aes = Aes.Create();
        _aes.Key = key.ToArray();
#pragma warning disable CA5358 // CA5358: Review the usage of cipher mode 'ECB' with cryptography experts -> CMAC
        _aes.Mode = CipherMode.ECB;
#pragma warning restore CA5358 // CA5358: Review the usage of cipher mode 'ECB' with cryptography experts
        _aes.Padding = PaddingMode.None;
        _aes.BlockSize = 128;
    }

    /// <summary> Encrypt a given message </summary>
    /// <param name="message"> The message to encrypt </param>
    /// <returns> The encrypted message. Always of length 16 </returns>
    public byte[] Encrypt(ReadOnlySpan<byte> message)
    {
        ICryptoTransform encryptor = _aes.CreateEncryptor();

        // Step 1
        Span<byte> subKey1 = stackalloc byte[16];
        Span<byte> subKey2 = stackalloc byte[16];
        GenerateSubKeys(encryptor, subKey1, subKey2);

        // Step 2
        int n = (message.Length + 15) / 16;

        // Step 3
        bool lastBlockComplete;
        if (n is 0)
        {
            n = 1;
            lastBlockComplete = false;
        }
        else
        {
            lastBlockComplete = message.Length % 16 is 0;
        }

        // Step 4
        Span<byte> lastBlock = stackalloc byte[16];
        int startIndexLastBlock = 16 * (n - 1);
        if (lastBlockComplete)
        {
            TensorPrimitives.Xor(message[startIndexLastBlock..], subKey1, destination: lastBlock);
        }
        else
        {
            AddPadding(message[startIndexLastBlock..], destination: lastBlock);
            TensorPrimitives.Xor(lastBlock, subKey2, destination: lastBlock);
        }

        // Step 5
        Span<byte> xBuffer = stackalloc byte[16];

        // Step 6
        var yBuffer = new byte[16];
        for (var i = 0; i < n - 1; i++)
        {
            TensorPrimitives.Xor(xBuffer, message.Slice(i * 16, 16), destination: yBuffer);
            byte[] x = encryptor.TransformFinalBlock(yBuffer, 0, 16);
            x.CopyTo(xBuffer);
        }
        TensorPrimitives.Xor<byte>(lastBlock, xBuffer, destination: yBuffer);

        return encryptor.TransformFinalBlock(yBuffer, 0, 16);
    }

    private static void AddPadding(ReadOnlySpan<byte> partialBlock, Span<byte> destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            if (i < partialBlock.Length)
            {
                destination[i] = partialBlock[i];
            }
            else if (i == partialBlock.Length)
            {
                destination[i] = 0x80;
            }
            else
            {
                destination[i] = 0x00;
            }
        }
    }

    /// <summary> Generate the two sub keys </summary>
    /// <seealso href="https://www.rfc-editor.org/rfc/rfc4493.html#section-2.3"/>
    private static void GenerateSubKeys(
        ICryptoTransform encryptor,
        Span<byte> subKey1Destination,
        Span<byte> subKey2Destination
    )
    {
        // Step 1
        byte[] l = encryptor.TransformFinalBlock(Zero, 0, 16);

        // Step 2
        LeftShiftOneBit(l, destination: subKey1Destination);
        if ((l[0] & 0x80) != 0)
            TensorPrimitives.Xor(subKey1Destination, Rb, destination: subKey1Destination);

        // Step 3
        LeftShiftOneBit(subKey1Destination, destination: subKey2Destination);
        if ((subKey1Destination[0] & 0x80) != 0)
            TensorPrimitives.Xor(subKey2Destination, Rb, destination: subKey2Destination);
    }

    private static void LeftShiftOneBit(ReadOnlySpan<byte> input, Span<byte> destination)
    {
        byte overflow = 0;
        for (var i = 15; i >= 0; i--)
        {
            byte value = input[i];
            destination[i] = (byte)(value << 1);
            destination[i] |= overflow;
            overflow = (byte)((value & 0x80) > 0 ? 1 : 0);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _aes.Dispose();
}
