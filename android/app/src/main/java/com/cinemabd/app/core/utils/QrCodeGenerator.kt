package com.cinemabd.app.core.utils

import android.graphics.Bitmap
import android.graphics.Color
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel

object QrCodeGenerator {
    /**
     * Sinh ảnh QR Code bitmap từ chuỗi nội dung (HMAC signed payload).
     * @param content Nội dung chuỗi cần encode vào QR
     * @param sizePx Kích thước vuông của ảnh bitmap trả về tính theo pixel (mặc định 512px)
     */
    fun generateQrCodeBitmap(content: String, sizePx: Int = 512): Bitmap? {
        if (content.isBlank()) return null
        return try {
            val hints = mapOf(
                EncodeHintType.CHARACTER_SET to "UTF-8",
                EncodeHintType.ERROR_CORRECTION to ErrorCorrectionLevel.M,
                EncodeHintType.MARGIN to 1 // Padding viền trắng (quiet zone) rõ nét để camera dễ nhận diện
            )
            val writer = QRCodeWriter()
            val bitMatrix = writer.encode(content, BarcodeFormat.QR_CODE, sizePx, sizePx, hints)
            val bitmap = Bitmap.createBitmap(sizePx, sizePx, Bitmap.Config.ARGB_8888)
            for (x in 0 until sizePx) {
                for (y in 0 until sizePx) {
                    bitmap.setPixel(x, y, if (bitMatrix[x, y]) Color.BLACK else Color.WHITE)
                }
            }
            bitmap
        } catch (e: Exception) {
            null
        }
    }

    /**
     * Alias tương thích ngược cho các màn hình hiện tại
     */
    fun generateQrBitmap(content: String, size: Int = 512): Bitmap? {
        return generateQrCodeBitmap(content, size)
    }
}
