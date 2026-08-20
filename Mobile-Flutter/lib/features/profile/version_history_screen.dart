import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import 'version_history_data.dart';

/// Man "Cac phien ban cap nhat" — liet ke tung dot phat hanh kem tinh nang moi/loi da sua.
/// Du lieu lay tu `versionHistoryEntries` (hien dang de trong, cho noi dung that duoc dien sau).
class VersionHistoryScreen extends StatefulWidget {
  const VersionHistoryScreen({super.key});

  @override
  State<VersionHistoryScreen> createState() => _VersionHistoryScreenState();
}

class _VersionHistoryScreenState extends State<VersionHistoryScreen> {
  String _currentVersionLabel = '';

  @override
  void initState() {
    super.initState();
    _loadCurrentVersion();
  }

  /// Doc so build hien tai tu goi cai dat — cung cach dung o man Dang nhap, chi de hien phu
  /// de tham khao, khong dung de doi chieu voi `versionHistoryEntries`.
  Future<void> _loadCurrentVersion() async {
    final info = await PackageInfo.fromPlatform();
    if (!mounted) return;
    setState(() => _currentVersionLabel = info.version);
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(title: 'Lịch sử phiên bản'),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppDimens.screenPadding),
          children: [
            if (_currentVersionLabel.isNotEmpty) ...[
              AppText('Phiên bản hiện tại: v$_currentVersionLabel',
                  variant: AppTextVariant.caption,
                  color: AppColors.textSecondary),
              const SizedBox(height: AppDimens.space16),
            ],
            if (versionHistoryEntries.isEmpty)
              const _EmptyVersionHistory()
            else
              for (final entry in versionHistoryEntries) ...[
                _VersionEntryCard(entry: entry),
                const SizedBox(height: AppDimens.space16),
              ],
          ],
        ),
      ),
    );
  }
}

class _EmptyVersionHistory extends StatelessWidget {
  const _EmptyVersionHistory();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.only(top: AppDimens.space32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            PhosphorIcon(PhosphorIconsRegular.clockCounterClockwise,
                color: AppColors.textFaint, size: 32),
            SizedBox(height: AppDimens.space12),
            AppText(
              'Chưa có nội dung cập nhật nào được ghi lại. Các phiên bản phát hành sau này sẽ '
              'hiện tại đây.',
              variant: AppTextVariant.body,
              color: AppColors.textSecondary,
              align: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

class _VersionEntryCard extends StatelessWidget {
  const _VersionEntryCard({required this.entry});

  final VersionHistoryEntry entry;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: AppText('Phiên bản ${entry.version}',
                    variant: AppTextVariant.heading),
              ),
              const SizedBox(width: AppDimens.space8),
              AppText(DateFormat('dd/MM/yyyy').format(entry.releasedAt),
                  variant: AppTextVariant.caption,
                  color: AppColors.textSecondary),
            ],
          ),
          const SizedBox(height: AppDimens.space12),
          for (var i = 0; i < entry.changes.length; i++) ...[
            _ChangeRow(change: entry.changes[i]),
            if (i < entry.changes.length - 1)
              const SizedBox(height: AppDimens.space8),
          ],
        ],
      ),
    );
  }
}

class _ChangeRow extends StatelessWidget {
  const _ChangeRow({required this.change});

  final VersionHistoryChange change;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _ChangeTypeBadge(type: change.type),
        const SizedBox(width: AppDimens.space8),
        Expanded(
          child: AppText(change.description,
              variant: AppTextVariant.body, height: 1.5),
        ),
      ],
    );
  }
}

/// Nhan mau nho phan biet "Tính năng mới" (mau success) va "Sửa lỗi" (mau warning) — theo dung
/// yeu cau khong chi truyen dat trang thai bang mau (co ca icon + chu).
class _ChangeTypeBadge extends StatelessWidget {
  const _ChangeTypeBadge({required this.type});

  final VersionChangeType type;

  @override
  Widget build(BuildContext context) {
    final isNewFeature = type == VersionChangeType.newFeature;
    final color =
        isNewFeature ? AppColors.successOnSoft : AppColors.warningOnSoft;
    final backgroundColor =
        isNewFeature ? AppColors.successSoft : AppColors.warningSoft;
    final icon = isNewFeature
        ? PhosphorIconsRegular.plusCircle
        : PhosphorIconsRegular.checkCircle;
    final label = isNewFeature ? 'Mới' : 'Sửa lỗi';

    return Container(
      padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space8, vertical: 3),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(AppDimens.radiusPill),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          PhosphorIcon(icon, size: 12, color: color),
          const SizedBox(width: 3),
          AppText(label,
              variant: AppTextVariant.overline,
              weight: FontWeight.w700,
              color: color),
        ],
      ),
    );
  }
}
