abstract final class AppCurrency {
  static const tabLabel = 'Novčići';
  static const shopTitle = 'Novčanik';
  static const walletLabel = 'Stanje novčanika';

  static String format(double amount, {bool perHour = false}) {
    final text = amount == amount.roundToDouble()
        ? amount.toInt().toString()
        : amount.toStringAsFixed(2);
    final suffix = perHour ? '/h' : '';
    return '$text ${_unit(amount)}$suffix';
  }

  static String _unit(double amount) {
    final n = amount.abs().round();
    final mod10 = n % 10;
    final mod100 = n % 100;
    if (mod10 == 1 && mod100 != 11) return 'novčić';
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) {
      return 'novčića';
    }
    return 'novčića';
  }
}

