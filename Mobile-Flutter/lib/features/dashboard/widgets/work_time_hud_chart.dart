import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_dimens.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_text.dart';
import '../dashboard_models.dart';

/// Biểu đồ HUD Power Curve hiển thị Tổng thời gian làm việc và Logtime mỗi ngày (T2 - CN).
/// Max = 12h.
/// Quy tắc màu sắc:
/// - >= 8h: Xanh lá (AppColors.success / #4ADE80)
/// - 6h <= hours < 8h: Xanh dương (AppColors.info / #38BDF8)
/// - 4h <= hours < 6h: Màu cam (#FB923C)
/// - 0h < hours < 4h: Màu đỏ (AppColors.danger / #F87171)
class WorkTimeHUDChart extends StatelessWidget {
  const WorkTimeHUDChart({
    super.key,
    required this.workTime,
    this.monthlyTargetHours,
  });

  final WorkTimeDashboard workTime;
  final double? monthlyTargetHours;

  /// Hàm trả về màu sắc theo ngưỡng giờ quy định
  static Color getHourColor(double hours) {
    if (hours >= 8.0) {
      return AppColors.success;
    } else if (hours >= 6.0) {
      return AppColors.info;
    } else if (hours >= 4.0) {
      return const Color(0xFFFB923C); // Màu cam nổi bật
    } else if (hours > 0.0) {
      return AppColors.danger;
    }
    return AppColors.border; // Chưa logtime (0h)
  }

  /// Tên trạng thái tương ứng với màu
  static String getHourStatus(double hours) {
    if (hours >= 8.0) return 'Đạt chuẩn';
    if (hours >= 6.0) return 'Khá';
    if (hours >= 4.0) return 'Trung bình';
    if (hours > 0.0) return 'Ít';
    return 'Chưa ghi';
  }

  @override
  Widget build(BuildContext context) {
    // Mục tiêu giờ công làm việc trong tháng (lấy từ KPI hoặc mặc định 176h = 22 ngày x 8h)
    final targetMonth = (monthlyTargetHours != null && monthlyTargetHours! > 0)
        ? monthlyTargetHours!
        : 176.0;
    final percent =
        (workTime.totalHoursMonth / targetMonth * 100).clamp(0.0, 100.0);

    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: Tiêu đề + Badge tổng giờ tuần
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(AppDimens.space8),
                decoration: BoxDecoration(
                  color: AppColors.primarySoft,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  border:
                      Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
                ),
                child: const Icon(
                  PhosphorIconsFill.lightning,
                  color: AppColors.primary,
                  size: 18,
                ),
              ),
              const SizedBox(width: AppDimens.space12),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      'THỜI GIAN LÀM VIỆC & LOGTIME',
                      variant: AppTextVariant.heading,
                      fontSize: 13,
                      weight: FontWeight.w700,
                      letterSpacing: 0.5,
                      color: AppColors.textPrimary,
                    ),
                    SizedBox(height: 2),
                    AppText(
                      'Theo dõi giờ công tuần & từng ngày (Max 12h)',
                      variant: AppTextVariant.caption,
                      fontSize: 11,
                      color: AppColors.textFaint,
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppDimens.space8,
                  vertical: AppDimens.space4,
                ),
                decoration: BoxDecoration(
                  color:
                      getHourColor(workTime.todayHours).withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                  border: Border.all(
                    color:
                        getHourColor(workTime.todayHours).withValues(alpha: 0.5),
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 6,
                      height: 6,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: getHourColor(workTime.todayHours),
                      ),
                    ),
                    const SizedBox(width: AppDimens.space4),
                    AppText(
                      'Hôm nay: ${workTime.todayHours.toStringAsFixed(1)}h',
                      variant: AppTextVariant.caption,
                      fontSize: 11,
                      weight: FontWeight.w600,
                      color: getHourColor(workTime.todayHours),
                    ),
                  ],
                ),
              ),
            ],
          ),

          const SizedBox(height: AppDimens.space16),

          // Main HUD Layout (Dual HUD: Battery Power Meter + Segmented Power Curve Bars)
          Container(
            padding: const EdgeInsets.all(AppDimens.space12),
            decoration: BoxDecoration(
              color: AppColors.background.withValues(alpha: 0.6),
              borderRadius: BorderRadius.circular(AppDimens.radiusMd),
              border: Border.all(color: AppColors.border),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                // Bên trái: Đồng hồ Pin HUD / Level Gauge
                _buildBatteryPowerGauge(percent),

                const SizedBox(width: AppDimens.space16),

                // Đường ngăn cách mờ
                Container(
                  width: 1,
                  height: 120,
                  color: AppColors.border,
                ),

                const SizedBox(width: AppDimens.space12),

                // Bên phải: Biểu đồ Power Curve 7 ngày (T2 - CN)
                Expanded(
                  child: _buildPowerCurveBars(),
                ),
              ],
            ),
          ),

          const SizedBox(height: AppDimens.space12),

          // Mini Metric Cards: 3 chỉ số nhanh
          Row(
            children: [
              Expanded(
                child: _buildMetricTile(
                  icon: PhosphorIconsRegular.clock,
                  label: 'Hôm nay',
                  value: '${workTime.todayHours.toStringAsFixed(1)}h',
                  color: getHourColor(workTime.todayHours),
                ),
              ),
              const SizedBox(width: AppDimens.space8),
              Expanded(
                child: _buildMetricTile(
                  icon: PhosphorIconsRegular.calendarCheck,
                  label: 'Tuần này',
                  value: '${workTime.totalHoursWeek.toStringAsFixed(1)}h',
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: AppDimens.space8),
              Expanded(
                child: _buildMetricTile(
                  icon: PhosphorIconsRegular.chartLineUp,
                  label: 'Tháng này',
                  value: '${workTime.totalHoursMonth.toStringAsFixed(1)}h',
                  color: AppColors.warning,
                ),
              ),
            ],
          ),

          const SizedBox(height: AppDimens.space12),

          // Legend Bar: Thang màu quy định
          _buildLegendBar(),
        ],
      ),
    );
  }

  /// Widget viên Pin HUD Gauge mô phỏng năng lượng làm việc
  Widget _buildBatteryPowerGauge(double percent) {
    const totalSegments = 6;
    final activeSegments = ((percent / 100.0) * totalSegments).round();
    final gaugeColor = percent >= 80.0
        ? AppColors.success
        : percent >= 50.0
            ? AppColors.info
            : percent >= 25.0
                ? const Color(0xFFFB923C)
                : AppColors.danger;

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        // Đầu pin cực dương nhỏ
        Container(
          width: 14,
          height: 4,
          decoration: BoxDecoration(
            color: gaugeColor.withValues(alpha: 0.7),
            borderRadius: const BorderRadius.vertical(top: Radius.circular(2)),
          ),
        ),

        // Thân pin
        Container(
          width: 52,
          height: 96,
          padding: const EdgeInsets.all(AppDimens.space4),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            border: Border.all(
              color: gaugeColor.withValues(alpha: 0.8),
              width: 1.5,
            ),
            boxShadow: [
              BoxShadow(
                color: gaugeColor.withValues(alpha: 0.15),
                blurRadius: 8,
                spreadRadius: 1,
              ),
            ],
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: List.generate(totalSegments, (index) {
              // Vẽ từ trên xuống dưới
              final segIndex = totalSegments - 1 - index;
              final isFilled = segIndex < activeSegments;

              return AnimatedContainer(
                duration: const Duration(milliseconds: 300),
                height: 10,
                decoration: BoxDecoration(
                  color: isFilled ? gaugeColor : AppColors.surfaceVariant,
                  borderRadius: BorderRadius.circular(2),
                  boxShadow: isFilled
                      ? [
                          BoxShadow(
                            color: gaugeColor.withValues(alpha: 0.6),
                            blurRadius: 3,
                          ),
                        ]
                      : null,
                ),
              );
            }),
          ),
        ),

        const SizedBox(height: AppDimens.space4),

        // Số % hoàn thành
        AppText(
          '${percent.toStringAsFixed(0)}%',
          variant: AppTextVariant.heading,
          fontSize: 13,
          weight: FontWeight.w700,
          color: gaugeColor,
        ),
        const AppText(
          'Mục tiêu',
          variant: AppTextVariant.caption,
          fontSize: 9,
          color: AppColors.textFaint,
        ),
      ],
    );
  }

  /// Biểu đồ cột Power Curve 7 ngày trong tuần
  Widget _buildPowerCurveBars() {
    final dailyLogs = workTime.dailyLogs;
    const maxHour = 12.0;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Tiêu đề nhỏ biểu đồ
        const Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            AppText(
              'LOGTIME 7 NGÀY',
              variant: AppTextVariant.caption,
              fontSize: 10,
              weight: FontWeight.w700,
              color: AppColors.textSecondary,
              letterSpacing: 0.5,
            ),
            AppText(
              'Trần: 12h/ngày',
              variant: AppTextVariant.caption,
              fontSize: 9,
              color: AppColors.textFaint,
            ),
          ],
        ),

        const SizedBox(height: AppDimens.space8),

        // 7 Cột biểu đồ tương ứng T2 -> CN
        SizedBox(
          height: 105,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            crossAxisAlignment: CrossAxisAlignment.end,
            children: dailyLogs.map((log) {
              final color = getHourColor(log.hours);
              final heightFactor = (log.hours / maxHour).clamp(0.06, 1.0);
              const barMaxHeight = 65.0;
              final barHeight = barMaxHeight * heightFactor;

              return Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 2),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      // Số giờ trên đầu cột
                      AppText(
                        log.hours > 0 ? log.hours.toStringAsFixed(1) : '0',
                        variant: AppTextVariant.caption,
                        fontSize: 8.5,
                        weight: log.isToday ? FontWeight.w700 : FontWeight.w500,
                        color: log.hours > 0 ? color : AppColors.textFaint,
                      ),
                      const SizedBox(height: 2),

                      // Thanh Capsule đứng đa phân đoạn (Segmented Pill Bar)
                      Container(
                        width: double.infinity,
                        height: barHeight,
                        decoration: BoxDecoration(
                          color: log.hours > 0
                              ? color
                              : AppColors.surfaceVariant.withValues(alpha: 0.5),
                          borderRadius:
                              BorderRadius.circular(AppDimens.radiusSm),
                          border: Border.all(
                            color: log.isToday
                                ? AppColors.primary
                                : (log.hours > 0
                                    ? color.withValues(alpha: 0.4)
                                    : AppColors.border),
                            width: log.isToday ? 1.5 : 1.0,
                          ),
                          boxShadow: log.hours > 0
                              ? [
                                  BoxShadow(
                                    color: color.withValues(alpha: 0.3),
                                    blurRadius: log.isToday ? 6 : 3,
                                    offset: const Offset(0, 1),
                                  ),
                                ]
                              : null,
                        ),
                        child: log.hours > 0
                            ? ClipRRect(
                                borderRadius: BorderRadius.circular(
                                    AppDimens.radiusSm - 1),
                                child: Column(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceEvenly,
                                  children: List.generate(
                                    (barHeight / 12).clamp(1, 5).toInt(),
                                    (_) => Container(
                                      height: 1.5,
                                      margin: const EdgeInsets.symmetric(
                                          horizontal: 2),
                                      color: AppColors.background
                                          .withValues(alpha: 0.3),
                                    ),
                                  ),
                                ),
                              )
                            : null,
                      ),

                      const SizedBox(height: AppDimens.space4),

                      // Thứ trong tuần (T2, T3, ... CN)
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 3,
                          vertical: 1,
                        ),
                        decoration: log.isToday
                            ? BoxDecoration(
                                color: AppColors.primarySoft,
                                borderRadius: BorderRadius.circular(3),
                                border: Border.all(
                                  color:
                                      AppColors.primary.withValues(alpha: 0.5),
                                ),
                              )
                            : null,
                        child: AppText(
                          log.dayOfWeek,
                          variant: AppTextVariant.caption,
                          fontSize: 9.5,
                          weight:
                              log.isToday ? FontWeight.w700 : FontWeight.w500,
                          color: log.isToday
                              ? AppColors.primary
                              : AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            }).toList(),
          ),
        ),
      ],
    );
  }

  /// Khung chỉ số nhỏ bên dưới
  Widget _buildMetricTile({
    required IconData icon,
    required String label,
    required String value,
    required Color color,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space8,
        vertical: AppDimens.space8,
      ),
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: AppDimens.space4),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppText(
                  label,
                  variant: AppTextVariant.caption,
                  fontSize: 9.5,
                  color: AppColors.textFaint,
                ),
                AppText(
                  value,
                  variant: AppTextVariant.body,
                  fontSize: 12,
                  weight: FontWeight.w700,
                  color: color,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Chú thích thang đo màu sắc theo yêu cầu
  Widget _buildLegendBar() {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space8,
        vertical: AppDimens.space4,
      ),
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          _buildLegendItem(AppColors.success, '≥ 8h'),
          _buildLegendItem(AppColors.info, '6 - 8h'),
          _buildLegendItem(const Color(0xFFFB923C), '4 - 6h'),
          _buildLegendItem(AppColors.danger, '< 4h'),
        ],
      ),
    );
  }

  Widget _buildLegendItem(Color color, String label) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: color,
          ),
        ),
        const SizedBox(width: AppDimens.space4),
        AppText(
          label,
          variant: AppTextVariant.caption,
          fontSize: 9.5,
          weight: FontWeight.w500,
          color: AppColors.textSecondary,
        ),
      ],
    );
  }
}
