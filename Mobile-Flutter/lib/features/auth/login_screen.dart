import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

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
import '../app_routes.dart';
import 'auth_provider.dart';

/// Do AppColors.danger (do dam) qua chim tren nen brandBlue toi cua man dang nhap, dung mot sac
/// do nhat hon rieng cho man nay de van doc ro chu "loi" ma khong lac mau ngu nghia.
const _errorColorOnDark = Color(0xFFFF8A80);

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

  @override
  void initState() {
    super.initState();
    _loadVersion();
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
        systemNavigationBarColor: AppTheme.brandBlueDark,
        systemNavigationBarIconBrightness: Brightness.light,
      ),
      child: AppScaffold(
        body: Container(
          width: double.infinity,
          height: double.infinity,
          // Nen phang mot mau (khong gradient) — dung huong flat design chung ca app, thay vi
          // do do tu brandBlue sang brandBlueDark nhu truoc.
          color: AppTheme.brandBlue,
          child: Stack(
            children: [
              // Hinh coc phong to, mo nhat, lam nen trang tri o goc duoi — cung mot bo nhan dien
              // voi icon app va splash, khong can dung them tai nguyen anh nao khac.
              Positioned(
                right: -70,
                bottom: -40,
                child: Opacity(
                  opacity: 0.08,
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
                            const SizedBox(height: 16),
                            const AppText(
                              'BrewTask',
                              variant: AppTextVariant.display,
                              fontSize: 30,
                              weight: FontWeight.w800,
                              color: AppColors.textOnPrimary,
                              letterSpacing: 1,
                            ),
                            const SizedBox(height: 2),
                            AppText(
                              'QUẢN LÝ CÔNG VIỆC',
                              variant: AppTextVariant.overline,
                              fontSize: 12,
                              color: AppColors.textOnPrimary.withValues(alpha: 0.85),
                              letterSpacing: 3,
                            ),
                            const SizedBox(height: 28),
                            AppText(
                              'CHÀO MỪNG BẠN QUAY TRỞ LẠI',
                              variant: AppTextVariant.overline,
                              align: TextAlign.center,
                              fontSize: 12,
                              // Alpha 0.8 (thay vi 0.7 truoc day) de dat toi thieu 4.5:1 tren nen
                              // brandBlue — 0.7 chi dat ~4.38:1, duoi chuan AA cho chu thuong.
                              color: AppColors.textOnPrimary.withValues(alpha: 0.8),
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
                            const SizedBox(height: 14),
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
                                color: AppColors.textOnPrimary.withValues(alpha: 0.7),
                                size: 20,
                                onPressed: () => setState(
                                    () => _obscurePassword = !_obscurePassword),
                              ),
                            ),
                            const SizedBox(height: 26),
                            // AppButton chua co kieu phu hop cho ngu canh nay: ca 3 loai
                            // (primary/secondary/outline) deu dung nen hoac vien mau
                            // brandBlue/brandBlueDark — gan nhu chim vao chinh nen xanh cua man
                            // hinh nay (tuong phan qua thap). Giu FilledButton tho, tu them vien
                            // trang mo de tach khoi nut ro voi nen, thay vi ep dung AppButton vao
                            // mot ngu canh no chua duoc thiet ke cho.
                            SizedBox(
                              width: double.infinity,
                              height: 50,
                              child: FilledButton(
                                style: FilledButton.styleFrom(
                                  backgroundColor: AppTheme.brandBlueDarker,
                                  foregroundColor: AppColors.textOnPrimary,
                                  side: BorderSide(
                                      color: Colors.white.withValues(alpha: 0.3)),
                                  shape: const StadiumBorder(),
                                  elevation: 0,
                                ),
                                onPressed: _isSubmitting ? null : _submit,
                                child: _isSubmitting
                                    ? const AppLoading(
                                        size: 20,
                                        strokeWidth: 2,
                                        color: AppColors.textOnPrimary,
                                      )
                                    : const AppText(
                                        'ĐĂNG NHẬP',
                                        variant: AppTextVariant.body,
                                        weight: FontWeight.w700,
                                        color: AppColors.textOnPrimary,
                                        letterSpacing: 1,
                                      ),
                              ),
                            ),
                            const SizedBox(height: 16),
                            // Giu TextButton tho: day la mot lien ket phu giua noi dung, khong
                            // phai nut hanh dong chinh — dung AppButton (luon to nhu nut that) se
                            // pha vo bo cuc. Tu dat minimumSize 48dp vi theme moi chi ap dung
                            // minimumSize cho Filled/OutlinedButton, chua ap dung cho TextButton.
                            TextButton(
                              style: TextButton.styleFrom(
                                  minimumSize: const Size(88, AppDimens.minTapTarget)),
                              onPressed: () => ToastService.show(
                                  'Tính năng đang được phát triển.'),
                              child: AppText(
                                'Quên mật khẩu?',
                                variant: AppTextVariant.body,
                                color: AppColors.textOnPrimary.withValues(alpha: 0.85),
                                decoration: TextDecoration.underline,
                              ),
                            ),
                            const SizedBox(height: 40),
                            AppText(
                              'Trung tâm KDGP - VNPT KHA',
                              variant: AppTextVariant.overline,
                              align: TextAlign.center,
                              fontSize: 12,
                              weight: FontWeight.w600,
                              color: AppColors.textOnPrimary.withValues(alpha: 0.6),
                              letterSpacing: 0,
                            ),
                            if (_versionLabel.isNotEmpty) ...[
                              const SizedBox(height: 2),
                              AppText(
                                _versionLabel,
                                variant: AppTextVariant.caption,
                                align: TextAlign.center,
                                fontSize: 11,
                                color: AppColors.textOnPrimary.withValues(alpha: 0.45),
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
      textColor: AppColors.textOnPrimary,
      labelColor: AppColors.textOnPrimary.withValues(alpha: 0.85),
      iconColor: AppColors.textOnPrimary.withValues(alpha: 0.7),
      // Nen mo va vien deu la lop trang tri (khong phai chu/icon) nen duoc phep giu Colors.white
      // truc tiep theo dung ngoai le cua Quy tac 2 (FLUTTER_RULES.md).
      fillColor: Colors.white.withValues(alpha: 0.14),
      borderColor: Colors.white.withValues(alpha: 0.25),
      focusedBorderColor: AppColors.textOnPrimary,
      errorColor: _errorColorOnDark,
      borderRadius: 30,
    );
  }
}
