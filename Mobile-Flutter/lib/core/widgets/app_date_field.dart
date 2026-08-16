import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import 'app_text_field.dart';

/// O chon ngay chuan cua du an — dung `.claude/rules/FLUTTER_RULES.md`. Du an chua co widget chon
/// ngay nao truoc day, man hinh KHONG duoc tu goi showDatePicker/DatePicker truc tiep, luon qua
/// AppDateField. Che giau hoan toan showDatePicker cua Material ben trong; ben ngoai chi thay mot
/// AppTextField chi-doc (readOnly, dong bo giao dien voi o nhap thuong — xem app_text_field.dart)
/// mo lich khi bam vao.
class AppDateField extends StatefulWidget {
  const AppDateField({
    super.key,
    required this.label,
    required this.value,
    required this.onChanged,
    this.firstDate,
    this.lastDate,
    this.required = false,
    this.hint = 'Chọn ngày',
  });

  final String label;
  final DateTime? value;
  final ValueChanged<DateTime> onChanged;

  /// Ngay som nhat/muon nhat chon duoc. Vi du form ghi gio cong phai chan [lastDate] bang hom nay
  /// vi may chu tu choi ghi gio cho ngay tuong lai (xem TimeLogService.Validate ben backend).
  final DateTime? firstDate;
  final DateTime? lastDate;
  final bool required;
  final String hint;

  @override
  State<AppDateField> createState() => _AppDateFieldState();
}

class _AppDateFieldState extends State<AppDateField> {
  late final TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: _format(widget.value));
  }

  @override
  void didUpdateWidget(covariant AppDateField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.value != widget.value) {
      _controller.text = _format(widget.value);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  String _format(DateTime? date) =>
      date == null ? '' : DateFormat('dd/MM/yyyy').format(date);

  Future<void> _pick() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: widget.value ?? widget.lastDate ?? now,
      firstDate: widget.firstDate ?? DateTime(now.year - 5),
      lastDate: widget.lastDate ?? DateTime(now.year + 5),
    );
    if (picked != null) widget.onChanged(picked);
  }

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      label: widget.label,
      required: widget.required,
      hint: widget.hint,
      controller: _controller,
      readOnly: true,
      onTap: _pick,
      suffixIcon: PhosphorIconsRegular.calendarBlank,
    );
  }
}
