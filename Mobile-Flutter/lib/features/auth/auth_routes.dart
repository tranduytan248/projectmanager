import '../../core/classes/route_manager.dart';
import '../../core/constants/route_names.dart';
import 'login_screen.dart';

class AuthRoutes extends RouteManager {
  static const String name = 'Auth';
  static const String login = CoreRouteNames.login;

  AuthRoutes() {
    addRoute(login, (context) => const LoginScreen());
  }
}
