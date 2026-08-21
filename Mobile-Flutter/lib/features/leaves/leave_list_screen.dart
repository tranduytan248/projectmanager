import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/dialog_service.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_fab.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import 'leave_models.dart';
import 'leave_request_form_screen.dart';
import 'leave_service.dart';

class LeaveListScreen extends StatefulWidget {
  const LeaveListScreen({super.key});

  @override
  State<LeaveListScreen> createState() => _LeaveListScreenState();
}

class _LeaveListScreenState extends State<LeaveListScreen> {
  final _service = LeaveService();

  // 0 = "Tất cả năm", '' = "Tất cả trạng thái" — cung sentinel voi backend
  // (LeavesApiController.Index(year=0, state=null) = tat ca).
  int _year = 0;
  String _state = '';

  late Future<LeavesData> _future;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch(year: _year, state: _state);
  }

  void _reload() {
    setState(() {
      _future = _service.fetch(year: _year, state: _state);
    });
  }

  Future<void> _openAdd() async {
    final created = await pushLeaveFormScreen(context, service: _service);
    if (created == true) _reload();
  }

  Future<void> _openEdit(LeaveRequestItem item) async {
    final updated =
        await pushLeaveFormScreen(context, service: _service, existing: item);
    if (updated == true) _reload();
  }

  Future<void> _cancelLeave(LeaveRequestItem item) async {
    final confirmed = await DialogService.showConfirm(
      'Thu hồi đơn nghỉ phép này?',
      title: 'Xác nhận',
    );
    if (!confirmed) return;

    final result = await _service.cancel(item.id);
    if (!mounted) return;

    if (result.isSuccess) {
      _reload();
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(title: 'Nghỉ phép của tôi'),
      body: SafeArea(
        child: FutureBuilder<LeavesData>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: AppLoading());
            }

            if (snapshot.hasError) {
              return AppErrorState(
                message:
                    'Không tải được danh sách đơn nghỉ phép. Kiểm tra kết nối mạng rồi thử lại.',
                onRetry: _reload,
              );
            }

            final data = snapshot.data!;

            return ListView(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              children: [
                AppCard(
                  child: AppText(
                    'Năm ${data.statYear} đã nghỉ ${formatLeaveDays(data.approvedDaysInYear)}'
                    ' ngày công đã duyệt'
                    '${data.pendingCount > 0 ? ' · ${data.pendingCount} đơn đang chờ duyệt.' : '.'}',
                    variant: AppTextVariant.body,
                    fontSize: 13.5,
                    weight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: AppDimens.space16),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: AppDropdown<int>(
                        label: 'Năm',
                        value: _year,
                        items: {
                          0: 'Tất cả năm',
                          for (final y in data.years) y: y.toString(),
                        },
                        onChanged: (v) {
                          _year = v ?? 0;
                          _reload();
                        },
                      ),
                    ),
                    const SizedBox(width: AppDimens.space12),
                    Expanded(
                      child: AppDropdown<String>(
                        label: 'Trạng thái',
                        value: _state,
                        items: {
                          '': 'Tất cả trạng thái',
                          for (final s in LeaveStates.all) s: leaveStateLabel(s),
                        },
                        onChanged: (v) {
                          _state = v ?? '';
                          _reload();
                        },
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space16),
                if (data.items.isEmpty)
                  _EmptyState(onCreate: _openAdd)
                else
                  for (final item in data.items)
                    _LeaveCard(
                      item: item,
                      onEdit: () => _openEdit(item),
                      onCancel: () => _cancelLeave(item),
                    ),
              ],
            );
          },
        ),
      ),
      floatingActionButton: AppFab(
        label: 'Đăng ký',
        icon: PhosphorIconsRegular.plus,
        onPressed: _openAdd,
      ),
    );
  }
}

/// Trang thai rong — chua co don nghi phep nao (chua tung dang ky, hoac bo loc dang khong khop
/// don nao). Luon kem huong dan bam nut Dang ky, khong de trang tron.
class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.onCreate});

  final VoidCallback onCreate;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppDimens.space32),
      child: Column(
        children: [
          const PhosphorIcon(PhosphorIconsRegular.calendarBlank,
              size: 40, color: AppColors.textFaint),
          const SizedBox(height: AppDimens.space12),
          const AppText('Chưa có đơn nghỉ phép nào.',
              variant: AppTextVariant.body, align: TextAlign.center),
          const SizedBox(height: AppDimens.space4),
          const AppText('Bấm "Đăng ký" bên dưới để tạo đơn nghỉ phép mới.',
              variant: AppTextVariant.caption,
              color: AppColors.textSecondary,
              align: TextAlign.center),
          const SizedBox(height: AppDimens.space16),
          AppButton(label: 'Đăng ký nghỉ phép', onPressed: onCreate),
        ],
      ),
    );
  }
}

class _LeaveCard extends StatelessWidget {
  const _LeaveCard({required this.item, required this.onEdit, required this.onCancel});

  final LeaveRequestItem item;
  final VoidCallback onEdit;
  final VoidCallback onCancel;

  bool _isSameDate(DateTime a, DateTime b) =>
      a.year == b.year && a.month == b.month && a.day == b.day;

  @override
  Widget build(BuildContext context) {
    final sameDay = _isSameDate(item.fromDate, item.toDate);
    final dateFormat = DateFormat('dd/MM/yyyy');
    final dateLabel = sameDay
        ? dateFormat.format(item.fromDate)
        : '${dateFormat.format(item.fromDate)} – ${dateFormat.format(item.toDate)}';

    return AppCard(
      margin: const EdgeInsets.only(bottom: AppDimens.space12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      item.isHalfDay
                          ? '$dateLabel (Nghỉ nửa ngày — ${halfDaySessionLabel(item.halfDaySession)})'
                          : dateLabel,
                      variant: AppTextVariant.body,
                      fontSize: 14.5,
                      weight: FontWeight.w700,
                    ),
                    const SizedBox(height: 2),
                    AppText(
                      '${leaveKindLabel(item.kind)} · ${formatLeaveDays(item.days)} ngày công',
                      variant: AppTextVariant.caption,
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ),
              const SizedBox(width: AppDimens.space8),
              _StateBadge(state: item.state),
            ],
          ),
          if (item.reason?.isNotEmpty == true) ...[
            const SizedBox(height: AppDimens.space8),
            AppText(item.reason!, variant: AppTextVariant.body, fontSize: 13),
          ],
          if (item.approverNote?.isNotEmpty == true) ...[
            const SizedBox(height: AppDimens.space8),
            AppText('Ý kiến: ${item.approverNote}',
                variant: AppTextVariant.caption,
                fontSize: 12,
                color: AppColors.textSecondary),
          ],
          if (item.approvedByName?.isNotEmpty == true) ...[
            const SizedBox(height: AppDimens.space4),
            AppText(
              'Duyệt bởi ${item.approvedByName}'
              '${item.approvedAt != null ? ' · ${DateFormat('dd/MM/yyyy').format(item.approvedAt!)}' : ''}',
              variant: AppTextVariant.caption,
              fontSize: 11.5,
              color: AppColors.textSecondary,
            ),
          ],
          if (leaveIsEditable(item.state)) ...[
            const SizedBox(height: AppDimens.space12),
            Row(
              children: [
                Expanded(
                  child: AppButton(
                    label: 'Sửa',
                    type: AppButtonType.outline,
                    onPressed: onEdit,
                  ),
                ),
                const SizedBox(width: AppDimens.space8),
                Expanded(
                  child: AppButton(
                    label: 'Thu hồi',
                    type: AppButtonType.danger,
                    onPressed: onCancel,
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

/// Huy hieu trang thai — mau CHI la tro giup nhan nhanh, ban than chu ("Chờ duyệt"/"Đã duyệt"...)
/// da du truyen dat y nghia (khong phu thuoc mau, an toan voi nguoi mu mau).
class _StateBadge extends StatelessWidget {
  const _StateBadge({required this.state});

  final String state;

  @override
  Widget build(BuildContext context) {
    late final Color bg;
    late final Color fg;

    switch (state) {
      case LeaveStates.approved:
        bg = AppColors.successSoft;
        fg = AppColors.successOnSoft;
        break;
      case LeaveStates.rejected:
        bg = AppColors.dangerSoft;
        fg = AppColors.danger;
        break;
      case LeaveStates.cancelled:
        bg = AppColors.border;
        fg = AppColors.textSecondary;
        break;
      default:
        bg = AppTheme.brandBlueSoft;
        fg = AppTheme.brandBlue;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: AppText(leaveStateLabel(state),
          variant: AppTextVariant.overline,
          fontSize: 10.5,
          weight: FontWeight.w700,
          color: fg,
          letterSpacing: 0),
    );
  }
}
