using QRCoder;

namespace HotelBillingSystem.Services
{
    public class QrCodeService : IQrCodeService
    {
        public string GenerateQrCodeBase64(string contentUrl)
        {
            var bytes = GenerateQrCodePngBytes(contentUrl);
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }

        public byte[] GenerateQrCodePngBytes(string contentUrl)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(contentUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }
}
