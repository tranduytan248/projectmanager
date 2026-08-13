import '../../core/classes/route_manager.dart';
import '../../core/constants/route_names.dart';
import 'login_screen.dart';
import 'splash_screen.dart';

class AuthRoutes extends RouteManager {
  static const String name = 'Auth';
  static const String login = CoreRouteNames.login;
  static const String splash = '$name/splash';

  AuthRoutes() {
    addRoute(splash, (context) => const SplashScreen());
    addRoute(login, (context) => const LoginScreen());
  }
}
