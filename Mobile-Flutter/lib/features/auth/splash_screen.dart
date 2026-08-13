import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../config/app_cache.dart';
import '../../config/app_theme.dart';
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

class _SplashScreenState extends State<SplashScreen>
    with TickerProviderStateMixin {
  static const _splashDuration = Duration(seconds: 3);

  final _appCache = AppCache();

  late final AnimationController _cupController;
  late final AnimationController _steamController;

  @override
  void initState() {
    super.initState();

    // An thanh trang thai/dieu huong trong luc splash de ca man hinh la mot manh xanh lien
    // mach (khop voi splash nguyen sinh cua OS, cung dang an status bar) — hien lai truoc khi
    // sang Login/Dashboard o _redirect, khong thi cac man sau cung bi mat thanh trang thai theo.
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);

    // Khong bounce (bo Curves.elasticOut cu) — mo dan + lon dan nhe nhang cho diu mat, giong
    // cach native splash (flutter_native_splash) dung im icon roi Flutter tiep quan nhe nhang.
    _cupController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    )..forward();

    _steamController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2400),
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
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
    final isLogin = await _appCache.isLogin();
    if (!mounted) return;
    await Nav.toAndClearStack(isLogin ? AppRoutes.dashboard : AuthRoutes.login);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.brandBlue,
      body: Center(
        child: SizedBox(
          width: 220,
          height: 250,
          child: Stack(
            alignment: Alignment.bottomCenter,
            clipBehavior: Clip.none,
            children: [
              Positioned(
                  top: 0, child: _RisingSteam(controller: _steamController)),
              ScaleTransition(
                scale: Tween(begin: 0.88, end: 1.0).animate(
                  CurvedAnimation(
                      parent: _cupController, curve: Curves.easeOutCubic),
                ),
                child: FadeTransition(
                  opacity: CurvedAnimation(
                      parent: _cupController, curve: Curves.easeOut),
                  child:
                      Image.asset('assets/images/splash_cup.png', width: 190),
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
        // sin uon nhe o ca hai dau (mo/tan cham hon o giua vong lap) thay vi boc/tan deu deu,
        // trong tu nhien hon; bien do bay len va lac ngang cung giam de dang nhin nhe nhang.
        final eased = Curves.easeInOut.transform(t);
        final opacity = math.sin(t * math.pi).clamp(0.0, 1.0) * 0.85;
        final riseY = -22.0 * eased;
        final sway = math.sin(t * math.pi * 2) * 2;

        return Opacity(
          opacity: opacity,
          child: Transform.translate(offset: Offset(sway, riseY), child: child),
        );
      },
      child: Image.asset('assets/images/splash_steam.png', width: 66),
    );
  }
}
