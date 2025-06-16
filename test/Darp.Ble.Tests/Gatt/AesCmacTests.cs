using FluentAssertions;

namespace Darp.Ble.Tests.Gatt;

public sealed class AesCmacTests
{
    [Theory]
    // Test vectors from https://www.rfc-editor.org/rfc/rfc4493.html#section-4
    [InlineData("2b7e151628aed2a6abf7158809cf4f3c", "", "bb1d6929e95937287fa37d129b756746")]
    [InlineData(
        "2b7e151628aed2a6abf7158809cf4f3c",
        "6bc1bee22e409f96e93d7e117393172a",
        "070a16b46b4d4144f79bdd9dd04a287c"
    )]
    [InlineData(
        "2b7e151628aed2a6abf7158809cf4f3c",
        "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411",
        "dfa66747de9ae63030ca32611497c827"
    )]
    [InlineData(
        "2b7e151628aed2a6abf7158809cf4f3c",
        "6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710",
        "51f0bebf7e3b9d92fc49741779363cfe"
    )]
    // Test vectors from https://github.com/ircmaxell/quality-checker/blob/master/tmp/gh_18/PHP-PasswordLib-master/test/Data/Vectors/cmac-aes.sp-800-38b.test-vectors
    [InlineData("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b", "", "d17ddf46adaacde531cac483de7a9367")]
    [InlineData(
        "603deb1015ca71be2b73aef0857d77811f352c073b6108d72d9810a30914dff4",
        "",
        "028962f61b7bf89efc6b551f4667d983"
    )]
    public void ValidKeyCombinations(string keyHexString, string messageHexString, string expectedEncryptionHexString)
    {
        byte[] key = Convert.FromHexString(keyHexString);
        byte[] message = Convert.FromHexString(messageHexString);
        byte[] expectedEncryption = Convert.FromHexString(expectedEncryptionHexString);

        using var cmac = new AesCmac(key);
        byte[] encryptedMessage = cmac.Encrypt(message);

        encryptedMessage.Should().BeEquivalentTo(expectedEncryption);
    }

    [Theory]
    [InlineData("11")]
    public void InvalidKeyCombinations(string keyHexString)
    {
        byte[] key = Convert.FromHexString(keyHexString);

        Func<AesCmac> func = () => new AesCmac(key);

        func.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }
}
