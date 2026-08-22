import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'auth_routes.dart';
import 'forgot_password_service.dart';

/// Số giây chờ giữa 2 lần gửi lại OTP (khớp backend)
const _resendCooldownSeconds = 60;

enum _ForgotPasswordStep { phone, otp, success }

/// Màn "Quên mật khẩu" — thiết kế đồng bộ theo chuẩn Dark Theme VS Code với LoginScreen.
class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _service = ForgotPasswordService();

  final _phoneFormKey = GlobalKey<FormState>();
  final _otpFormKey = GlobalKey<FormState>();
  final _phoneController = TextEditingController();
  final _otpController = TextEditingController();

  _ForgotPasswordStep _step = _ForgotPasswordStep.phone;
  bool _isSubmitting = false;
  int _resendSecondsLeft = 0;
  Timer? _resendTimer;

  @override
  void dispose() {
    _resendTimer?.cancel();
    _phoneController.dispose();
    _otpController.dispose();
    super.dispose();
  }

  String? _validatePhone(String? value) {
    final phone = value?.trim() ?? '';
    if (phone.isEmpty) return 'Vui lòng nhập số điện thoại';
    final digits = phone.replaceAll(RegExp(r'\D'), '');
    if (digits.length < 9 || digits.length > 11) {
      return 'Số điện thoại không hợp lệ';
    }
    return null;
  }

  String? _validateOtp(String? value) {
    final code = value?.trim() ?? '';
    if (code.isEmpty) return 'Vui lòng nhập mã OTP';
    if (!RegExp(r'^\d{6}$').hasMatch(code)) {
      return 'Mã OTP phải gồm đúng 6 chữ số';
    }
    return null;
  }

  void _startResendCountdown() {
    _resendTimer?.cancel();
    setState(() => _resendSecondsLeft = _resendCooldownSeconds);
    _resendTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }
      if (_resendSecondsLeft <= 1) {
        timer.cancel();
        setState(() => _resendSecondsLeft = 0);
      } else {
        setState(() => _resendSecondsLeft -= 1);
      }
    });
  }

  Future<void> _requestOtp() async {
    if (!(_phoneFormKey.currentState?.validate() ?? false)) return;

    setState(() => _isSubmitting = true);
    try {
      final result = await _service.requestOtp(_phoneController.text.trim());
      if (!mounted) return;
      if (result.ok) {
        setState(() => _step = _ForgotPasswordStep.otp);
        _startResendCountdown();
      } else {
        ToastService.show(result.message, type: ToastType.error);
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  Future<void> _resendOtp() async {
    if (_resendSecondsLeft > 0 || _isSubmitting) return;

    setState(() => _isSubmitting = true);
    try {
      final result = await _service.requestOtp(_phoneController.text.trim());
      if (!mounted) return;
      if (result.ok) _startResendCountdown();
      ToastService.show(
          result.message.isNotEmpty ? result.message : 'Đã gửi lại mã OTP.',
          type: result.ok ? ToastType.success : ToastType.error);
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  Future<void> _confirmOtp() async {
    if (!(_otpFormKey.currentState?.validate() ?? false)) return;

    setState(() => _isSubmitting = true);
    try {
      final result = await _service.resetPassword(
        phone: _phoneController.text.trim(),
        code: _otpController.text.trim(),
      );
      if (!mounted) return;
      if (result.ok) {
        _resendTimer?.cancel();
        setState(() => _step = _ForgotPasswordStep.success);
      } else {
        ToastService.show(result.message, type: ToastType.error);
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  void _backToPhoneStep() {
    _resendTimer?.cancel();
    _otpController.clear();
    setState(() {
      _resendSecondsLeft = 0;
      _step = _ForgotPasswordStep.phone;
    });
  }

  void _handleBack() {
    if (_isSubmitting) return;
    if (_step == _ForgotPasswordStep.otp) {
      _backToPhoneStep();
    } else {
      Nav.close(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light.copyWith(
        statusBarColor: Colors.transparent,
        systemNavigationBarColor: const Color(0xFF181818),
        systemNavigationBarIconBrightness: Brightness.light,
      ),
      child: AppScaffold(
        resizeToAvoidBottomInset: true,
        body: Container(
          width: double.infinity,
          height: double.infinity,
          color: const Color(0xFF1E1E1E),
          child: Stack(
            children: [
              Positioned(
                right: -70,
                bottom: -40,
                child: Opacity(
                  opacity: 0.05,
                  child: Image.asset('assets/images/logo_mark.png', width: 320),
                ),
              ),
              SafeArea(
                child: Column(
                  children: [
                    if (_step != _ForgotPasswordStep.success)
                      Align(
                        alignment: Alignment.topLeft,
                        child: Padding(
                          padding: const EdgeInsets.only(left: AppDimens.space8, top: AppDimens.space8),
                          child: AppIconButton(
                            icon: PhosphorIconsRegular.arrowLeft,
                            color: AppColors.textPrimary,
                            tooltip: 'Quay lại',
                            onPressed: _isSubmitting ? null : _handleBack,
                          ),
                        ),
                      ),
                    Expanded(
                      child: Center(
                        child: SingleChildScrollView(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 28, vertical: 16),
                          child: ConstrainedBox(
                            constraints: const BoxConstraints(maxWidth: 360),
                            child: _buildStepContent(),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStepContent() {
    switch (_step) {
      case _ForgotPasswordStep.phone:
        return _buildPhoneStep();
      case _ForgotPasswordStep.otp:
        return _buildOtpStep();
      case _ForgotPasswordStep.success:
        return _buildSuccessStep();
    }
  }

  Widget _header({required String heading, required String subtitle}) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Image.asset('assets/images/logo_mark.png', width: 64),
        const SizedBox(height: AppDimens.space16),
        const AppText(
          'BrewTask',
          variant: AppTextVariant.display,
          fontSize: 28,
          weight: FontWeight.w800,
          color: AppColors.textPrimary,
          letterSpacing: 1,
        ),
        const SizedBox(height: AppDimens.space4),
        const AppText(
          'QUẢN LÝ CÔNG VIỆC',
          variant: AppTextVariant.overline,
          fontSize: 12,
          color: Color(0xFF3794FF),
          letterSpacing: 3,
        ),
        const SizedBox(height: AppDimens.space24),
        AppText(
          heading.toUpperCase(),
          variant: AppTextVariant.overline,
          align: TextAlign.center,
          fontSize: 12,
          color: AppColors.textSecondary,
          letterSpacing: 1.5,
        ),
        const SizedBox(height: AppDimens.space8),
        AppText(
          subtitle,
          variant: AppTextVariant.body,
          align: TextAlign.center,
          color: AppColors.textSecondary,
          height: 1.45,
        ),
        const SizedBox(height: AppDimens.space24),
      ],
    );
  }

  // Bước 1 — nhập số điện thoại
  Widget _buildPhoneStep() {
    return Form(
      key: _phoneFormKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          _header(
            heading: 'Quên mật khẩu',
            subtitle:
                'Nhập số điện thoại đã đăng ký để nhận mã OTP khôi phục mật khẩu.',
          ),
          _ForgotPasswordField(
            controller: _phoneController,
            label: 'Số điện thoại',
            icon: PhosphorIconsRegular.phone,
            keyboardType: TextInputType.phone,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            textInputAction: TextInputAction.done,
            onFieldSubmitted: (_) => _isSubmitting ? null : _requestOtp(),
            validator: _validatePhone,
          ),
          const SizedBox(height: AppDimens.space24),
          _PrimaryDarkButton(
            label: 'GỬI MÃ OTP',
            isLoading: _isSubmitting,
            onPressed: _requestOtp,
          ),
        ],
      ),
    );
  }

  // Bước 2 — nhập mã OTP
  Widget _buildOtpStep() {
    final canResend = _resendSecondsLeft <= 0 && !_isSubmitting;

    return Form(
      key: _otpFormKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          _header(
            heading: 'Xác nhận mã OTP',
            subtitle:
                'Mã gồm 6 chữ số đã được gửi qua tin nhắn tới số điện thoại:',
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            decoration: BoxDecoration(
              color: const Color(0xFF252526),
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
              border: Border.all(color: const Color(0xFF3C3C3C)),
            ),
            child: AppText(
              _phoneController.text.trim(),
              variant: AppTextVariant.heading,
              fontSize: 16,
              align: TextAlign.center,
              color: const Color(0xFF3794FF),
              weight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: AppDimens.space16),
          _ForgotPasswordField(
            controller: _otpController,
            label: 'Mã OTP',
            icon: PhosphorIconsRegular.shieldCheck,
            keyboardType: TextInputType.number,
            inputFormatters: [
              FilteringTextInputFormatter.digitsOnly,
              LengthLimitingTextInputFormatter(6),
            ],
            textInputAction: TextInputAction.done,
            onFieldSubmitted: (_) => _isSubmitting ? null : _confirmOtp(),
            validator: _validateOtp,
          ),
          const SizedBox(height: AppDimens.space12),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const AppText(
                'Chưa nhận được mã?',
                variant: AppTextVariant.caption,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: AppDimens.space4),
              InkWell(
                onTap: canResend ? _resendOtp : null,
                borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 6),
                  child: AppText(
                    _resendSecondsLeft > 0
                        ? 'Gửi lại (${_resendSecondsLeft}s)'
                        : 'Gửi lại mã',
                    variant: AppTextVariant.caption,
                    weight: FontWeight.w700,
                    color: canResend
                        ? const Color(0xFF3794FF)
                        : AppColors.textSecondary.withValues(alpha: 0.5),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space24),
          _PrimaryDarkButton(
            label: 'XÁC NHẬN',
            isLoading: _isSubmitting,
            onPressed: _confirmOtp,
          ),
          const SizedBox(height: AppDimens.space16),
          InkWell(
            onTap: _isSubmitting ? null : _backToPhoneStep,
            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            child: const Padding(
              padding: EdgeInsets.symmetric(horizontal: 8, vertical: 8),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(PhosphorIconsRegular.pencilSimple, size: 14, color: Color(0xFF3794FF)),
                  SizedBox(width: 4),
                  AppText(
                    'Đổi số điện thoại',
                    variant: AppTextVariant.caption,
                    fontSize: 13,
                    color: Color(0xFF3794FF),
                    weight: FontWeight.w600,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // Bước 3 — báo thành công
  Widget _buildSuccessStep() {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 84,
          height: 84,
          decoration: BoxDecoration(
            color: AppColors.success.withValues(alpha: 0.15),
            shape: BoxShape.circle,
            border: Border.all(color: AppColors.success.withValues(alpha: 0.4), width: 2),
          ),
          child: const Center(
            child: PhosphorIcon(
              PhosphorIconsFill.checkCircle,
              size: 48,
              color: AppColors.success,
            ),
          ),
        ),
        const SizedBox(height: AppDimens.space24),
        const AppText(
          'Đã cấp lại mật khẩu!',
          variant: AppTextVariant.title,
          align: TextAlign.center,
          color: AppColors.textPrimary,
          weight: FontWeight.w800,
        ),
        const SizedBox(height: AppDimens.space8),
        const AppText(
          'Mật khẩu mới đã được gửi qua tin nhắn SMS tới số điện thoại của bạn. '
          'Vui lòng kiểm tra tin nhắn và đăng nhập lại.',
          variant: AppTextVariant.body,
          align: TextAlign.center,
          color: AppColors.textSecondary,
          height: 1.45,
        ),
        const SizedBox(height: AppDimens.space24),
        _PrimaryDarkButton(
          label: 'VỀ MÀN ĐĂNG NHẬP',
          isLoading: false,
          onPressed: () => Nav.to(context, AuthRoutes.login),
        ),
      ],
    );
  }
}

/// Nút bấm Primary chuẩn VS Code
class _PrimaryDarkButton extends StatelessWidget {
  const _PrimaryDarkButton({
    required this.label,
    required this.onPressed,
    this.isLoading = false,
  });

  final String label;
  final VoidCallback onPressed;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 50,
      child: FilledButton(
        style: FilledButton.styleFrom(
          backgroundColor: const Color(0xFF007ACC),
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          ),
          elevation: 0,
        ),
        onPressed: isLoading ? null : onPressed,
        child: isLoading
            ? const AppLoading(size: 20, strokeWidth: 2, color: Colors.white)
            : AppText(
                label,
                variant: AppTextVariant.body,
                weight: FontWeight.w700,
                color: Colors.white,
                letterSpacing: 1.2,
              ),
      ),
    );
  }
}

/// Ô nhập liệu chuẩn Dark Theme
class _ForgotPasswordField extends StatelessWidget {
  const _ForgotPasswordField({
    required this.controller,
    required this.label,
    required this.icon,
    this.keyboardType,
    this.inputFormatters,
    this.textInputAction,
    this.onFieldSubmitted,
    this.validator,
  });

  final TextEditingController controller;
  final String label;
  final IconData icon;
  final TextInputType? keyboardType;
  final List<TextInputFormatter>? inputFormatters;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;
  final String? Function(String?)? validator;

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      label: label,
      controller: controller,
      keyboardType: keyboardType,
      inputFormatters: inputFormatters,
      textInputAction: textInputAction,
      onFieldSubmitted: onFieldSubmitted,
      validator: validator,
      prefixIcon: icon,
      fillColor: const Color(0xFF252526),
      borderColor: const Color(0xFF3C3C3C),
      focusedBorderColor: const Color(0xFF007ACC),
      textColor: AppColors.textPrimary,
      labelColor: AppColors.textSecondary,
      iconColor: AppColors.textSecondary,
      borderRadius: AppDimens.radiusMd,
    );
  }
}

