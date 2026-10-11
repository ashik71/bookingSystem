using System.Security.Cryptography;
using System.Text;

namespace SlotBook.SharedKernel.Domain;

/// <summary>
/// Name-based ids (RFC 9562 version 5, AD-8). The same namespace and name always give the same id.
/// Changing <see cref="SlotBookNamespace"/> or a name format orphans stored data.
/// </summary>
public static class DeterministicId
{
    /// <summary>The one namespace for every derived id. Names carry a typed prefix, e.g. <c>tenant:</c>.</summary>
    public static readonly Guid SlotBookNamespace = new("7c1d5a0e-3b64-4f1a-9d2e-5e8a41c6b7f3");

    public static Guid From(Guid namespaceId, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[16 + nameBytes.Length];
        namespaceId.TryWriteBytes(input, bigEndian: true, out _);
        nameBytes.CopyTo(input, 16);

#pragma warning disable CA5350 // SHA-1 is fixed by RFC 9562 v5; this is identity, not security.
        var hash = SHA1.HashData(input);
#pragma warning restore CA5350

        var bytes = hash.AsSpan(0, 16).ToArray();
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }

    /// <summary>The tenant id for a configured slug (AD-5).</summary>
    public static Guid ForTenant(string slug) => From(SlotBookNamespace, "tenant:" + slug);
}
