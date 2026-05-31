import 'package:arena_book_mobile/core/app_currency.dart';
import 'package:arena_book_mobile/models/coin_models.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class ReservationHistoryScreen extends StatefulWidget {
  const ReservationHistoryScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<ReservationHistoryScreen> createState() =>
      _ReservationHistoryScreenState();
}

class _ReservationHistoryScreenState extends State<ReservationHistoryScreen> {
  List<SessionListItem> _sessions = [];
  List<CoinLedgerEntry> _ledger = [];
  bool _loading = true;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final sessions = await widget.api.mySessions(pageSize: 100);
      final ledger = await widget.api.ledger();
      if (mounted) {
        setState(() {
          _sessions = sessions.items;
          _ledger = ledger.items;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Row(
            children: [
              Icon(Icons.history, size: 22),
              SizedBox(width: 8),
              Text('Historija rezervacija'),
            ],
          ),
          bottom: const TabBar(
            tabs: [
              Tab(icon: Icon(Icons.event_outlined), text: 'Termini'),
              Tab(icon: Icon(Icons.toll_outlined), text: 'Novčići'),
            ],
          ),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : TabBarView(
                children: [
                  RefreshIndicator(
                    onRefresh: _load,
                    child: _sessions.isEmpty
                        ? ListView(
                            children: const [
                              SizedBox(height: 48),
                              Center(child: Text('Nema rezervacija.')),
                            ],
                          )
                        : ListView.builder(
                            padding: const EdgeInsets.all(16),
                            itemCount: _sessions.length,
                            itemBuilder: (context, i) {
                              final s = _sessions[i];
                              return AppListTile(
                                icon: Icons.stadium_outlined,
                                title: s.hallName,
                                subtitle:
                                    '${_fmt.format(s.startUtc.toLocal())} · ${s.sessionLifecycleCode}',
                                trailing: NovcicChip(amount: s.priceTotalCoins),
                              );
                            },
                          ),
                  ),
                  _ledger.isEmpty
                      ? const Center(child: Text('Nema transakcija.'))
                      : ListView.builder(
                          padding: const EdgeInsets.all(16),
                          itemCount: _ledger.length,
                          itemBuilder: (context, i) {
                            final e = _ledger[i];
                            return AppListTile(
                              icon: e.amountCoins >= 0
                                  ? Icons.add_circle_outline
                                  : Icons.remove_circle_outline,
                              title: AppCurrency.format(e.amountCoins.abs()),
                              subtitle:
                                  '${e.reasonCode} · ${_fmt.format(e.createdUtc.toLocal())}',
                              trailing:
                                  Text(AppCurrency.format(e.balanceAfter)),
                            );
                          },
                        ),
                ],
              ),
      ),
    );
  }
}

