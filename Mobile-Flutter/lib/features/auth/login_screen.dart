import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/utils/toast_service.dart';
import '../app_routes.dart';
import 'auth_provider.dart';

/// Do AppTheme.statusDanger (do dam) qua chim tren nen brandBlue toi cua man dang nhap, dung
/// mot sac do nhat hon rieng cho man nay de van doc ro chu "loi" ma khong lac mau ngu nghia.
const _errorColor = Color(0xFFFF8A80);

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
      child: Scaffold(
        // Ve tran man hinh (khong de he thong tu to mau sau SafeArea) de nen mau phu het ca
        // vung status bar, giong mau thiet ke.
        extendBodyBehindAppBar: true,
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
                            const Text(
                              'BrewTask',
                              style: TextStyle(
                                fontSize: 30,
                                fontWeight: FontWeight.w800,
                                color: Colors.white,
                                letterSpacing: 1,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              'QUẢN LÝ CÔNG VIỆC',
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                                color: Colors.white.withValues(alpha: 0.85),
                                letterSpacing: 3,
                              ),
                            ),
                            const SizedBox(height: 28),
                            Text(
                              'CHÀO MỪNG BẠN QUAY TRỞ LẠI',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                                color: Colors.white.withValues(alpha: 0.7),
                                letterSpacing: 1.5,
                              ),
                            ),
                            const SizedBox(height: 24),
                            _PillField(
                              controller: _usernameController,
                              hint: 'Tài khoản',
                              icon: PhosphorIconsRegular.user,
                              textInputAction: TextInputAction.next,
                              validator: (value) =>
                                  (value == null || value.trim().isEmpty)
                                      ? 'Nhập tài khoản'
                                      : null,
                            ),
                            const SizedBox(height: 14),
                            _PillField(
                              controller: _passwordController,
                              hint: 'Mật khẩu',
                              icon: PhosphorIconsRegular.lockSimple,
                              obscureText: _obscurePassword,
                              textInputAction: TextInputAction.done,
                              onFieldSubmitted: (_) =>
                                  _isSubmitting ? null : _submit(),
                              validator: (value) =>
                                  (value == null || value.isEmpty)
                                      ? 'Nhập mật khẩu'
                                      : null,
                              suffixIcon: IconButton(
                                icon: PhosphorIcon(
                                  _obscurePassword
                                      ? PhosphorIconsRegular.eyeSlash
                                      : PhosphorIconsRegular.eye,
                                  color: Colors.white70,
                                  size: 20,
                                ),
                                onPressed: () => setState(
                                    () => _obscurePassword = !_obscurePassword),
                              ),
                            ),
                            const SizedBox(height: 26),
                            SizedBox(
                              width: double.infinity,
                              height: 50,
                              child: FilledButton(
                                style: FilledButton.styleFrom(
                                  backgroundColor: AppTheme.brandBlueDarker,
                                  foregroundColor: Colors.white,
                                  shape: const StadiumBorder(),
                                  elevation: 0,
                                ),
                                onPressed: _isSubmitting ? null : _submit,
                                child: _isSubmitting
                                    ? const SizedBox(
                                        height: 20,
                                        width: 20,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: Colors.white,
                                        ),
                                      )
                                    : const Text(
                                        'ĐĂNG NHẬP',
                                        style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            letterSpacing: 1),
                                      ),
                              ),
                            ),
                            const SizedBox(height: 16),
                            TextButton(
                              onPressed: () => ToastService.show(
                                  'Tính năng đang được phát triển.'),
                              child: Text(
                                'Quên mật khẩu?',
                                style: TextStyle(
                                  color: Colors.white.withValues(alpha: 0.85),
                                  decoration: TextDecoration.underline,
                                  decorationColor:
                                      Colors.white.withValues(alpha: 0.85),
                                ),
                              ),
                            ),
                            const SizedBox(height: 40),
                            Text(
                              'Trung tâm KDGP - VNPT KHA',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                                color: Colors.white.withValues(alpha: 0.6),
                              ),
                            ),
                            if (_versionLabel.isNotEmpty) ...[
                              const SizedBox(height: 2),
                              Text(
                                _versionLabel,
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 11,
                                  color: Colors.white.withValues(alpha: 0.45),
                                ),
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

/// O nhap bo tron kieu "pill", nen trang mo — dung chung cho ca tai khoan lan mat khau de hai
/// o luon giong het nhau ve kieu dang, chi khac icon/thuoc tinh.
class _PillField extends StatelessWidget {
  const _PillField({
    required this.controller,
    required this.hint,
    required this.icon,
    this.obscureText = false,
    this.textInputAction,
    this.onFieldSubmitted,
    this.validator,
    this.suffixIcon,
  });

  final TextEditingController controller;
  final String hint;
  final IconData icon;
  final bool obscureText;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;
  final String? Function(String?)? validator;
  final Widget? suffixIcon;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      obscureText: obscureText,
      textInputAction: textInputAction,
      onFieldSubmitted: onFieldSubmitted,
      validator: validator,
      style: const TextStyle(color: Colors.white),
      cursorColor: Colors.white,
      decoration: InputDecoration(
        hintText: hint,
        hintStyle: TextStyle(color: Colors.white.withValues(alpha: 0.7)),
        prefixIcon: PhosphorIcon(icon, color: Colors.white70, size: 20),
        suffixIcon: suffixIcon,
        filled: true,
        fillColor: Colors.white.withValues(alpha: 0.14),
        contentPadding: const EdgeInsets.symmetric(vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: BorderSide(color: Colors.white.withValues(alpha: 0.25)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: const BorderSide(color: Colors.white, width: 1.4),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: const BorderSide(color: _errorColor, width: 1.4),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: const BorderSide(color: _errorColor, width: 1.4),
        ),
        errorStyle: const TextStyle(color: _errorColor),
      ),
    );
  }
}
