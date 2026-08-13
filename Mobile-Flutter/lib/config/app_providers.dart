import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';

import '../features/auth/auth_provider.dart';
import 'theme_provider.dart';

/// Danh sach ChangeNotifier dang ky mot lan duy nhat, dung trong MultiProvider o main.dart.
final appProviders = <SingleChildWidget>[
  ChangeNotifierProvider(create: (_) => ThemeProvider()),
  ChangeNotifierProvider(create: (_) => AuthProvider()),
];
