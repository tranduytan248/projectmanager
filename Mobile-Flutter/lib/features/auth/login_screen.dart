import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

import '../../core/classes/cache_manager.dart';
import '../../core/classes/route_manager.dart';
import '../../core/services/fcm_notification_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../app_routes.dart';
import 'auth_provider.dart';
import 'auth_routes.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isSubmitting = false;
  bool _obscurePassword = true;
  String _versionLabel = '';
  bool _isNotificationEnabled = false;

  @override
  void initState() {
    super.initState();
    _loadVersion();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _checkAndPromptNotification();
    });
  }

  void _checkAndPromptNotification() async {
    final isEnabled = FcmNotificationService.instance.isNotificationEnabled;
    if (mounted) setState(() => _isNotificationEnabled = isEnabled);

    final prompted = Cache.readData<bool>('fcm_notification_prompted') ?? false;
    if (!prompted && !isEnabled && mounted) {
      await _showNotificationPermissionDialog();
    }
  }

  Future<void> _showNotificationPermissionDialog() async {
    Cache.saveData('fcm_notification_prompted', true);
    await showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (dialogCtx) => Dialog(
        backgroundColor: const Color(0xFF252526),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusLg),
          side: const BorderSide(color: Color(0xFF3C3C3C)),
        ),
        child: Padding(
          padding: const EdgeInsets.all(AppDimens.space24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 64,
                height: 64,
                decoration: BoxDecoration(
                  color: const Color(0xFF007ACC).withValues(alpha: 0.15),
                  shape: BoxShape.circle,
                  border: Border.all(
                      color: const Color(0xFF007ACC).withValues(alpha: 0.3)),
                ),
                child: const Icon(
                  PhosphorIconsRegular.bellRinging,
                  color: Color(0xFF3794FF),
                  size: 32,
                ),
              ),
              const SizedBox(height: AppDimens.space16),
              const AppText(
                'Nhận thông báo công việc',
                variant: AppTextVariant.title,
                weight: FontWeight.w700,
                color: AppColors.textPrimary,
                align: TextAlign.center,
              ),
              const SizedBox(height: AppDimens.space12),
              AppText(
                'Cho phép BrewTask gửi thông báo đẩy tới thiết bị này khi có việc mới được giao, việc con hoàn thành, trao đổi mới hoặc nhắc việc sắp đến hạn.',
                variant: AppTextVariant.body,
                color: AppColors.textSecondary,
                align: TextAlign.center,
                height: 1.4,
              ),
              const SizedBox(height: AppDimens.space24),
              SizedBox(
                width: double.infinity,
                height: 44,
                child: FilledButton.icon(
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFF007ACC),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                    ),
                  ),
                  icon: const Icon(PhosphorIconsRegular.checkCircle, size: 20),
                  label: const AppText(
                    'Xác nhận bật thông báo',
                    weight: FontWeight.w700,
                    color: Colors.white,
                  ),
                  onPressed: () async {
                    Navigator.of(dialogCtx).pop();
                    final success = await FcmNotificationService.instance
                        .requestPermissionAndRegister();
                    if (mounted) {
                      setState(() => _isNotificationEnabled = success);
                      if (success) {
                        ToastService.show(
                            'Đã bật và lưu thiết bị nhận thông báo thành công!',
                            type: ToastType.success);
                      } else {
                        ToastService.show(
                            'Chưa thể cấp quyền thông báo trên thiết bị.',
                            type: ToastType.warning);
                      }
                    }
                  },
                ),
              ),
              const SizedBox(height: AppDimens.space8),
              TextButton(
                style: TextButton.styleFrom(
                  foregroundColor: AppColors.textSecondary,
                ),
                onPressed: () => Navigator.of(dialogCtx).pop(),
                child: const AppText(
                  'Để sau',
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// Doc so build THAT tu goi cai dat, khong go tay — luon khop pubspec.yaml luc build, khong
  /// bao gio lech nhu chuoi hardcode moi lan tang version ma quen sua o day.
  ///
  /// Quy uoc hien thi rieng cua du an: "1.00.xxx" — 1 la phien ban dau tien, 00 la lan cap nhat
  /// thu n (tang moi lan phat hanh mot dot thay doi), xxx la so chuc nang duoc cap nhat trong
  /// lan do. pubspec.yaml van giu dung semver (vi du "1.2.5") de cong cu Flutter doc duoc; o day
  /// chi dem lai chu so 0 cho dung khuon hien thi, khong doi gia tri.
  Future<void> _loadVersion() async {
    final info = await PackageInfo.fromPlatform();
    if (!mounted) return;

    final parts = info.version.split('.');
    final major = parts.isNotEmpty ? parts[0] : '1';
    final minor = (parts.length > 1 ? parts[1] : '0').padLeft(2, '0');
    final patch = (parts.length > 2 ? parts[2] : '0').padLeft(3, '0');

    setState(() => _versionLabel = 'v$major.$minor.$patch');
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() => _isSubmitting = true);
    try {
      final auth = context.read<AuthProvider>();
      final success = await auth.login(
          context, _usernameController.text.trim(), _passwordController.text);
      if (mounted && success) {
        await Nav.to(context, AppRoutes.dashboard);
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    // Status bar trong suot, icon mau sang — de vung status bar hoa lien vao nen gradient phia
    // duoi thay vi hien mot thanh mau toi rieng (mac dinh cua he thong) tao thanh "header" gia.
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light.copyWith(
        statusBarColor: Colors.transparent,
        systemNavigationBarColor: const Color(0xFF181818),
        systemNavigationBarIconBrightness: Brightness.light,
      ),
      child: AppScaffold(
        body: Container(
          width: double.infinity,
          height: double.infinity,
          color: const Color(0xFF1E1E1E),
          child: Stack(
            children: [
              // Hinh coc phong to, mo nhat, lam nen trang tri o goc duoi — cung mot bo nhan dien
              // voi icon app va splash, khong can dung them tai nguyen anh nao khac.
              Positioned(
                right: -70,
                bottom: -40,
                child: Opacity(
                  opacity: 0.05,
                  child: Image.asset('assets/images/logo_mark.png', width: 320),
                ),
              ),
              SafeArea(
                child: Center(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 28, vertical: 32),
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 360),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Image.asset('assets/images/logo_mark.png',
                                width: 64),
                            const SizedBox(height: AppDimens.space16),
                            const AppText(
                              'BrewTask',
                              variant: AppTextVariant.display,
                              fontSize: 30,
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
                              'CHÀO MỪNG BẠN QUAY TRỞ LẠI',
                              variant: AppTextVariant.overline,
                              align: TextAlign.center,
                              fontSize: 12,
                              color: AppColors.textSecondary,
                              letterSpacing: 1.5,
                            ),
                            const SizedBox(height: 24),
                            _LoginField(
                              controller: _usernameController,
                              label: 'Tài khoản',
                              icon: PhosphorIconsRegular.user,
                              textInputAction: TextInputAction.next,
                              validator: (value) =>
                                  (value == null || value.trim().isEmpty)
                                      ? 'Nhập tài khoản'
                                      : null,
                            ),
                            const SizedBox(height: AppDimens.space16),
                            _LoginField(
                              controller: _passwordController,
                              label: 'Mật khẩu',
                              icon: PhosphorIconsRegular.lockSimple,
                              obscureText: _obscurePassword,
                              textInputAction: TextInputAction.done,
                              onFieldSubmitted: (_) =>
                                  _isSubmitting ? null : _submit(),
                              validator: (value) =>
                                  (value == null || value.isEmpty)
                                      ? 'Nhập mật khẩu'
                                      : null,
                              suffixIconWidget: AppIconButton(
                                icon: _obscurePassword
                                    ? PhosphorIconsRegular.eyeSlash
                                    : PhosphorIconsRegular.eye,
                                color: AppColors.textSecondary,
                                size: 20,
                                onPressed: () => setState(
                                    () => _obscurePassword = !_obscurePassword),
                              ),
                            ),
                            const SizedBox(height: AppDimens.space24),
                            SizedBox(
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
                                onPressed: _isSubmitting ? null : _submit,
                                child: _isSubmitting
                                    ? const AppLoading(
                                        size: 20,
                                        strokeWidth: 2,
                                        color: Colors.white,
                                      )
                                    : const AppText(
                                        'ĐĂNG NHẬP',
                                        variant: AppTextVariant.body,
                                        weight: FontWeight.w700,
                                        color: Colors.white,
                                        letterSpacing: 1.2,
                                      ),
                              ),
                            ),
                            const SizedBox(height: 16),
                            TextButton(
                              onPressed: () =>
                                  Nav.toNamed(context, AuthRoutes.forgotPassword),
                              style: TextButton.styleFrom(
                                  foregroundColor: const Color(0xFF3794FF),
                                  minimumSize:
                                      const Size(88, AppDimens.minTapTarget)),
                              child: const AppText(
                                'Quên mật khẩu?',
                                variant: AppTextVariant.body,
                                color: Color(0xFF3794FF),
                                decoration: TextDecoration.underline,
                              ),
                            ),
                            const SizedBox(height: AppDimens.space16),
                            // Thẻ trạng thái Thông báo Push Notification trên thiết bị
                            InkWell(
                              onTap: _showNotificationPermissionDialog,
                              borderRadius:
                                  BorderRadius.circular(AppDimens.radiusMd),
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: AppDimens.space12,
                                    vertical: AppDimens.space8),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF252526),
                                  borderRadius:
                                      BorderRadius.circular(AppDimens.radiusMd),
                                  border: Border.all(
                                    color: _isNotificationEnabled
                                        ? const Color(0xFF388A34)
                                            .withValues(alpha: 0.5)
                                        : const Color(0xFF3C3C3C),
                                  ),
                                ),
                                child: Row(
                                  children: [
                                    Icon(
                                      _isNotificationEnabled
                                          ? PhosphorIconsRegular
                                              .bellSimpleRinging
                                          : PhosphorIconsRegular.bellSlash,
                                      size: 18,
                                      color: _isNotificationEnabled
                                          ? const Color(0xFF89D185)
                                          : const Color(0xFFCCA700),
                                    ),
                                    const SizedBox(width: AppDimens.space8),
                                    Expanded(
                                      child: AppText(
                                        _isNotificationEnabled
                                            ? 'Thông báo: Đã bật trên thiết bị này'
                                            : 'Thông báo: Chưa bật trên thiết bị',
                                        variant: AppTextVariant.caption,
                                        fontSize: 11,
                                        color: _isNotificationEnabled
                                            ? const Color(0xFF89D185)
                                            : AppColors.textSecondary,
                                      ),
                                    ),
                                    Container(
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 8, vertical: 3),
                                      decoration: BoxDecoration(
                                        color: _isNotificationEnabled
                                            ? const Color(0xFF388A34)
                                                .withValues(alpha: 0.2)
                                            : const Color(0xFF007ACC),
                                        borderRadius: BorderRadius.circular(4),
                                        border: Border.all(
                                          color: _isNotificationEnabled
                                              ? const Color(0xFF388A34)
                                              : Colors.transparent,
                                        ),
                                      ),
                                      child: AppText(
                                        _isNotificationEnabled
                                            ? 'Đã lưu'
                                            : 'Bật ngay',
                                        fontSize: 10,
                                        weight: FontWeight.w700,
                                        color: _isNotificationEnabled
                                            ? const Color(0xFF89D185)
                                            : Colors.white,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            const SizedBox(height: AppDimens.space24),
                            AppText(
                              'Trung tâm KDGP - VNPT KHA',
                              variant: AppTextVariant.overline,
                              align: TextAlign.center,
                              fontSize: 12,
                              weight: FontWeight.w600,
                              color: AppColors.textOnPrimary
                                  .withValues(alpha: 0.6),
                              letterSpacing: 0,
                            ),
                            if (_versionLabel.isNotEmpty) ...[
                              const SizedBox(height: AppDimens.space4),
                              AppText(
                                _versionLabel,
                                variant: AppTextVariant.caption,
                                align: TextAlign.center,
                                fontSize: 11,
                                color: AppColors.textOnPrimary
                                    .withValues(alpha: 0.45),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// O nhap bo tron kieu "pill" tren nen toi, dung chung cho ca tai khoan lan mat khau de hai o
/// luon giong het nhau ve kieu dang, chi khac nhan/icon/thuoc tinh. Boc AppTextField voi bo mau
/// rieng cho nen brandBlue thay vi lap lai tung tham so mau o hai noi goi.
class _LoginField extends StatelessWidget {
  const _LoginField({
    required this.controller,
    required this.label,
    required this.icon,
    this.obscureText = false,
    this.textInputAction,
    this.onFieldSubmitted,
    this.validator,
    this.suffixIconWidget,
  });

  final TextEditingController controller;
  final String label;
  final IconData icon;
  final bool obscureText;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;
  final String? Function(String?)? validator;
  final Widget? suffixIconWidget;

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      label: label,
      controller: controller,
      obscureText: obscureText,
      textInputAction: textInputAction,
      onFieldSubmitted: onFieldSubmitted,
      validator: validator,
      prefixIcon: icon,
      suffixIconWidget: suffixIconWidget,
      textColor: AppColors.textPrimary,
      labelColor: AppColors.textSecondary,
      iconColor: AppColors.textSecondary,
      fillColor: AppColors.surfaceVariant,
      borderColor: AppColors.border,
      focusedBorderColor: AppColors.primary,
      errorColor: AppColors.danger,
      borderRadius: AppDimens.radiusMd,
    );
  }
}
