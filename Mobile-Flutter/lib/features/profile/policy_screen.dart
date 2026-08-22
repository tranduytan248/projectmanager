import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';

/// Mục chính sách có tiêu đề, biểu tượng và danh sách các nội dung chi tiết
class PolicySection {
  final String title;
  final IconData icon;
  final List<String> items;

  const PolicySection({
    required this.title,
    required this.icon,
    required this.items,
  });
}

/// Màn hình hiển thị "Chính sách bảo mật" và "Điều khoản sử dụng"
/// Nội dung được biên soạn chi tiết dựa trên toàn bộ các tính năng nghiệp vụ của hệ thống Tổ NCPT.
class PolicyScreen extends StatelessWidget {
  const PolicyScreen({
    super.key,
    required this.title,
    required this.sections,
    this.lastUpdated = '22/08/2026',
  });

  final String title;
  final List<PolicySection> sections;
  final String lastUpdated;

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(title: title),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.symmetric(
            horizontal: AppDimens.space16,
            vertical: AppDimens.space16,
          ),
          children: [
            // Header Card
            AppCard(
              padding: const EdgeInsets.all(AppDimens.space16),
              radius: AppDimens.radiusMd,
              child: Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: AppColors.primarySoft,
                      borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                    ),
                    child: const Icon(
                      PhosphorIconsRegular.shieldCheck,
                      color: AppColors.primary,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: AppDimens.space12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        AppText(
                          title,
                          variant: AppTextVariant.body,
                          fontSize: 16,
                          weight: FontWeight.w700,
                        ),
                        const SizedBox(height: 2),
                        AppText(
                          'Hệ thống Quản lý Dự án & Nhân sự Tổ NCPT · Cập nhật: $lastUpdated',
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          color: AppColors.textSecondary,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppDimens.space16),

            // Sections List
            for (int i = 0; i < sections.length; i++) ...[
              _PolicySectionCard(
                index: i + 1,
                section: sections[i],
              ),
              const SizedBox(height: AppDimens.space12),
            ],

            const SizedBox(height: AppDimens.space16),
            // Footer Note
            const Center(
              child: AppText(
                'Mọi thắc mắc hoặc yêu cầu hỗ trợ về an toàn thông tin,\nvui lòng liên hệ Quản trị viên hệ thống Tổ NCPT.',
                variant: AppTextVariant.caption,
                fontSize: 11.5,
                color: AppColors.textFaint,
                align: TextAlign.center,
              ),
            ),
            const SizedBox(height: AppDimens.space24),
          ],
        ),
      ),
    );
  }
}

class _PolicySectionCard extends StatelessWidget {
  const _PolicySectionCard({
    required this.index,
    required this.section,
  });

  final int index;
  final PolicySection section;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      radius: AppDimens.radiusMd,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Section Title
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: AppColors.primarySoft,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                ),
                child: AppText(
                  'Mục $index',
                  variant: AppTextVariant.caption,
                  fontSize: 11,
                  weight: FontWeight.w700,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: AppDimens.space8),
              Expanded(
                child: AppText(
                  section.title,
                  variant: AppTextVariant.body,
                  fontSize: 14.5,
                  weight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space12),
          const Divider(height: 1, color: AppColors.border),
          const SizedBox(height: AppDimens.space12),

          // Items
          for (final item in section.items) ...[
            Padding(
              padding: const EdgeInsets.only(bottom: AppDimens.space8),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    margin: const EdgeInsets.only(top: 6, right: 10),
                    width: 6,
                    height: 6,
                    decoration: const BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                    ),
                  ),
                  Expanded(
                    child: AppText(
                      item,
                      variant: AppTextVariant.body,
                      fontSize: 13,
                      height: 1.55,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Dữ liệu toàn diện: Chính sách bảo mật (Privacy Policy)
const privacyPolicySections = [
  PolicySection(
    title: 'Phạm vi & Đối tượng áp dụng',
    icon: PhosphorIconsRegular.buildings,
    items: [
      'Hệ thống quản lý công việc và dự án (Website và ứng dụng di động BrewTask) là phần mềm nghiệp vụ nội bộ của Tổ Nghiên cứu & Phát triển (Tổ NCPT — Trung tâm Kinh doanh Giải pháp).',
      'Hệ thống chỉ cấp phát tài khoản định danh cho cán bộ, nhân viên trực thuộc đơn vị và các đối tác dự án được phân quyền — tuyệt đối không mở đăng ký tự do ra ngoài cộng đồng.',
    ],
  ),
  PolicySection(
    title: 'Dữ liệu thu thập & Xử lý',
    icon: PhosphorIconsRegular.database,
    items: [
      'Thông tin định danh cá nhân: Họ và tên, Tên đăng nhập, Địa chỉ Email công vụ, Số điện thoại, Ảnh đại diện và Vị trí chuyên môn trong Tổ.',
      'Dữ liệu vận hành dự án: Danh sách dự án tham gia, Vai trò (Quản lý dự án - PM, Thành viên), Đầu việc được phân công, Bảng tiến độ và Bảng kiểm tra checklist (Todo).',
      'Dữ liệu chấm công & Giờ làm việc: Nhật ký ghi giờ công thực tế (Time Log) theo từng nhiệm vụ, phục vụ đối soát thời gian làm việc tiêu chuẩn và tính toán năng suất.',
      'Dữ liệu tương tác & Trao đổi: Nội dung bình luận, Tin nhắn nhắc tên thành viên (@mention) và các tệp tài liệu đính kèm phục vụ xử lý công việc.',
      'Dữ liệu quản lý nhân sự & Nghỉ phép: Đơn xin nghỉ phép (loại phép, số ngày, lý do), Người duyệt và Lịch sử phê duyệt của Quản lý Tổ.',
      'Dữ liệu đánh giá hiệu quả & KPI: Điểm số thực thi, Điểm hỗ trợ, Trừ điểm trễ hạn, Tỷ lệ chuyên cần (Attendance rate) và Bảng xếp loại KPI hàng tháng.',
      'Dữ liệu thiết bị & Thông báo đẩy: Mã FCM Device Token (Firebase Cloud Messaging) được đăng ký tự động và chỉ cập nhật cho thiết bị hoạt động mới nhất của mỗi tài khoản.',
    ],
  ),
  PolicySection(
    title: 'Mục đích sử dụng dữ liệu',
    icon: PhosphorIconsRegular.target,
    items: [
      'Phân bổ nguồn lực, giao việc và theo dõi sát sao tiến độ hoàn thành các dự án giải pháp phần mềm.',
      'Tự động tổng hợp dữ liệu giờ công, tính toán bảng điểm KPI tháng chính xác, công bằng và minh bạch cho từng thành viên.',
      'Quản lý phê duyệt nghỉ phép, lịch trực và giám sát hoạt động nhân sự theo thời gian thực.',
      'Gửi thông báo đẩy tức thì đến thiết bị nhân viên khi có công việc mới được giao, đơn phép được duyệt, nhắc nhở hạn chót hoặc có trao đổi chuyên môn mới.',
    ],
  ),
  PolicySection(
    title: 'Bảo mật & Lưu trữ thông tin',
    icon: PhosphorIconsRegular.lockKey,
    items: [
      'Toàn bộ dữ liệu được lưu trữ tập trung tại hệ thống máy chủ cơ sở dữ liệu nội bộ (SQL Server) với cơ chế bảo mật và sao lưu dữ liệu định kỳ.',
      'Mật khẩu đăng nhập được mã hóa một chiều bằng thuật toán băm chuẩn an toàn (PBKDF2/SHA-256), đảm bảo ngay cả quản trị viên cũng không thể đọc được mật khẩu gốc.',
      'Xác thực API trên ứng dụng di động qua chuẩn Bearer Token an toàn, có thời hạn và tự động thu hồi khi người dùng đăng xuất.',
      'Tuyệt đối KHÔNG chia sẻ, kinh doanh, chuyển nhượng hoặc cung cấp dữ liệu người dùng cho bất kỳ bên thứ ba thương mại nào.',
    ],
  ),
  PolicySection(
    title: 'Quyền hạn của người sử dụng',
    icon: PhosphorIconsRegular.userGear,
    items: [
      'Được quyền xem, chỉnh sửa thông tin liên lạc cá nhân (họ tên, số điện thoại) và chủ động thay đổi mật khẩu tài khoản bất kỳ lúc nào.',
      'Tra cứu toàn bộ lịch sử công việc, nhật ký giờ công đã ghi và chi tiết công thức tính điểm KPI của chính mình.',
      'Được quyền bật/tắt quyền nhận thông báo đẩy trên thiết bị hoặc yêu cầu quản trị viên hỗ trợ giải đáp các vấn đề liên quan đến dữ liệu cá nhân.',
    ],
  ),
];

/// Dữ liệu toàn diện: Điều khoản sử dụng (Terms of Service)
const termsOfUseSections = [
  PolicySection(
    title: 'Trách nhiệm tài khoản & Đăng nhập',
    icon: PhosphorIconsRegular.userCircle,
    items: [
      'Tài khoản đăng nhập được cấp phát riêng cho từng cá nhân; người dùng có trách nhiệm tự bảo mật mật khẩu và không chia sẻ tài khoản cho người khác sử dụng.',
      'Nếu phát hiện dấu hiệu truy cập trái phép hoặc mất thông tin đăng nhập, người dùng cần lập tức đổi mật khẩu hoặc thông báo cho Quản trị viên để khoá tài khoản kịp thời.',
    ],
  ),
  PolicySection(
    title: 'Tính chính xác & Trung thực của dữ liệu',
    icon: PhosphorIconsRegular.checkCircle,
    items: [
      'Người dùng chịu trách nhiệm hoàn toàn về tính trung thực, chuẩn xác của các dữ liệu do mình tự khai báo (bao gồm giờ công thực hiện, trạng thái công việc, lý do xin nghỉ phép).',
      'Mọi số liệu ghi nhận trên phần mềm là căn cứ chính thức để Tổ và Đơn vị đánh giá thi đua, xếp loại năng suất KPI và thực hiện chế độ nhân sự.',
    ],
  ),
  PolicySection(
    title: 'Chuẩn mực giao tiếp & Chia sẻ tài liệu',
    icon: PhosphorIconsRegular.chatCircleText,
    items: [
      'Kênh trao đổi và bình luận chỉ phục vụ cho mục đích phối hợp công việc chuyên môn; nghiêm cấm sử dụng ngôn từ xúc phạm, thiếu văn hóa hoặc đăng tải các nội dung không phù hợp.',
      'Không tải lên các tệp tin chứa mã độc, virus hoặc các tài liệu tuyệt mật không thuộc phạm vi xử lý của dự án.',
    ],
  ),
  PolicySection(
    title: 'Quyền quản trị & Thu hồi tài khoản',
    icon: PhosphorIconsRegular.shieldWarning,
    items: [
      'Đơn vị có toàn quyền đình chỉ, thu hồi hoặc vô hiệu hóa tài khoản khi nhân sự thuyên chuyển công tác, chấm dứt hợp đồng lao động hoặc vi phạm nghiêm trọng các quy định an toàn bảo mật thông tin.',
    ],
  ),
];
