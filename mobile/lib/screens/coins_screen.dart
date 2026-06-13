import 'package:arena_book_mobile/core/app_currency.dart';
import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:arena_book_mobile/models/coin_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/services/mobile_payment_service.dart';
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
  bool _usePayPal = false;
  bool _acceptedTerms = false;
  bool _loading = true;
  bool _paying = false;
  String? _error;
  String? _coinsError;
  String? _termsError;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');
  static const double _maxCoinsPurchase = 100000;
  late final MobilePaymentService _payments = MobilePaymentService(widget.api);

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

  String? _validateCoins() {
    final text = _coinsCtrl.text.trim();
    if (text.isEmpty) {
      return 'Unesite broj novčića za kupovinu.';
    }
    final coins = double.tryParse(text.replaceAll(',', '.'));
    if (coins == null) {
      return 'Iznos mora biti valjan broj.';
    }
    if (coins <= 0) {
      return 'Iznos mora biti veći od nule.';
    }
    if (coins != coins.roundToDouble()) {
      return 'Unesite cijeli broj novčića.';
    }
    if (coins > _maxCoinsPurchase) {
      return 'Maksimalan iznos je ${AppCurrency.format(_maxCoinsPurchase)}.';
    }
    return null;
  }

  String? _validateTerms() {
    if (!_acceptedTerms) {
      return 'Morate prihvatiti uslove korištenja prije kupovine.';
    }
    return null;
  }

  bool _validatePurchaseForm() {
    final coinsError = _validateCoins();
    final termsError = _validateTerms();
    setState(() {
      _coinsError = coinsError;
      _termsError = termsError;
    });
    return coinsError == null && termsError == null;
  }

  Future<bool> _confirmPurchase(double coins) async {
    final method = _usePayPal ? 'PayPal sandbox' : 'karticu (Stripe PaymentSheet)';
    final balance = _wallet?.balanceCoins ?? 0;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.payment_outlined, size: 40),
        title: const Text('Potvrdite kupovinu'),
        content: Text(
          'Kupujete ${AppCurrency.format(coins)} putem $method.\n\n'
          'Trenutno stanje: ${AppCurrency.format(balance)}.\n'
          'Nakon uspješnog plaćanja novčići će biti dodani na vaš novčanik. '
          'Ova akcija pokreće stvarno plaćanje u sandbox okruženju.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Potvrdi kupovinu'),
          ),
        ],
      ),
    );
    return confirmed ?? false;
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
      final result = await _payments.payWithStripe(coins);
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
    } catch (e) {
      final message = e.toString();
      setState(() => _error = message);
      await _showErrorDialog(message);
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
      final result = await _payments.payWithPayPal(coins);
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
    } catch (e) {
      final message = e.toString();
      setState(() => _error = message);
      await _showErrorDialog(message);
    } finally {
      if (mounted) {
        setState(() => _paying = false);
      }
    }
  }

  Future<void> _pay() async {
    if (!_validatePurchaseForm()) {
      return;
    }

    final coins = _coinsAmount;
    if (!await _confirmPurchase(coins)) {
      return;
    }

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
                  subtitle: 'Stripe PaymentSheet ili PayPal odobrenje',
                  icon: Icons.shopping_cart_outlined,
                  tone: AppSectionTone.neutral,
                  children: [
                    TextField(
                      controller: _coinsCtrl,
                      decoration: InputDecoration(
                        labelText: 'Broj novčića za kupovinu',
                        prefixIcon: const Icon(Icons.toll_outlined),
                        helperText: _coinsError == null
                            ? '1 KM ≈ 10 novčića (postavka platforme)'
                            : null,
                        errorText: _coinsError,
                      ),
                      keyboardType: TextInputType.number,
                      onChanged: (_) {
                        if (_coinsError != null) {
                          setState(() => _coinsError = _validateCoins());
                        }
                      },
                    ),
                    const SizedBox(height: 8),
                    SwitchListTile(
                      secondary: Icon(_usePayPal
                          ? Icons.account_balance_wallet_outlined
                          : Icons.credit_card_outlined),
                      title: const Text('PayPal (sandbox)'),
                      subtitle: const Text(
                          'Preusmjerenje na PayPal, zatim povratak u aplikaciju'),
                      value: _usePayPal,
                      onChanged: (v) => setState(() => _usePayPal = v),
                    ),
                    if (!_usePayPal)
                      const ListTile(
                        dense: true,
                        leading: Icon(Icons.info_outline),
                        title: Text('Stripe PaymentSheet'),
                        subtitle: Text(
                          'Podaci kartice (broj, datum isteka, CVC) unose se u '
                          'zaštićenom Stripe prozoru.\n'
                          'Test kartica: 4242 4242 4242 4242, bilo koji budući '
                          'datum isteka i CVC.',
                        ),
                      )
                    else
                      const ListTile(
                        dense: true,
                        leading: Icon(Icons.info_outline),
                        title: Text('PayPal sandbox'),
                        subtitle: Text(
                          'Otvara se PayPal sandbox (USD). Prijavite se Personal sandbox '
                          'kupcem (ne Business). Nakon potvrde vraćate se u aplikaciju.',
                        ),
                      ),
                    CheckboxListTile(
                      value: _acceptedTerms,
                      onChanged: (v) => setState(() {
                        _acceptedTerms = v ?? false;
                        if (_termsError != null) {
                          _termsError = _validateTerms();
                        }
                      }),
                      title: const Text('Prihvatam uslove korištenja'),
                      subtitle: _termsError != null
                          ? Text(
                              _termsError!,
                              style: TextStyle(
                                color: Theme.of(context).colorScheme.error,
                              ),
                            )
                          : null,
                      controlAffinity: ListTileControlAffinity.leading,
                    ),
                    const SizedBox(height: 8),
                    FilledButton.icon(
                      onPressed: _paying ? null : _pay,
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
