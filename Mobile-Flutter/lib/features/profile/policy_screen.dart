import 'package:flutter/material.dart';

/// Man tinh dung chung cho "Chinh sach bao mat" va "Dieu khoan su dung" — noi dung noi bo,
/// chua co API rieng nen viet thang trong app thay vi tai tu server.
class PolicyScreen extends StatelessWidget {
  const PolicyScreen({super.key, required this.title, required this.paragraphs});

  final String title;
  final List<String> paragraphs;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            for (final p in paragraphs) ...[
              Text(p, style: const TextStyle(fontSize: 14, height: 1.6, color: Colors.black87)),
              const SizedBox(height: 14),
            ],
          ],
        ),
      ),
    );
  }
}

const privacyPolicyParagraphs = [
  'Ứng dụng BrewTask là công cụ quản lý dự án và công việc nội bộ của TTKDGP, chỉ cấp tài '
      'khoản cho nhân sự trong đơn vị — không mở đăng ký công khai.',
  'Dữ liệu được lưu trữ: thông tin tài khoản (họ tên, vai trò), dữ liệu công việc (dự án, đầu '
      'việc, giờ công, KPI), đơn nghỉ phép, và nội dung trao đổi trong từng công việc.',
  'Dữ liệu chỉ phục vụ hoạt động quản lý nội bộ (giao việc, chấm công, tính KPI, duyệt nghỉ '
      'phép) — không chia sẻ cho bên thứ ba ngoài mục đích này.',
  'Ứng dụng dùng Firebase Analytics để ghi nhận số liệu sử dụng chung (màn hình được mở, thời '
      'gian dùng), không thu thập nội dung công việc hay dữ liệu cá nhân nhạy cảm khác.',
  'Mọi thắc mắc về dữ liệu cá nhân, liên hệ quản trị hệ thống của đơn vị.',
];

const termsOfUseParagraphs = [
  'Tài khoản đăng nhập là tài sản của cá nhân được cấp — không chia sẻ mật khẩu hoặc dùng '
      'chung tài khoản với người khác.',
  'Thông tin công việc, giờ công và nội dung trao đổi ghi nhận trong ứng dụng được xem là căn '
      'cứ chính thức phục vụ đánh giá KPI và quản lý nhân sự.',
  'Người dùng chịu trách nhiệm về tính chính xác của dữ liệu tự khai (giờ công, nội dung công '
      'việc, đơn nghỉ phép).',
  'Nghiêm cấm dùng ứng dụng để đăng tải nội dung sai sự thật, xúc phạm, hoặc không liên quan '
      'đến công việc.',
  'Đơn vị có quyền thu hồi tài khoản khi người dùng nghỉ việc hoặc vi phạm các điều khoản trên.',
];
