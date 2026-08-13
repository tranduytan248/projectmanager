import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../config/app_cache.dart';
import '../../core/classes/route_manager.dart';
import '../app_routes.dart';
import 'auth_routes.dart';

/// Man hinh mo dau: nen xanh thuong hieu, coc ca-phe boc khoi trong ~3 giay, roi kiem tra da
/// dang nhap hay chua de dieu huong thang toi Dashboard hoac Login. Dat lam initialRoute thay
/// vi Dashboard de tranh nhap nhay (flicker) — neu initialRoute la Dashboard, StatelessController
/// van ve PlaceholderBody cua Dashboard 1 frame truoc khi checkLogin() (chay post-frame, bat
/// dong bo) kip day nguoi dung ve Login.
class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> with TickerProviderStateMixin {
  static const _splashDuration = Duration(seconds: 3);

  /// Cung mau xanh voi icon_app.png, de splash va icon ngoai man hinh chinh la mot the thong nhat.
  static const _brandBlue = Color(0xFF03448C);

  final _appCache = AppCache();

  late final AnimationController _cupController;
  late final AnimationController _steamController;

  @override
  void initState() {
    super.initState();

    _cupController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 700),
    )..forward();

    _steamController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1800),
    )..repeat();

    Future.delayed(_splashDuration, _redirect);
  }

  @override
  void dispose() {
    _cupController.dispose();
    _steamController.dispose();
    super.dispose();
  }

  Future<void> _redirect() async {
    final isLogin = await _appCache.isLogin();
    if (!mounted) return;
    await Nav.toAndClearStack(isLogin ? AppRoutes.dashboard : AuthRoutes.login);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _brandBlue,
      body: Center(
        child: SizedBox(
          width: 220,
          height: 250,
          child: Stack(
            alignment: Alignment.bottomCenter,
            clipBehavior: Clip.none,
            children: [
              Positioned(top: 0, child: _RisingSteam(controller: _steamController)),
              ScaleTransition(
                scale: CurvedAnimation(parent: _cupController, curve: Curves.elasticOut),
                child: FadeTransition(
                  opacity: _cupController,
                  child: Image.asset('assets/images/splash_cup.png', width: 190),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Lan khoi boc len tu mieng coc: hai lop cung mot anh khoi, lech pha nua vong lap, lop nay
/// vua mo dan-boc len-tan bien thi lop kia da bat dau mo dan tu duoi len — nho vay khoi bay
/// lien tuc suot 3 giay thay vi giat cuc moi khi lap lai.
class _RisingSteam extends StatelessWidget {
  const _RisingSteam({required this.controller});

  final AnimationController controller;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 90,
      height: 70,
      child: Stack(
        alignment: Alignment.bottomCenter,
        children: [
          _SteamWisp(controller: controller, phase: 0),
          _SteamWisp(controller: controller, phase: 0.5),
        ],
      ),
    );
  }
}

class _SteamWisp extends StatelessWidget {
  const _SteamWisp({required this.controller, required this.phase});

  final AnimationController controller;
  final double phase;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, child) {
        final t = (controller.value + phase) % 1.0;
        final opacity = math.sin(t * math.pi).clamp(0.0, 1.0);
        final riseY = -30.0 * t;
        final sway = math.sin(t * math.pi * 2) * 3;

        return Opacity(
          opacity: opacity,
          child: Transform.translate(offset: Offset(sway, riseY), child: child),
        );
      },
      child: Image.asset('assets/images/splash_steam.png', width: 66),
    );
  }
}
