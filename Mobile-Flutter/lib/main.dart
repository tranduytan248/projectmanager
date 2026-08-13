import 'package:flutter/material.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'config/app_providers.dart';
import 'core/classes/cache_manager.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Hive.initFlutter();
  await Cache.init();

  runApp(MultiProvider(providers: appProviders, child: const TtkdgpApp()));
}
