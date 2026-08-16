import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';

/// O nhap chuan cua du an — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung
/// TextField/TextFormField truc tiep, luon qua AppTextField. Luon co nhan (labelText) — KHONG
/// dung hint lam nhan duy nhat (hint chi la vi du gia tri, không thay the nhan).
class AppTextField extends StatelessWidget {
  const AppTextField({
    super.key,
    required this.label,
    this.controller,
    this.hint,
    this.obscureText = false,
    this.maxLines = 1,
    this.minLines,
    this.keyboardType,
    this.inputFormatters,
    this.textInputAction,
    this.onChanged,
    this.onFieldSubmitted,
    this.validator,
    this.prefixIcon,
    this.suffixIcon,
    this.suffixIconWidget,
    this.readOnly = false,
    this.onTap,
    this.enabled = true,
    this.required = false,
    this.fillColor,
    this.textColor,
    this.labelColor,
    this.iconColor,
    this.borderColor,
    this.focusedBorderColor,
    this.errorColor,
    this.borderRadius,
  });

  final String label;
  final TextEditingController? controller;
  final String? hint;
  final bool obscureText;
  final int? maxLines;
  final int? minLines;
  final TextInputType? keyboardType;

  /// Rang buoc dinh dang ky tu go vao (vi du chi cho so, gioi han so ky tu) — dung khi validator
  /// (chi bao loi SAU khi go) khong du, can CHAN truoc khi ky tu sai xuat hien trong o (vi du o
  /// nhap ma OTP 6 so).
  final List<TextInputFormatter>? inputFormatters;

  final TextInputAction? textInputAction;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onFieldSubmitted;
  final FormFieldValidator<String>? validator;
  final IconData? prefixIcon;
  final IconData? suffixIcon;

  /// Widget rieng cho phan duoi o nhap (vi du nut bam an/hien mat khau) — dung khi suffixIcon
  /// (chi la icon tinh, khong bam duoc) khong du. Uu tien hon suffixIcon neu ca hai cung truyen.
  final Widget? suffixIconWidget;

  final bool readOnly;
  final VoidCallback? onTap;
  final bool enabled;

  /// Chi doi mau dau * o nhan, KHONG tu them validator — man hinh van phai tu kiem tra bat buoc.
  final bool required;

  // Cac tham so tuy chinh mau/bo goc duoi day danh cho o nhap dat tren nen KHONG phai mau trang
  // chuan (vi du man dang nhap nen xanh thuong hieu) — de trong (null) thi giu nguyen giao dien
  // mac dinh nhu truoc, khong anh huong cac man dang dung AppTextField kieu thuong (nen trang).
  final Color? fillColor;
  final Color? textColor;
  final Color? labelColor;
  final Color? iconColor;
  final Color? borderColor;
  final Color? focusedBorderColor;
  final Color? errorColor;
  final double? borderRadius;

  @override
  Widget build(BuildContext context) {
    final radius = borderRadius ?? AppDimens.radiusMd;
    final resolvedBorderColor = borderColor ?? AppColors.border;
    final resolvedFocusedBorderColor = focusedBorderColor ?? AppColors.primary;
    final resolvedErrorColor =
        errorColor ?? Theme.of(context).colorScheme.error;
    // Truong bat buoc thi doi nhan sang mau do — bao hieu ngay tu cai nhin dau, khong doi nguoi
    // dung phai bam thu moi biet thieu truong nao. labelColor tuong minh (vi du man nen mau)
    // van duoc uu tien hon, tranh vo giao dien cac man da tuy chinh mau rieng.
    final resolvedLabelColor =
        labelColor ?? (required ? AppColors.danger : null);

    return TextFormField(
      controller: controller,
      obscureText: obscureText,
      maxLines: obscureText ? 1 : maxLines,
      minLines: minLines,
      keyboardType: keyboardType,
      inputFormatters: inputFormatters,
      textInputAction: textInputAction,
      onChanged: onChanged,
      onFieldSubmitted: onFieldSubmitted,
      validator: validator,
      readOnly: readOnly,
      onTap: onTap,
      enabled: enabled,
      style: TextStyle(fontSize: 14, color: textColor ?? AppColors.textPrimary),
      cursorColor: textColor,
      decoration: InputDecoration(
        labelText: required ? '$label *' : label,
        labelStyle: resolvedLabelColor == null
            ? null
            : TextStyle(color: resolvedLabelColor),
        floatingLabelStyle: resolvedLabelColor == null
            ? null
            : TextStyle(color: resolvedLabelColor),
        hintText: hint,
        hintStyle: textColor == null
            ? null
            : TextStyle(color: textColor!.withValues(alpha: 0.7)),
        prefixIcon: prefixIcon == null
            ? null
            : Icon(prefixIcon,
                size: 20, color: iconColor ?? AppColors.textFaint),
        suffixIcon: suffixIconWidget ??
            (suffixIcon == null
                ? null
                : Icon(suffixIcon,
                    size: 20, color: iconColor ?? AppColors.textFaint)),
        filled: fillColor != null,
        fillColor: fillColor,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radius),
          borderSide: BorderSide(color: resolvedBorderColor),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radius),
          borderSide: BorderSide(color: resolvedBorderColor),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radius),
          borderSide: BorderSide(color: resolvedFocusedBorderColor, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radius),
          borderSide: BorderSide(color: resolvedErrorColor, width: 1.4),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(radius),
          borderSide: BorderSide(color: resolvedErrorColor, width: 1.4),
        ),
        errorStyle: TextStyle(color: resolvedErrorColor),
      ),
    );
  }
}
