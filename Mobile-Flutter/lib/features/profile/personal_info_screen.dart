import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_detail_section.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'profile_models.dart';
import 'profile_service.dart';

/// Man "Thong tin ca nhan" — tuong duong Account/Profile ben web: mot khoi chi xem (tai khoan/
/// phan quyen/ngay tao), mot form sua Ho ten, mot form doi mat khau. Ten dang nhap va phan quyen
/// do quan tri vien thiet lap nen khong sua duoc o day, dung y het web.
class PersonalInfoScreen extends StatefulWidget {
  const PersonalInfoScreen({super.key});

  @override
  State<PersonalInfoScreen> createState() => _PersonalInfoScreenState();
}

class _PersonalInfoScreenState extends State<PersonalInfoScreen> {
  final _service = ProfileService();
  late Future<ProfileInfo> _future;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch();
  }

  void _reload() {
    setState(() => _future = _service.fetch());
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(title: 'Thông tin cá nhân'),
      body: SafeArea(
        child: FutureBuilder<ProfileInfo>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: AppLoading());
            }

            if (snapshot.hasError) {
              return AppErrorState(
                  message: 'Không tải được thông tin cá nhân.',
                  onRetry: _reload);
            }

            return _ProfileBody(profile: snapshot.data!, service: _service);
          },
        ),
      ),
    );
  }
}

class _ProfileBody extends StatefulWidget {
  const _ProfileBody({required this.profile, required this.service});

  final ProfileInfo profile;
  final ProfileService service;

  @override
  State<_ProfileBody> createState() => _ProfileBodyState();
}

class _ProfileBodyState extends State<_ProfileBody> {
  late ProfileInfo _profile;
  late final TextEditingController _fullNameController;
  final _fullNameFormKey = GlobalKey<FormState>();
  bool _savingName = false;

  final _passwordFormKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _changingPassword = false;
  bool _obscureCurrent = true;
  bool _obscureNew = true;
  bool _obscureConfirm = true;

  @override
  void initState() {
    super.initState();
    _profile = widget.profile;
    _fullNameController = TextEditingController(text: _profile.fullName);
  }

  @override
  void dispose() {
    _fullNameController.dispose();
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _saveFullName() async {
    if (!_fullNameFormKey.currentState!.validate()) return;

    setState(() => _savingName = true);
    final result =
        await widget.service.updateFullName(_fullNameController.text.trim());
    if (!mounted) return;
    setState(() => _savingName = false);

    if (result.isSuccess) {
      setState(() => _profile = result.profile!);
      ToastService.show('Đã cập nhật thông tin cá nhân.',
          type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _changePassword() async {
    if (!_passwordFormKey.currentState!.validate()) return;

    setState(() => _changingPassword = true);
    final result = await widget.service.changePassword(
      currentPassword: _currentPasswordController.text,
      newPassword: _newPasswordController.text,
      confirmPassword: _confirmPasswordController.text,
    );
    if (!mounted) return;
    setState(() => _changingPassword = false);

    if (result.isSuccess) {
      _currentPasswordController.clear();
      _newPasswordController.clear();
      _confirmPasswordController.clear();
      ToastService.show('Đã đổi mật khẩu thành công.',
          type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Widget _obscureToggle(bool obscured, VoidCallback onTap) {
    return AppIconButton(
      icon: obscured ? PhosphorIconsRegular.eye : PhosphorIconsRegular.eyeSlash,
      size: 19,
      color: AppColors.textFaint,
      // Nut phu, nam doc lap trong o nhap (giong nut "x" go dieu kien loc tren chip) — giam
      // vung cham xuong 32dp thay vi 48dp mac dinh de khong lam o nhap phinh to bat thuong.
      minSize: 32,
      onPressed: onTap,
    );
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
      children: [
        const AppText(
          'Xem thông tin tài khoản, cập nhật họ tên và đổi mật khẩu.',
          variant: AppTextVariant.caption,
          fontSize: 12.5,
          color: AppColors.textSecondary,
        ),
        const SizedBox(height: 16),
        AppDetailSection(
          title: 'Tài khoản',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AppDetailInfoRow(
                  label: 'Tên đăng nhập', value: _profile.userName),
              const SizedBox(height: 2),
              _RoleRow(profile: _profile),
              const SizedBox(height: 8),
              AppDetailInfoRow(
                label: 'Ngày tạo',
                value: _profile.createdAt == null
                    ? '—'
                    : DateFormat('dd/MM/yyyy').format(_profile.createdAt!),
              ),
              const SizedBox(height: 4),
              const AppText(
                'Tên đăng nhập và phân quyền do quản trị viên thiết lập, bạn không tự đổi được.',
                variant: AppTextVariant.caption,
                fontSize: 11.5,
                color: AppColors.textSecondary,
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        AppDetailSection(
          title: 'Cập nhật thông tin',
          child: Form(
            key: _fullNameFormKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppTextField(
                  label: 'Họ và tên',
                  controller: _fullNameController,
                  required: true,
                  validator: (v) => (v == null || v.trim().isEmpty)
                      ? 'Vui lòng nhập họ tên'
                      : null,
                ),
                const SizedBox(height: 14),
                AppButton(
                  label: 'Lưu thông tin',
                  isLoading: _savingName,
                  onPressed: _savingName ? null : _saveFullName,
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        AppDetailSection(
          title: 'Đổi mật khẩu',
          child: Form(
            key: _passwordFormKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppTextField(
                  label: 'Mật khẩu hiện tại',
                  controller: _currentPasswordController,
                  obscureText: _obscureCurrent,
                  required: true,
                  suffixIconWidget: _obscureToggle(_obscureCurrent,
                      () => setState(() => _obscureCurrent = !_obscureCurrent)),
                  validator: (v) => (v == null || v.isEmpty)
                      ? 'Vui lòng nhập mật khẩu hiện tại'
                      : null,
                ),
                const SizedBox(height: 12),
                AppTextField(
                  label: 'Mật khẩu mới',
                  controller: _newPasswordController,
                  obscureText: _obscureNew,
                  required: true,
                  hint: 'Tối thiểu 6 ký tự',
                  suffixIconWidget: _obscureToggle(_obscureNew,
                      () => setState(() => _obscureNew = !_obscureNew)),
                  validator: (v) => (v == null || v.length < 6)
                      ? 'Mật khẩu mới tối thiểu 6 ký tự'
                      : null,
                ),
                const SizedBox(height: 12),
                AppTextField(
                  label: 'Xác nhận mật khẩu mới',
                  controller: _confirmPasswordController,
                  obscureText: _obscureConfirm,
                  required: true,
                  suffixIconWidget: _obscureToggle(_obscureConfirm,
                      () => setState(() => _obscureConfirm = !_obscureConfirm)),
                  validator: (v) => v != _newPasswordController.text
                      ? 'Xác nhận mật khẩu không khớp'
                      : null,
                ),
                const SizedBox(height: 14),
                AppButton(
                  label: 'Đổi mật khẩu',
                  isLoading: _changingPassword,
                  onPressed: _changingPassword ? null : _changePassword,
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _RoleRow extends StatelessWidget {
  const _RoleRow({required this.profile});

  final ProfileInfo profile;

  @override
  Widget build(BuildContext context) {
    final color = profile.isAdmin ? AppTheme.statusDanger : AppTheme.brandBlue;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(
          width: 112,
          child: AppText('Phân quyền',
              variant: AppTextVariant.caption,
              fontSize: 12.5,
              color: AppColors.textSecondary),
        ),
        Expanded(
          child: Wrap(
            spacing: AppDimens.space8,
            runSpacing: AppDimens.space8,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: AppText(profile.roleDisplay,
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    weight: FontWeight.w700,
                    color: color),
              ),
              // Vai Quan ly To nam NGOAI (cac) nhom quyen o tren — the rieng, y het cach web
              // (Views/Users/Index.cshtml) hien them badge "Quản lý Tổ" ben canh badge nhom quyen.
              if (profile.isTeamManager)
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: AppColors.successOnSoft.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: const AppText('Quản lý Tổ',
                      variant: AppTextVariant.caption,
                      fontSize: 11.5,
                      weight: FontWeight.w700,
                      color: AppColors.successOnSoft),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
