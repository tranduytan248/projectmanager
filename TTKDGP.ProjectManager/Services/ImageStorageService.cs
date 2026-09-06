using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>
    /// Dịch vụ quản lý và tối ưu hóa hình ảnh tải lên máy chủ nội bộ.
    /// Tự động nén dung lượng, thu nhỏ kích thước hợp lý (giữ nguyên độ sắc nét)
    /// và xử lý hướng xoay EXIF, giúp tiết kiệm đến 90% dung lượng đĩa cứng máy chủ.
    /// </summary>
    public static class ImageStorageService
    {
        private const int MaxDimension = 1600; // Giới hạn chiều rộng/dài tối đa phù hợp cho hiển thị web/mobile
        private const long JpegQuality = 82L;  // Mức chất lượng nén ảnh JPEG tối ưu (mắt thường không phân biệt được)

        /// <summary>
        /// Xử lý nén, tối ưu hóa và lưu ảnh vào thư mục máy chủ.
        /// </summary>
        /// <param name="inputStream">Luồng dữ liệu ảnh tải lên</param>
        /// <param name="originalFileName">Tên file gốc</param>
        /// <param name="contentType">MIME type</param>
        /// <returns>Đường dẫn URL ảnh tương đối để chèn vào nội dung</returns>
        public static async Task<string> SaveAndOptimizeImageAsync(Stream inputStream, string originalFileName, string contentType)
        {
            if (inputStream == null || inputStream.Length == 0)
            {
                throw new ArgumentException("Dữ liệu file ảnh rỗng.", nameof(inputStream));
            }

            // Đọc toàn bộ luồng vào MemoryStream
            var ms = new MemoryStream();
            await inputStream.CopyToAsync(ms);
            ms.Position = 0;

            var ext = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? ".png";
            var now = DateTime.UtcNow;
            var subFolder = string.Format("task_images/{0:yyyyMM}", now);
            var relativeDir = "/Uploads/" + subFolder;
            var physicalDir = HostingEnvironment.MapPath("~" + relativeDir);
            if (string.IsNullOrWhiteSpace(physicalDir))
            {
                var asmDir = Path.GetDirectoryName(typeof(ImageStorageService).Assembly.Location) ?? "";
                var rootDir = asmDir.EndsWith("\\bin", StringComparison.OrdinalIgnoreCase)
                    ? Directory.GetParent(asmDir).FullName
                    : asmDir;
                physicalDir = Path.Combine(rootDir, relativeDir.TrimStart('/').Replace('/', '\\'));
            }

            if (!Directory.Exists(physicalDir))
            {
                Directory.CreateDirectory(physicalDir);
            }

            var fileId = Guid.NewGuid().ToString("N");

            // Thử xử lý nén và tối ưu hóa bằng System.Drawing
            try
            {
                using (var originalImage = Image.FromStream(ms))
                {
                    // Tự động xoay ảnh đúng chiều theo EXIF (rất quan trọng với ảnh chụp từ điện thoại)
                    FixExifOrientation(originalImage);

                    int origW = originalImage.Width;
                    int origH = originalImage.Height;

                    // Tính toán kích thước mới nếu ảnh vượt quá MaxDimension
                    int newW = origW;
                    int newH = origH;

                    if (origW > MaxDimension || origH > MaxDimension)
                    {
                        if (origW >= origH)
                        {
                            newW = MaxDimension;
                            newH = (int)Math.Round((double)origH * MaxDimension / origW);
                        }
                        else
                        {
                            newH = MaxDimension;
                            newW = (int)Math.Round((double)origW * MaxDimension / origH);
                        }
                    }

                    // Kiểm tra xem ảnh có trong suốt (alpha channel) không
                    bool hasAlpha = ImageHasAlpha(originalImage);

                    // Với ảnh chụp màn hình dán vào hoặc ảnh lớn không cần nền trong suốt:
                    // chuyển sang JPEG để nén dung lượng từ 3-5MB xuống chỉ còn 100-250KB!
                    bool saveAsJpeg = !hasAlpha || ext == ".jpg" || ext == ".jpeg";
                    var targetExt = saveAsJpeg ? ".jpg" : ".png";
                    var fileName = fileId + targetExt;
                    var physicalPath = Path.Combine(physicalDir, fileName);

                    using (var targetBitmap = new Bitmap(newW, newH, PixelFormat.Format32bppArgb))
                    {
                        using (var graphics = Graphics.FromImage(targetBitmap))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            // Nền trắng cho ảnh JPG nếu chuyển đổi từ PNG
                            if (saveAsJpeg)
                            {
                                graphics.Clear(Color.White);
                            }

                            graphics.DrawImage(originalImage, new Rectangle(0, 0, newW, newH));
                        }

                        if (saveAsJpeg)
                        {
                            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
                            targetBitmap.Save(physicalPath, jpegEncoder, encoderParams);
                        }
                        else
                        {
                            targetBitmap.Save(physicalPath, ImageFormat.Png);
                        }
                    }

                    return relativeDir + "/" + fileName;
                }
            }
            catch
            {
                // Fallback nếu System.Drawing không giải mã được định dạng lạ (ví dụ WebP hoặc SVG):
                // Lưu trực tiếp file nhị phân gốc
                ms.Position = 0;
                var fallbackName = fileId + ext;
                var fallbackPath = Path.Combine(physicalDir, fallbackName);
                File.WriteAllBytes(fallbackPath, ms.ToArray());
                return relativeDir + "/" + fallbackName;
            }
            finally
            {
                ms.Dispose();
            }
        }

        private static bool ImageHasAlpha(Image img)
        {
            if (img.PixelFormat == PixelFormat.Format32bppArgb ||
                img.PixelFormat == PixelFormat.Format32bppPArgb ||
                img.PixelFormat == PixelFormat.Format16bppArgb1555 ||
                img.PixelFormat == PixelFormat.Format64bppArgb ||
                img.PixelFormat == PixelFormat.Format64bppPArgb)
            {
                return true;
            }
            return false;
        }

        private static void FixExifOrientation(Image img)
        {
            const int ExifOrientationTag = 0x0112;
            if (!img.PropertyIdList.Contains(ExifOrientationTag)) return;

            try
            {
                var prop = img.GetPropertyItem(ExifOrientationTag);
                int orientation = BitConverter.ToUInt16(prop.Value, 0);

                switch (orientation)
                {
                    case 2:
                        img.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                    case 3:
                        img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 4:
                        img.RotateFlip(RotateFlipType.Rotate180FlipX);
                        break;
                    case 5:
                        img.RotateFlip(RotateFlipType.Rotate90FlipX);
                        break;
                    case 6:
                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 7:
                        img.RotateFlip(RotateFlipType.Rotate270FlipX);
                        break;
                    case 8:
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }

                // Xoá tag orientation sau khi đã xoay để không bị xoay lần 2
                img.RemovePropertyItem(ExifOrientationTag);
            }
            catch
            {
                // Bỏ qua nếu lỗi đọc EXIF
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return codecs.FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
        }
    }
}
