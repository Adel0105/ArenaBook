import 'package:arena_book_mobile/core/app_currency.dart';
import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:arena_book_mobile/models/coin_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class CoinsScreen extends StatefulWidget {
  const CoinsScreen({
    super.key,
    required this.api,
    this.isActive = false,
  });

  final ArenaBookApi api;
  final bool isActive;

  @override
  State<CoinsScreen> createState() => _CoinsScreenState();
}

class _CoinsScreenState extends State<CoinsScreen> with WidgetsBindingObserver {
  CoinWallet? _wallet;
  List<CoinLedgerEntry> _ledger = [];
  final _coinsCtrl = TextEditingController(text: '50');
  final _cardCtrl = TextEditingController(text: '4242 4242 4242 4242');
  final _expiryCtrl = TextEditingController(text: '12/34');
  final _cvcCtrl = TextEditingController(text: '123');
  bool _usePayPal = false;
  bool _acceptedTerms = false;
  bool _loading = true;
  bool _paying = false;
  String? _error;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    if (widget.isActive) {
      _load();
    } else {
      _loading = false;
    }
  }

  @override
  void didUpdateWidget(CoinsScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.isActive && !oldWidget.isActive) {
      _load();
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && widget.isActive) {
      _load();
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _coinsCtrl.dispose();
    _cardCtrl.dispose();
    _expiryCtrl.dispose();
    _cvcCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    CoinWallet? wallet;
    List<CoinLedgerEntry> ledger = [];
    final errors = <String>[];

    try {
      wallet = await widget.api.wallet();
    } catch (e) {
      errors.add(e is ApiError ? e.message : 'Novčanik nije učitan.');
    }

    try {
      final page = await widget.api.ledger();
      ledger = page.items;
    } catch (e) {
      errors.add(e is ApiError ? e.message : 'Historija transakcija nije učitana.');
    }

    if (!mounted) return;
    setState(() {
      if (wallet != null) {
        _wallet = wallet;
      }
      _ledger = ledger;
      _error = errors.isEmpty ? null : errors.join('\n');
      _loading = false;
    });
  }

  void _applyPurchaseResult(CoinPurchaseResult result) {
    setState(() {
      _wallet = CoinWallet(
        balanceCoins: result.balanceCoins,
        updatedUtc: DateTime.now().toUtc(),
      );
    });
  }

  double get _coinsAmount =>
      double.tryParse(_coinsCtrl.text.replaceAll(',', '.')) ?? 0;

  bool get _canPay =>
      _acceptedTerms && _coinsAmount > 0 && (!_usePayPal && _cardValid || _usePayPal);

  bool get _cardValid {
    final card = _cardCtrl.text.replaceAll(' ', '');
    return card.length >= 13 &&
        _expiryCtrl.text.contains('/') &&
        _cvcCtrl.text.length >= 3;
  }

  Future<void> _showSuccessDialog(String message) async {
    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.check_circle_outline, color: AppColors.primaryGreen, size: 48),
        title: const Text('Uspješna kupovina'),
        content: Text(message),
        actions: [
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('U redu'),
          ),
        ],
      ),
    );
  }

  Future<void> _showErrorDialog(String message) async {
    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: Icon(Icons.error_outline, color: Theme.of(ctx).colorScheme.error, size: 48),
        title: const Text('Plaćanje nije uspjelo'),
        content: Text(message),
        actions: [
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('U redu'),
          ),
        ],
      ),
    );
  }

  Future<void> _payStripe() async {
    final coins = _coinsAmount;
    setState(() {
      _paying = true;
      _error = null;
    });

    try {
      final result = await widget.api.stripeConfirmSandboxPurchase(coins);
      if (!mounted) return;
      _applyPurchaseResult(result);
      await _showSuccessDialog(
        'Uspješno ste dokupili ${AppCurrency.format(coins)}. '
        'Novo stanje: ${AppCurrency.format(result.balanceCoins)}.',
      );
      await _load();
    } on ApiError catch (e) {
      setState(() => _error = e.message);
      await _showErrorDialog(e.message);
    } finally {
      if (mounted) {
        setState(() => _paying = false);
      }
    }
  }

  Future<void> _payPayPal() async {
    final coins = _coinsAmount;
    setState(() {
      _paying = true;
      _error = null;
    });

    try {
      final result = await widget.api.paypalConfirmSandbox(coins);
      if (!mounted) return;
      _applyPurchaseResult(result);
      await _showSuccessDialog(
        'Uspješno ste dokupili ${AppCurrency.format(coins)}. '
        'Novo stanje: ${AppCurrency.format(result.balanceCoins)}.',
      );
      await _load();
    } on ApiError catch (e) {
      setState(() => _error = e.message);
      await _showErrorDialog(e.message);
    } finally {
      if (mounted) {
        setState(() => _paying = false);
      }
    }
  }

  Future<void> _pay() async {
    if (!_canPay) return;
    if (_usePayPal) {
      await _payPayPal();
    } else {
      await _payStripe();
    }
  }

  @override
  Widget build(BuildContext context) {
    final balance = _wallet?.balanceCoins ?? 0;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const Icon(Icons.toll_outlined, size: 22),
            const SizedBox(width: 8),
            Text(AppCurrency.shopTitle),
          ],
        ),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.fromLTRB(0, 8, 0, 24),
              children: [
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
                    child: Text(
                      _error!,
                      style: TextStyle(color: Theme.of(context).colorScheme.error),
                    ),
                  ),
                AppSection(
                  title: AppCurrency.walletLabel,
                  subtitle: 'Za rezervacije termina',
                  icon: Icons.account_balance_wallet_outlined,
                  tone: AppSectionTone.mint,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.toll,
                            size: 36, color: AppColors.forest),
                        const SizedBox(width: 12),
                        Text(
                          AppCurrency.format(balance),
                          style: Theme.of(context)
                              .textTheme
                              .headlineSmall
                              ?.copyWith(
                                fontWeight: FontWeight.w700,
                                color: AppColors.forest,
                              ),
                        ),
                      ],
                    ),
                  ],
                ),
                AppSection(
                  title: 'Kupovina novčića',
                  subtitle: 'Stripe PaymentIntent / PayPal narudžba u aplikaciji',
                  icon: Icons.shopping_cart_outlined,
                  tone: AppSectionTone.neutral,
                  children: [
                    TextField(
                      controller: _coinsCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Broj novčića za kupovinu',
                        prefixIcon: Icon(Icons.toll_outlined),
                        helperText: '1 KM ≈ 10 novčića (postavka platforme)',
                      ),
                      keyboardType: TextInputType.number,
                    ),
                    const SizedBox(height: 8),
                    SwitchListTile(
                      secondary: Icon(_usePayPal
                          ? Icons.account_balance_wallet_outlined
                          : Icons.credit_card_outlined),
                      title: const Text('PayPal (sandbox)'),
                      subtitle: const Text(
                          'Simulacija uplate — novčići se dodaju odmah'),
                      value: _usePayPal,
                      onChanged: (v) => setState(() => _usePayPal = v),
                    ),
                    if (!_usePayPal) ...[
                      const ListTile(
                        dense: true,
                        leading: Icon(Icons.info_outline),
                        title: Text('Testna kartica (Stripe sandbox)'),
                        subtitle: Text(
                          '4242 4242 4242 4242 · 12/34 · CVC 123\n'
                          'Korak 1: PaymentIntent · Korak 2: potvrda u sandboxu.',
                        ),
                      ),
                      TextField(
                        controller: _cardCtrl,
                        decoration: const InputDecoration(
                          labelText: 'Broj kartice',
                          prefixIcon: Icon(Icons.credit_card),
                        ),
                        keyboardType: TextInputType.number,
                      ),
                      TextField(
                        controller: _expiryCtrl,
                        decoration: const InputDecoration(labelText: 'MM/GG'),
                      ),
                      TextField(
                        controller: _cvcCtrl,
                        decoration: const InputDecoration(labelText: 'CVC'),
                        obscureText: true,
                      ),
                    ] else
                      const ListTile(
                        dense: true,
                        leading: Icon(Icons.info_outline),
                        title: Text('PayPal sandbox'),
                        subtitle: Text(
                          'Korak 1: kreiranje narudžbe · Korak 2: capture u sandboxu.',
                        ),
                      ),
                    CheckboxListTile(
                      value: _acceptedTerms,
                      onChanged: (v) =>
                          setState(() => _acceptedTerms = v ?? false),
                      title: const Text('Prihvatam uslove korištenja'),
                    ),
                    const SizedBox(height: 8),
                    FilledButton.icon(
                      onPressed: _paying || !_canPay ? null : _pay,
                      icon: _paying
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : Icon(_usePayPal
                              ? Icons.account_balance_wallet_outlined
                              : Icons.payment),
                      label: Text(_usePayPal
                          ? 'Kupi novčiće (PayPal)'
                          : 'Kupi novčiće (kartica)'),
                    ),
                  ],
                ),
                AppSection(
                  title: 'Historija transakcija',
                  subtitle: 'Zadnje promjene na novčaniku',
                  icon: Icons.history,
                  tone: AppSectionTone.slate,
                  children: _ledger.isEmpty
                      ? [const Text('Nema transakcija.')]
                      : _ledger
                          .map(
                            (e) => AppListTile(
                              icon: e.amountCoins >= 0
                                  ? Icons.add_circle_outline
                                  : Icons.remove_circle_outline,
                              title: AppCurrency.format(e.amountCoins.abs()),
                              subtitle:
                                  '${e.reasonCode} · ${_fmt.format(e.createdUtc.toLocal())}',
                              trailing: Text(
                                AppCurrency.format(e.balanceAfter),
                                style: const TextStyle(
                                    fontWeight: FontWeight.w600),
                              ),
                            ),
                          )
                          .toList(),
                ),
              ],
            ),
    );
  }
}

