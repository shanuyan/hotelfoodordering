namespace HotelBillingSystem.Services
{
    public interface IQrCodeService
    {
        string GenerateQrCodeBase64(string contentUrl);
        byte[] GenerateQrCodePngBytes(string contentUrl);
    }
}
