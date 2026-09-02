import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../config/app_theme.dart';

/// Bieu tuong dang tai hinh LY CA PHE dung CHUNG toan app — theo `.claude/rules/FLUTTER_RULES.md`,
/// man hinh KHONG duoc dung CircularProgressIndicator truc tiep, luon phai qua AppLoading.
///
/// Ly dung yen (khong nay, khong doi mau) — chi co khoi boc len tren mieng ly, lap lai lien tuc,
/// dung ca hai anh da co san o man Splash (`splash_cup.png`/`splash_steam.png`, deu la khoi trang
/// dac nen trong suot) nhuom lai theo mau [color] bang ColorFiltered/BlendMode.srcIn.
class AppLoading extends StatefulWidget {
  const AppLoading(
      {super.key, this.size = 40, this.color, this.strokeWidth = 2.5});

  final double size;
  final Color? color;

  /// Giu lai de tuong thich voi cac noi da goi AppLoading(strokeWidth: ...) truoc day (vi du nut
  /// Dang nhap) — bieu tuong ly ca phe khong con net ve dang vong tron nen khong dung gia tri nay.
  final double strokeWidth;

  @override
  State<AppLoading> createState() => _AppLoadingState();
}

class _AppLoadingState extends State<AppLoading>
    with SingleTickerProviderStateMixin {
  late final AnimationController _steamController;

  @override
  void initState() {
    super.initState();
    _steamController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2200),
    )..repeat();
  }

  @override
  void dispose() {
    _steamController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final color = widget.color ?? AppTheme.brandBlue;

    // Cao hon chieu rong mot chut de co cho khoi boc len tren mieng ly ma khong bi cat.
    return Semantics(
      label: 'Đang tải dữ liệu...',
      child: SizedBox(
        width: widget.size,
        height: widget.size * 1.35,
        child: Stack(
          alignment: Alignment.bottomCenter,
          clipBehavior: Clip.none,
          children: [
            Positioned(
              top: 0,
              child: _RisingSteam(
                  controller: _steamController, color: color, size: widget.size),
            ),
            _CupIcon(size: widget.size, color: color),
          ],
        ),
      ),
    );
  }
}

/// Ly ca phe dung yen, mot mau — tai su dung anh coc cua man Splash.
class _CupIcon extends StatelessWidget {
  const _CupIcon({required this.size, required this.color});

  final double size;
  final Color color;

  static const _asset = 'assets/images/splash_cup.png';

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: ColorFiltered(
        colorFilter: ColorFilter.mode(color, BlendMode.srcIn),
        child: Image.asset(_asset, fit: BoxFit.contain, excludeFromSemantics: true),
      ),
    );
  }
}

/// Hai lan khoi cung anh, lech pha nua vong lap — lop nay vua mo dan-boc len-tan bien thi lop kia
/// da bat dau mo dan tu duoi len, nho vay khoi bay lien tuc khong giat cuc moi khi lap lai. Y het
/// ky thuat man Splash (`splash_screen.dart._SteamWisp`) nhung nhuom theo mau ly thay vi de trang.
class _RisingSteam extends StatelessWidget {
  const _RisingSteam(
      {required this.controller, required this.color, required this.size});

  final AnimationController controller;
  final Color color;
  final double size;

  @override
  Widget build(BuildContext context) {
    final steamWidth = size * 0.6;

    return SizedBox(
      width: steamWidth,
      height: size * 0.5,
      child: Stack(
        alignment: Alignment.bottomCenter,
        // Khoi bay vuot mep hop khi dang o dinh chuyen dong — luc do opacity da gan 0 (xem cong
        // thuc trong _SteamWisp) nen khong clip cung khong sao, nhung ANH mac dinh cua Stack la
        // tu cat theo mep (Clip.hardEdge) se cat cut phan con nhin thay duoc o giua chu ky — bo
        // clip de khoi tan mo tu nhien, dung y het Stack ngoai cung cua AppLoading.
        clipBehavior: Clip.none,
        children: [
          _SteamWisp(
              controller: controller,
              phase: 0,
              color: color,
              width: steamWidth),
          _SteamWisp(
              controller: controller,
              phase: 0.5,
              color: color,
              width: steamWidth),
        ],
      ),
    );
  }
}

class _SteamWisp extends StatelessWidget {
  const _SteamWisp({
    required this.controller,
    required this.phase,
    required this.color,
    required this.width,
  });

  final AnimationController controller;
  final double phase;
  final Color color;
  final double width;

  static const _asset = 'assets/images/splash_steam.png';

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, child) {
        final t = (controller.value + phase) % 1.0;
        // sin uon nhe o ca hai dau (mo/tan cham hon o giua vong lap) thay vi boc/tan deu deu.
        final eased = Curves.easeInOut.transform(t);
        final opacity = math.sin(t * math.pi).clamp(0.0, 1.0) * 0.7;
        final riseY = -width * 0.55 * eased;
        final sway = math.sin(t * math.pi * 2) * (width * 0.05);

        return Opacity(
          opacity: opacity,
          child: Transform.translate(offset: Offset(sway, riseY), child: child),
        );
      },
      child: ColorFiltered(
        colorFilter: ColorFilter.mode(color, BlendMode.srcIn),
        child: Image.asset(_asset, width: width, excludeFromSemantics: true),
      ),
    );
  }
}
