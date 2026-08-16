import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
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

/// So giay cho giua hai lan gui OTP — khop dung cooldown that cua backend (PasswordResetService).
const _resendCooldownSeconds = 60;

enum _ForgotPasswordStep { phone, otp, success }

/// Man "Quen mat khau" — 3 buoc noi bo trong CUNG mot man hinh (khong tach route rieng):
/// 1) nhap so dien thoai -> gui OTP; 2) nhap OTP -> backend tu sinh mat khau moi va gui qua SMS;
/// 3) bao thanh cong, quay ve man Dang nhap. Cung ngon ngu hinh anh voi LoginScreen (man mo man
/// nay ra) — nen brandBlue toan man, o nhap kieu "pill" toi mau, cung logo/wordmark.
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
    if (!RegExp(r'^\d{6}$').hasMatch(code))
      return 'Mã OTP phải gồm đúng 6 chữ số';
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
        ToastService.show(result.message);
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  /// Gui lai OTP luc dang o Buoc 2 — khong doi buoc (man khong doi), chi khoi dong lai dem nguoc
  /// va bao ket qua qua toast vi khong co thay doi giao dien nao khac de nguoi dung nhan biet.
  Future<void> _resendOtp() async {
    if (_resendSecondsLeft > 0 || _isSubmitting) return;

    setState(() => _isSubmitting = true);
    try {
      final result = await _service.requestOtp(_phoneController.text.trim());
      if (!mounted) return;
      if (result.ok) _startResendCountdown();
      ToastService.show(
          result.message.isNotEmpty ? result.message : 'Đã gửi lại mã OTP.');
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
        ToastService.show(result.message);
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  /// Quay lai Buoc 1 de sua so dien thoai go nham — khong phai dieu huong route, chi doi trang
  /// thai noi bo, huy dem nguoc va xoa ma OTP da go (khong con hop le cho so moi).
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
    // Status bar trong suot, icon mau sang — dong bo voi LoginScreen (xem giai thich trong
    // login_screen.dart) de chuyen man khong bi giat mau status bar.
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light.copyWith(
        statusBarColor: Colors.transparent,
        systemNavigationBarColor: AppTheme.brandBlueDark,
        systemNavigationBarIconBrightness: Brightness.light,
      ),
      child: AppScaffold(
        // Cho ban phim tu day len khi go OTP/so dien thoai khong bi che noi dung phia duoi.
        resizeToAvoidBottomInset: true,
        body: Container(
          width: double.infinity,
          height: double.infinity,
          color: AppTheme.brandBlue,
          child: Stack(
            children: [
              Positioned(
                right: -70,
                bottom: -40,
                child: Opacity(
                  opacity: 0.08,
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
                          padding:
                              const EdgeInsets.only(left: AppDimens.space8),
                          child: AppIconButton(
                            icon: PhosphorIconsRegular.arrowLeft,
                            color: AppColors.textOnPrimary,
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
        Image.asset('assets/images/logo_mark.png', width: 56),
        const SizedBox(height: AppDimens.space16),
        AppText(
          heading,
          variant: AppTextVariant.title,
          align: TextAlign.center,
          color: AppColors.textOnPrimary,
          weight: FontWeight.w800,
        ),
        const SizedBox(height: AppDimens.space8),
        AppText(
          subtitle,
          variant: AppTextVariant.body,
          align: TextAlign.center,
          color: AppColors.textOnPrimary.withValues(alpha: 0.85),
          height: 1.45,
        ),
        const SizedBox(height: AppDimens.space24),
      ],
    );
  }

  // Buoc 1 — nhap so dien thoai.
  Widget _buildPhoneStep() {
    return Form(
      key: _phoneFormKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          _header(
            heading: 'Quên mật khẩu',
            subtitle:
                'Nhập số điện thoại đã đăng ký để nhận mã xác nhận qua tin nhắn.',
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

  // Buoc 2 — nhap ma OTP, co dem nguoc gui lai va duong quay lai sua so dien thoai.
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
                'Mã gồm 6 chữ số vừa được gửi qua tin nhắn tới số điện thoại:',
          ),
          AppText(
            _phoneController.text.trim(),
            variant: AppTextVariant.heading,
            align: TextAlign.center,
            color: AppColors.textOnPrimary,
            weight: FontWeight.w700,
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
              AppText(
                'Chưa nhận được mã?',
                variant: AppTextVariant.caption,
                color: AppColors.textOnPrimary.withValues(alpha: 0.75),
              ),
              const SizedBox(width: AppDimens.space4),
              // Giu TextButton tho cung ly do da giai thich trong login_screen.dart (lien ket phu,
              // khong phai nut hanh dong chinh) — tu dat minimumSize 48dp du vung chu ngan.
              TextButton(
                style: TextButton.styleFrom(
                  minimumSize: const Size(88, AppDimens.minTapTarget),
                  padding: const EdgeInsets.symmetric(horizontal: 4),
                ),
                onPressed: canResend ? _resendOtp : null,
                child: AppText(
                  _resendSecondsLeft > 0
                      ? 'Gửi lại (${_resendSecondsLeft}s)'
                      : 'Gửi lại mã',
                  variant: AppTextVariant.caption,
                  weight: FontWeight.w700,
                  color: canResend
                      ? AppColors.textOnPrimary
                      : AppColors.textOnPrimary.withValues(alpha: 0.45),
                  decoration: TextDecoration.underline,
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
          const SizedBox(height: AppDimens.space12),
          TextButton(
            style: TextButton.styleFrom(
                minimumSize: const Size(88, AppDimens.minTapTarget)),
            onPressed: _isSubmitting ? null : _backToPhoneStep,
            child: AppText(
              'Sửa số điện thoại',
              variant: AppTextVariant.body,
              color: AppColors.textOnPrimary.withValues(alpha: 0.85),
              decoration: TextDecoration.underline,
            ),
          ),
        ],
      ),
    );
  }

  // Buoc 3 — bao thanh cong, dieu huong ve man Dang nhap.
  Widget _buildSuccessStep() {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 84,
          height: 84,
          decoration: BoxDecoration(
            // Nen mo va chinh icon deu la lop trang tri tren nen brandBlue, khong phai chu/du lieu
            // — duoc phep dung mau tho theo dung ngoai le da ap dung trong login_screen.dart.
            color: Colors.white.withValues(alpha: 0.14),
            shape: BoxShape.circle,
          ),
          child: const Center(
            child: PhosphorIcon(
              PhosphorIconsFill.checkCircle,
              size: 48,
              color: AppColors.textOnPrimary,
            ),
          ),
        ),
        const SizedBox(height: AppDimens.space24),
        const AppText(
          'Đã gửi mật khẩu mới',
          variant: AppTextVariant.title,
          align: TextAlign.center,
          color: AppColors.textOnPrimary,
          weight: FontWeight.w800,
        ),
        const SizedBox(height: AppDimens.space8),
        AppText(
          'Mật khẩu mới đã được gửi qua tin nhắn tới số điện thoại của bạn. '
          'Vui lòng kiểm tra tin nhắn và đăng nhập lại.',
          variant: AppTextVariant.body,
          align: TextAlign.center,
          color: AppColors.textOnPrimary.withValues(alpha: 0.9),
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

/// Nut chinh mau dam tren nen brandBlue — dung LAI dung kieu nut cua LoginScreen (xem comment giai
/// thich trong login_screen.dart._LoginScreenState.build ve viec AppButton chua co bien the phu
/// hop cho nen mau nay). Factor rieng thanh widget vi man nay dung lai kieu nut nay o ca 3 buoc.
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
          backgroundColor: AppTheme.brandBlueDarker,
          foregroundColor: AppColors.textOnPrimary,
          side: BorderSide(color: Colors.white.withValues(alpha: 0.3)),
          shape: const StadiumBorder(),
          elevation: 0,
        ),
        // Khoa nut khi dang xu ly de chong bam kep tao 2 yeu cau OTP/reset cung luc.
        onPressed: isLoading ? null : onPressed,
        child: isLoading
            ? const AppLoading(
                size: 20, strokeWidth: 2, color: AppColors.textOnPrimary)
            : AppText(
                label,
                variant: AppTextVariant.body,
                weight: FontWeight.w700,
                color: AppColors.textOnPrimary,
                letterSpacing: 1,
              ),
      ),
    );
  }
}

/// Tuong duong _LoginField cua login_screen.dart — khong import duoc vi la class private cua file
/// khac, dung lai dung kieu dang (o nhap bo tron "pill" tren nen toi) de hai man dong bo tuyet
/// doi, chi them keyboardType/inputFormatters ma man Dang nhap khong can toi.
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
      textColor: AppColors.textOnPrimary,
      labelColor: AppColors.textOnPrimary.withValues(alpha: 0.85),
      iconColor: AppColors.textOnPrimary.withValues(alpha: 0.7),
      // Nen mo va vien deu la lop trang tri (khong phai chu/icon) nen duoc phep giu Colors.white
      // truc tiep theo dung ngoai le cua Quy tac 2 (FLUTTER_RULES.md) — giong het _LoginField.
      fillColor: Colors.white.withValues(alpha: 0.14),
      borderColor: Colors.white.withValues(alpha: 0.25),
      focusedBorderColor: AppColors.textOnPrimary,
      errorColor: AppColors.errorOnDark,
      borderRadius: 30,
    );
  }
}
