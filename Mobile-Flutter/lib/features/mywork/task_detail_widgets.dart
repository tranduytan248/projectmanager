import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import 'task_detail_models.dart';

/// Widget/ham dung chung giua trang "Chi tiet cong viec" (view-only) va cac sheet Ghi gio/Cap
/// nhat todolist — tach ra day de tranh trung lap (truoc do moi noi tu dinh nghia rieng ban gan
/// giong het nhau).

/// Dinh dang so thap phan, cat bot so 0 thua o cuoi — giong dinh dang "0.##"/"0.#" ben web (vi du
/// 2.50 -> "2.5", 3.00 -> "3").
String formatHours(double value, {int maxDecimals = 2}) {
  var text = value.toStringAsFixed(maxDecimals);
  if (text.contains('.')) {
    text = text.replaceFirst(RegExp(r'0+$'), '');
    text = text.replaceFirst(RegExp(r'\.$'), '');
  }
  return text;
}

/// Mot cap nhan/gia tri nho gon — dung trong khoi "Gio cong" cho 4 so lieu Da ghi/Tran/Con
/// lai/Hom nay.
class MiniStat extends StatelessWidget {
  const MiniStat(
      {super.key, required this.label, required this.value, this.valueColor});

  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        AppText(label,
            variant: AppTextVariant.caption,
            fontSize: 11.5,
            color: AppColors.textSecondary),
        const SizedBox(height: AppDimens.space4 / 2),
        AppText(value,
            variant: AppTextVariant.body,
            fontSize: 14,
            weight: FontWeight.w700,
            color: valueColor),
      ],
    );
  }
}

/// Mot luot ghi gio — the vien mong, hien ngay/gio/noi dung/nguoi ghi. Trang chinh dung dang
/// view-only (bo qua onDelete), sheet Ghi gio truyen onDelete cho dong cua chinh minh.
class TimeLogRow extends StatelessWidget {
  const TimeLogRow(
      {super.key, required this.log, this.busy = false, this.onDelete});

  final TimeLogEntry log;
  final bool busy;
  final VoidCallback? onDelete;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: AppDimens.space8),
      padding: const EdgeInsets.all(AppDimens.space8),
      decoration: BoxDecoration(
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    AppText(
                      log.workDate == null
                          ? '—'
                          : DateFormat('dd/MM/yyyy').format(log.workDate!),
                      variant: AppTextVariant.caption,
                      fontSize: 12.5,
                      weight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                    const SizedBox(width: AppDimens.space8),
                    AppText(
                      '${formatHours(log.hours)} giờ',
                      variant: AppTextVariant.body,
                      fontSize: 13,
                      weight: FontWeight.w700,
                      color: AppTheme.brandBlue,
                    ),
                  ],
                ),
                if (log.note?.trim().isNotEmpty == true) ...[
                  const SizedBox(height: AppDimens.space4),
                  AppText(log.note!,
                      variant: AppTextVariant.body, fontSize: 13),
                ],
                const SizedBox(height: AppDimens.space4),
                AppText(
                  log.userName?.isNotEmpty == true ? log.userName! : '—',
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  color: AppColors.textSecondary,
                ),
              ],
            ),
          ),
          if (onDelete != null)
            busy
                ? const Padding(
                    padding: EdgeInsets.only(left: AppDimens.space8),
                    child: AppLoading(size: 20),
                  )
                : AppIconButton(
                    icon: PhosphorIconsRegular.trash,
                    color: AppTheme.statusDanger,
                    tooltip: 'Xoá lượt ghi giờ',
                    onPressed: onDelete,
                  ),
        ],
      ),
    );
  }
}

/// Mau theo trang thai — dong bo cach phoi mau dang dung o the Kanban (checklist_board_screen.dart
/// `_stateColor`) va danh sach viec (my_work_screen.dart `_StateBadge`): hoan thanh = xanh la, tam
/// dung = vang canh bao, con lai (chua bat dau/dang lam/huy) = xam trung tinh — giu MOT bang mau
/// duy nhat cho trang thai xuyen suot app.
Color taskStateColor(String state) {
  switch (state) {
    case 'HoanThanh':
      return AppTheme.statusSuccess;
    case 'TamDung':
      return AppTheme.statusWarning;
    default:
      return AppColors.textSecondary;
  }
}

/// Icon theo trang thai — di kem [taskStateColor] de trang thai khong chi phan biet bang mau
/// (nguoi mu mau van doc duoc qua hinh dang icon).
IconData taskStateIcon(String state) {
  switch (state) {
    case 'HoanThanh':
      return PhosphorIconsRegular.checkCircle;
    case 'DangLam':
      return PhosphorIconsRegular.playCircle;
    case 'TamDung':
      return PhosphorIconsRegular.pauseCircle;
    case 'Huy':
      return PhosphorIconsRegular.xCircle;
    default:
      return PhosphorIconsRegular.circle;
  }
}

/// Mau theo do uu tien — dong bo mau do cua nhan "Ưu tiên cao" dang dung o the Kanban
/// (checklist_board_screen.dart)/danh sach viec (dashboard_screen.dart), them muc vang/xam cho
/// Trung binh/Thap de phan biet ngay ca ba muc bang mau.
Color taskPriorityColor(String priority) {
  switch (priority) {
    case 'Cao':
      return AppTheme.statusDanger;
    case 'Thap':
      return AppColors.textSecondary;
    default:
      return AppTheme.statusWarning;
  }
}

/// Nhan dang "chip" mau — nen mo (soft) + icon/chu dam mau cung tong mau, dung cho Trang thai va
/// Do uu tien trong khoi "Thong tin chung" de nhan dien nhanh bang mau THEM icon (khong chi dua
/// vao mau, dam bao van doc duoc voi nguoi mu mau). Boc trong Flexible o noi goi de an toan khi
/// hai chip dung chung mot Row tren man hinh hep — chu se rut gon bang dau "..." thay vi tran ra
/// ngoai.
class AppInfoChip extends StatelessWidget {
  const AppInfoChip(
      {super.key, required this.icon, required this.label, required this.color});

  final IconData icon;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding:
          const EdgeInsets.symmetric(horizontal: AppDimens.space12, vertical: 7),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(AppDimens.radiusPill),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          PhosphorIcon(icon, size: 14, color: color),
          const SizedBox(width: 5),
          Flexible(
            child: AppText(
              label,
              variant: AppTextVariant.caption,
              fontSize: 12.5,
              weight: FontWeight.w700,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}

/// Mot dong nhan/gia tri co icon dan dat, dat trong khung mau nhat — thay cho hang chu phang de
/// khoi "Thong tin chung" de quet mat hon. [iconColor]/[valueColor] tuy chinh cho truong can nhan
/// manh (vi du han hoan thanh qua han to mau canh bao).
class TaskInfoIconRow extends StatelessWidget {
  const TaskInfoIconRow({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
    this.iconColor,
    this.valueColor,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color? iconColor;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    final color = iconColor ?? AppTheme.brandBlue;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 28,
            height: 28,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            ),
            child: PhosphorIcon(icon, size: 15, color: color),
          ),
          const SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppText(label,
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    color: AppColors.textSecondary),
                const SizedBox(height: 2),
                AppText(value,
                    variant: AppTextVariant.body,
                    fontSize: 13.5,
                    weight: FontWeight.w600,
                    color: valueColor),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Dong "Tien do" trong khoi "Thong tin chung" — cung kieu thanh tien trinh mau voi khoi "Gio
/// cong" ben duoi (ClipRRect + LinearProgressIndicator) de dong bo trong CUNG mot man hinh, thay
/// vi chi hien "NN%" bang chu nhu truoc.
class TaskProgressRow extends StatelessWidget {
  const TaskProgressRow({super.key, required this.percent});

  final int percent;

  @override
  Widget build(BuildContext context) {
    final color =
        percent >= 100 ? AppTheme.statusSuccess : AppTheme.brandBlue;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 28,
            height: 28,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            ),
            child: PhosphorIcon(PhosphorIconsRegular.gauge, size: 15, color: color),
          ),
          const SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Expanded(
                      child: AppText('Tiến độ',
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          color: AppColors.textSecondary),
                    ),
                    AppText('$percent%',
                        variant: AppTextVariant.body,
                        fontSize: 13.5,
                        weight: FontWeight.w700,
                        color: color),
                  ],
                ),
                const SizedBox(height: 4),
                ClipRRect(
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  child: LinearProgressIndicator(
                    value: (percent.clamp(0, 100)) / 100,
                    minHeight: 6,
                    backgroundColor: color.withValues(alpha: 0.12),
                    valueColor: AlwaysStoppedAnimation(color),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Dong "Nguoi thuc hien" co avatar tron chua chu cai dau ten — thay cho chu suong, giup nhan
/// dien nguoi phu trach nhanh hon (dung mau brand nhat, dong bo voi bang mau primary cua app).
class TaskAssigneeRow extends StatelessWidget {
  const TaskAssigneeRow({super.key, required this.name});

  final String name;

  String get _initials {
    final parts =
        name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts.first.substring(0, 1) + parts.last.substring(0, 1))
        .toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space12),
      child: Row(
        children: [
          Container(
            width: 32,
            height: 32,
            alignment: Alignment.center,
            decoration: const BoxDecoration(
              color: AppTheme.brandBlueSoft,
              shape: BoxShape.circle,
            ),
            child: AppText(_initials,
                variant: AppTextVariant.caption,
                fontSize: 12.5,
                weight: FontWeight.w800,
                color: AppTheme.brandBlueDark),
          ),
          const SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const AppText('Người thực hiện',
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    color: AppColors.textSecondary),
                const SizedBox(height: 2),
                AppText(name,
                    variant: AppTextVariant.body,
                    fontSize: 13.5,
                    weight: FontWeight.w600),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Mot dong "Viec can lam" dang CHI XEM (tick tinh, khong bam duoc) — dung cho trang chinh
/// view-only va nhanh khong-co-quyen quan ly cua sheet Cap nhat todolist.
class TodoReadOnlyRow extends StatelessWidget {
  const TodoReadOnlyRow({super.key, required this.item});

  final TodoItem item;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space4),
      child: Row(
        children: [
          PhosphorIcon(
            item.isDone
                ? PhosphorIconsRegular.checkCircle
                : PhosphorIconsRegular.circle,
            size: 20,
            color: item.isDone ? AppTheme.statusSuccess : AppColors.textFaint,
          ),
          const SizedBox(width: AppDimens.space8),
          Expanded(
            child: AppText(
              item.content,
              variant: AppTextVariant.body,
              color: item.isDone ? AppColors.textSecondary : null,
              decoration: item.isDone ? TextDecoration.lineThrough : null,
            ),
          ),
        ],
      ),
    );
  }
}
