using DesktopRFID.Data.Helpers;
using DesktopRFID.Data.Interfaces;

namespace DesktopRFID.Data.Services;

public sealed class RfidService
{
    private readonly IRfidReader _reader;
    public RfidService(IRfidReader reader) => _reader = reader;
    public async Task<IReadOnlyList<TagRecord>> ScanAsync()
        => await _reader.InventoryOnceAsync();
    public async Task<bool> ProgramAsync(TagRecord current, string plate, string inFileId,
                                         byte[]? accessPwd = null)
    {
        accessPwd ??= new byte[] { 0x00, 0x00, 0x00, 0x00 };

        if (!EpcCodec.TryBuildPlateRaw_NoPad(plate, out var plateRaw) || plateRaw is null)
            throw new ArgumentException("Plaka 7-8 görünür ASCII olmalı.", nameof(plate));

        if (!EpcCodec.TryBuildBcdFlexible(inFileId, out var idBcd) || idBcd is null || idBcd.Length == 0)
            throw new ArgumentException("InFileId yalnız rakamlardan oluşmalı.", nameof(inFileId));

        var payload = EpcCodec.BuildCompositeEpcFF_WithTail(plateRaw, idBcd);
        return await _reader.WriteEpcAsync(current.EPCHex, payload, accessPwd);
    }
    public Task<byte[]?> ReadTidShortAsync(TagRecord tag) => _reader.ReadTidShortAsync(tag.EPCHex);
}