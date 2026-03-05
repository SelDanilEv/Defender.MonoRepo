using System.Security.Cryptography;

namespace Defender.PersonalFoodAdviser.Application.Common.Helpers;

internal static class ImageHashHelper
{
    public static string ComputeSha256(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Convert.ToHexString(SHA256.HashData(data));
    }
}
