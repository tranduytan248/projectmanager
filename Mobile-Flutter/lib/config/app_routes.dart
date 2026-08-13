import 'package:flutter/material.dart';

import '../features/app_routes.dart';
import '../features/auth/auth_routes.dart';

/// Gop bang route cua tung module thanh mot map phang duy nhat cho MaterialApp.routes.
class Routes {
  Map<String, WidgetBuilder> get routes => {
        ...AuthRoutes().routes,
        ...AppRoutes().routes,
      };
}
