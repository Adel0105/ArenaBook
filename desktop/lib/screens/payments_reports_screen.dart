import 'package:arena_book_desktop/models/finance_models.dart';
import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/session_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/paged_footer.dart';
import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class PaymentsReportsScreen extends StatefulWidget {
  const PaymentsReportsScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<PaymentsReportsScreen> createState() => _PaymentsReportsScreenState();
}

class _PaymentsReportsScreenState extends State<PaymentsReportsScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;
  final _searchCtrl = TextEditingController();
  List<ReferenceItem> _paymentStatuses = [];
  int? _statusFilterId;
  DateTime? _filterFrom;
  DateTime? _filterTo;
  DateTime? _reportFrom;
  DateTime? _reportTo;
  bool _downloadingReport = false;
  bool _excludeDemoSeed = false;

  PagedList<ExternalPaymentListItem>? _paymentsPage;
  PagedList<CoinLedgerListItem>? _ledgerPage;
  PagedList<CoinWalletListItem>? _walletsPage;
  List<HallEarningsListItem> _hallEarnings = [];
  int _paymentsPageNum = 1;
  int _ledgerPageNum = 1;
  int _walletsPageNum = 1;
  bool _loading = true;
  String? _error;

  static final _dateFmt = DateFormat('dd.MM.yyyy HH:mm');
  static final _moneyFmt = NumberFormat('#,##0.00');

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: 4, vsync: this);
    _tabs.addListener(() {
      if (!_tabs.indexIsChanging) {
        _loadCurrentTab();
      }
    });
    _bootstrap();
  }

  @override
  void dispose() {
    _tabs.dispose();
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    try {
      final statuses = await widget.api.paymentProcessingStatuses();
      if (!mounted) {
        return;
      }
      setState(() => _paymentStatuses = statuses.items);
    } catch (_) {}
    await _loadCurrentTab();
  }

  Future<void> _loadCurrentTab() async {
    switch (_tabs.index) {
      case 0:
        await _loadPayments();
        break;
      case 1:
        await _loadLedger();
        break;
      case 2:
        await _loadWallets();
        break;
      case 3:
        await _loadHallEarnings();
        break;
    }
  }

  Future<void> _loadHallEarnings() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final items = await widget.api.adminHallEarnings(
        dateFrom: _filterFrom,
        dateTo: _filterTo,
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _hallEarnings = items;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  Future<void> _loadPayments({int? page}) async {
    final p = page ?? _paymentsPageNum;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.api.adminExternalPayments(
        page: p,
        pageSize: 25,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
        paymentProcessingStatusId: _statusFilterId,
        dateFrom: _filterFrom,
        dateTo: _filterTo,
        excludeDemoSeed: _excludeDemoSeed,
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _paymentsPage = res;
        _paymentsPageNum = p;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Nije moguće učitati uplate.';
        _loading = false;
      });
    }
  }

  Future<void> _loadLedger({int? page}) async {
    final p = page ?? _ledgerPageNum;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.api.adminCoinLedger(
        page: p,
        pageSize: 25,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
        dateFrom: _filterFrom,
        dateTo: _filterTo,
        excludeDemoSeed: _excludeDemoSeed,
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _ledgerPage = res;
        _ledgerPageNum = p;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Nije moguće učitati knjigu koina.';
        _loading = false;
      });
    }
  }

  Future<void> _loadWallets({int? page}) async {
    final p = page ?? _walletsPageNum;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.api.adminCoinWallets(
        page: p,
        pageSize: 25,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _walletsPage = res;
        _walletsPageNum = p;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Nije moguće učitati novčanike.';
        _loading = false;
      });
    }
  }

  Future<void> _pickDate(bool isFrom, {bool forReport = false}) async {
    final initial = (forReport ? (isFrom ? _reportFrom : _reportTo) : (isFrom ? _filterFrom : _filterTo)) ??
        DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
    );
    if (picked == null || !mounted) {
      return;
    }
    setState(() {
      if (forReport) {
        if (isFrom) {
          _reportFrom = picked;
        } else {
          _reportTo = DateTime(picked.year, picked.month, picked.day, 23, 59, 59);
        }
      } else {
        if (isFrom) {
          _filterFrom = picked;
        } else {
          _filterTo = DateTime(picked.year, picked.month, picked.day, 23, 59, 59);
        }
      }
    });
    if (!forReport) {
      await _loadCurrentTab();
    }
  }

  void _clearFilters() {
    setState(() {
      _searchCtrl.clear();
      _statusFilterId = null;
      _filterFrom = null;
      _filterTo = null;
      _excludeDemoSeed = false;
      _paymentsPageNum = 1;
      _ledgerPageNum = 1;
      _walletsPageNum = 1;
    });
    _loadCurrentTab();
  }

  Future<void> _downloadReport({
    required String path,
    required String suggestedName,
    required String label,
  }) async {
    setState(() => _downloadingReport = true);
    try {
      final bytes = await widget.api.downloadAdminReportPdf(
        path,
        dateFrom: _reportFrom,
        dateTo: _reportTo,
      );
      final location = await getSaveLocation(
        suggestedName: suggestedName,
        acceptedTypeGroups: const [
          XTypeGroup(label: 'PDF', extensions: ['pdf']),
        ],
      );
      if (location == null) {
        return;
      }
      final file = XFile.fromData(bytes, mimeType: 'application/pdf', name: suggestedName);
      await file.saveTo(location.path);
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$label preuzet.')),
      );
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
    } catch (_) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Preuzimanje izvještaja nije uspjelo.')),
      );
    } finally {
      if (mounted) {
        setState(() => _downloadingReport = false);
      }
    }
  }

  Color _statusColor(String? code) {
    final c = (code ?? '').toLowerCase();
    if (c.contains('complete') || c.contains('success')) {
      return const Color(0xFF16A34A);
    }
    if (c.contains('pending') || c.contains('wait')) {
      return const Color(0xFFCA8A04);
    }
    if (c.contains('cancel') || c.contains('fail')) {
      return const Color(0xFFDC2626);
    }
    return const Color(0xFF64748B);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 24, 24, 0),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Uplate i izvještaji',
                      style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Uplate s mobilne aplikacije (Stripe/PayPal) dolaze u isti API i prikazuju se iznad demo podataka.',
                      style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                  ],
                ),
              ),
              IconButton(
                tooltip: 'Osvježi',
                onPressed: _loading ? null : _loadCurrentTab,
                icon: const Icon(Icons.refresh),
              ),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: TabBar(
            controller: _tabs,
            tabs: const [
              Tab(text: 'Vanjske uplate'),
              Tab(text: 'Knjiga koina'),
              Tab(text: 'Novčanici'),
              Tab(text: 'Zarada dvorana'),
            ],
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 12, 24, 0),
          child: _FilterBar(
            searchCtrl: _searchCtrl,
            statusFilterId: _statusFilterId,
            paymentStatuses: _paymentStatuses,
            showStatusFilter: _tabs.index == 0,
            showAppOnlyFilter: _tabs.index == 0 || _tabs.index == 1,
            excludeDemoSeed: _excludeDemoSeed,
            filterFrom: _filterFrom,
            filterTo: _filterTo,
            onPickFrom: () => _pickDate(true),
            onPickTo: () => _pickDate(false),
            onStatusChanged: (v) {
              setState(() => _statusFilterId = v);
              _loadCurrentTab();
            },
            onExcludeDemoSeedChanged: (v) {
              setState(() => _excludeDemoSeed = v);
              _loadCurrentTab();
            },
            onClearFilters: _clearFilters,
            onSearch: () {
              _paymentsPageNum = 1;
              _ledgerPageNum = 1;
              _walletsPageNum = 1;
              _loadCurrentTab();
            },
          ),
        ),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
            child: Material(
              color: theme.colorScheme.errorContainer,
              borderRadius: BorderRadius.circular(8),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Text(_error!, style: TextStyle(color: theme.colorScheme.onErrorContainer)),
              ),
            ),
          ),
        Expanded(
          child: _loading
              ? const Center(child: CircularProgressIndicator())
              : TabBarView(
                  controller: _tabs,
                  children: [
                    _PaymentsTable(
                      page: _paymentsPage,
                      dateFmt: _dateFmt,
                      moneyFmt: _moneyFmt,
                      statusColor: _statusColor,
                      onPage: _loadPayments,
                    ),
                    _LedgerTable(page: _ledgerPage, dateFmt: _dateFmt, onPage: _loadLedger),
                    _WalletsTable(page: _walletsPage, dateFmt: _dateFmt, onPage: _loadWallets),
                    _HallEarningsTable(items: _hallEarnings, moneyFmt: _moneyFmt),
                  ],
                ),
        ),
        _ReportsPanel(
          reportFrom: _reportFrom,
          reportTo: _reportTo,
          downloading: _downloadingReport,
          dateFmt: _dateFmt,
          onPickFrom: () => _pickDate(true, forReport: true),
          onPickToEnd: () => _pickDate(false, forReport: true),
          onSessions: () => _downloadReport(
            path: '/api/admin/reports/sessions/pdf',
            suggestedName: 'arena-book-rezervacije.pdf',
            label: 'Izvještaj o rezervacijama',
          ),
          onTransactions: () => _downloadReport(
            path: '/api/admin/reports/transactions/pdf',
            suggestedName: 'arena-book-transakcije.pdf',
            label: 'Izvještaj o transakcijama',
          ),
          onUsers: () => _downloadReport(
            path: '/api/admin/reports/users/pdf',
            suggestedName: 'arena-book-korisnici.pdf',
            label: 'Izvještaj o korisnicima',
          ),
        ),
      ],
    );
  }
}

class _FilterBar extends StatelessWidget {
  const _FilterBar({
    required this.searchCtrl,
    required this.statusFilterId,
    required this.paymentStatuses,
    required this.showStatusFilter,
    required this.showAppOnlyFilter,
    required this.excludeDemoSeed,
    required this.filterFrom,
    required this.filterTo,
    required this.onPickFrom,
    required this.onPickTo,
    required this.onStatusChanged,
    required this.onExcludeDemoSeedChanged,
    required this.onClearFilters,
    required this.onSearch,
  });

  final TextEditingController searchCtrl;
  final int? statusFilterId;
  final List<ReferenceItem> paymentStatuses;
  final bool showStatusFilter;
  final bool showAppOnlyFilter;
  final bool excludeDemoSeed;
  final DateTime? filterFrom;
  final DateTime? filterTo;
  final VoidCallback onPickFrom;
  final VoidCallback onPickTo;
  final ValueChanged<int?> onStatusChanged;
  final ValueChanged<bool> onExcludeDemoSeedChanged;
  final VoidCallback onClearFilters;
  final VoidCallback onSearch;

  static final _shortDate = DateFormat('dd.MM.yyyy');

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 12,
      runSpacing: 8,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        SizedBox(
          width: 220,
          child: TextField(
            controller: searchCtrl,
            decoration: const InputDecoration(
              labelText: 'Pretraga (e-mail)',
              hintText: 'npr. amir.hadzic@arena.local',
              isDense: true,
              prefixIcon: Icon(Icons.search, size: 20),
            ),
            onSubmitted: (_) => onSearch(),
          ),
        ),
        if (showStatusFilter)
          SizedBox(
            width: 200,
            child: DropdownButtonFormField<int?>(
              isExpanded: true,
              value: statusFilterId,
              decoration: const InputDecoration(labelText: 'Status uplate', isDense: true),
              items: [
                const DropdownMenuItem<int?>(value: null, child: Text('Svi')),
                for (final s in paymentStatuses)
                  DropdownMenuItem<int?>(
                    value: s.id,
                    child: Text(s.displayName, overflow: TextOverflow.ellipsis),
                  ),
              ],
              onChanged: onStatusChanged,
            ),
          ),
        OutlinedButton.icon(
          onPressed: onPickFrom,
          icon: const Icon(Icons.calendar_today, size: 18),
          label: Text(filterFrom == null ? 'Od datuma' : _shortDate.format(filterFrom!)),
        ),
        OutlinedButton.icon(
          onPressed: onPickTo,
          icon: const Icon(Icons.calendar_today, size: 18),
          label: Text(filterTo == null ? 'Do datuma' : _shortDate.format(filterTo!)),
        ),
        FilledButton.tonalIcon(
          onPressed: onSearch,
          icon: const Icon(Icons.filter_alt_outlined),
          label: const Text('Primijeni'),
        ),
        if (showAppOnlyFilter)
          FilterChip(
            label: const Text('Samo aplikacijske'),
            selected: excludeDemoSeed,
            onSelected: onExcludeDemoSeedChanged,
          ),
        TextButton.icon(
          onPressed: onClearFilters,
          icon: const Icon(Icons.clear_all, size: 18),
          label: const Text('Očisti filtere'),
        ),
      ],
    );
  }
}

class _PaymentsTable extends StatelessWidget {
  const _PaymentsTable({
    required this.page,
    required this.dateFmt,
    required this.moneyFmt,
    required this.statusColor,
    required this.onPage,
  });

  final PagedList<ExternalPaymentListItem>? page;
  final DateFormat dateFmt;
  final NumberFormat moneyFmt;
  final Color Function(String?) statusColor;
  final void Function({int? page}) onPage;

  @override
  Widget build(BuildContext context) {
    final items = page?.items ?? [];
    if (items.isEmpty) {
      return const Center(child: Text('Nema uplata za prikaz.'));
    }
    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Card(
              elevation: 0,
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: DataTable(
                  columns: const [
                    DataColumn(label: Text('ID')),
                    DataColumn(label: Text('Korisnik')),
                    DataColumn(label: Text('Svrha')),
                    DataColumn(label: Text('Provajder')),
                    DataColumn(label: Text('Iznos')),
                    DataColumn(label: Text('Koini')),
                    DataColumn(label: Text('Status')),
                    DataColumn(label: Text('Datum')),
                  ],
                  rows: [
                    for (final p in items)
                      DataRow(
                        cells: [
                          DataCell(Text('${p.id}')),
                          DataCell(_TableUserCell(
                            email: p.userEmail,
                            userId: p.userId,
                            isDemo: p.isDemoSeed,
                          )),
                          DataCell(Text(_purposeLabel(p.purposeCode))),
                          DataCell(Text(p.provider)),
                          DataCell(Text('${moneyFmt.format(p.amountMoney)} ${p.currency}')),
                          DataCell(Text(moneyFmt.format(p.coinsPurchased))),
                          DataCell(
                            Chip(
                              label: Text(p.paymentStatusCode ?? '-', style: const TextStyle(fontSize: 11)),
                              backgroundColor: statusColor(p.paymentStatusCode).withValues(alpha: 0.15),
                              side: BorderSide.none,
                              visualDensity: VisualDensity.compact,
                            ),
                          ),
                          DataCell(Text(dateFmt.format(p.createdUtc))),
                        ],
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
        if (page != null)
          PagedFooter(
            page: page!.page,
            totalPages: page!.totalPages,
            totalCount: page!.totalCount,
            onPageChanged: (p) => onPage(page: p),
          ),
      ],
    );
  }

  static String _purposeLabel(String code) {
    if (code.toUpperCase().contains('COIN')) {
      return 'Kupovina koina';
    }
    if (code.toUpperCase().contains('SESSION')) {
      return 'Plaćanje termina';
    }
    return code;
  }
}

class _LedgerTable extends StatelessWidget {
  const _LedgerTable({required this.page, required this.dateFmt, required this.onPage});

  final PagedList<CoinLedgerListItem>? page;
  final DateFormat dateFmt;
  final void Function({int? page}) onPage;

  @override
  Widget build(BuildContext context) {
    final items = page?.items ?? [];
    if (items.isEmpty) {
      return const Center(child: Text('Nema stavki u knjizi koina.'));
    }
    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Card(
              elevation: 0,
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: DataTable(
                  columnSpacing: 28,
                  columns: const [
                    DataColumn(label: Text('Korisnik')),
                    DataColumn(label: Text('Iznos')),
                    DataColumn(label: Text('Stanje poslije')),
                    DataColumn(label: Text('Razlog')),
                    DataColumn(label: Text('Dvorana / termin')),
                    DataColumn(label: Text('Datum')),
                  ],
                  rows: [
                    for (final e in items)
                      DataRow(
                        cells: [
                          DataCell(_TableUserCell(
                            email: e.userEmail,
                            userId: e.userId,
                            isDemo: e.isDemoSeed,
                          )),
                          DataCell(Text(e.amountCoins.toStringAsFixed(2))),
                          DataCell(Text(e.balanceAfter.toStringAsFixed(2))),
                          DataCell(Text(_reasonLabel(e.reasonCode))),
                          DataCell(Text(
                            e.relatedHallName != null
                                ? '${e.relatedHallName} (#${e.relatedScheduledSessionId ?? '-'})'
                                : '-',
                            overflow: TextOverflow.ellipsis,
                          )),
                          DataCell(Text(dateFmt.format(e.createdUtc))),
                        ],
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
        if (page != null)
          PagedFooter(
            page: page!.page,
            totalPages: page!.totalPages,
            totalCount: page!.totalCount,
            onPageChanged: (p) => onPage(page: p),
          ),
      ],
    );
  }

  static String _reasonLabel(String code) {
    switch (code) {
      case 'COIN_PURCHASE_CREDIT':
        return 'Kupovina koina';
      case 'COIN_PURCHASE_REFUND':
        return 'Refund kupovine';
      case 'SESSION_JOIN':
        return 'Pridruživanje terminu';
      case 'SESSION_REFUND_CANCEL':
        return 'Povrat (otkazani termin)';
      case 'SEED_INITIAL':
        return 'Demo početno stanje';
      default:
        return code;
    }
  }
}

class _WalletsTable extends StatelessWidget {
  const _WalletsTable({required this.page, required this.dateFmt, required this.onPage});

  final PagedList<CoinWalletListItem>? page;
  final DateFormat dateFmt;
  final void Function({int? page}) onPage;

  @override
  Widget build(BuildContext context) {
    final items = page?.items ?? [];
    if (items.isEmpty) {
      return const Center(child: Text('Nema novčanika.'));
    }
    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Card(
              elevation: 0,
              child: DataTable(
                columns: const [
                  DataColumn(label: Text('Korisnik')),
                  DataColumn(label: Text('Stanje (koini)')),
                  DataColumn(label: Text('Ažurirano')),
                ],
                rows: [
                  for (final w in items)
                    DataRow(
                      cells: [
                        DataCell(Text(w.userEmail ?? w.userId)),
                        DataCell(Text(w.balanceCoins.toStringAsFixed(2))),
                        DataCell(Text(dateFmt.format(w.updatedUtc))),
                      ],
                    ),
                ],
              ),
            ),
          ),
        ),
        if (page != null)
          PagedFooter(
            page: page!.page,
            totalPages: page!.totalPages,
            totalCount: page!.totalCount,
            onPageChanged: (p) => onPage(page: p),
          ),
      ],
    );
  }
}

class _HallEarningsTable extends StatelessWidget {
  const _HallEarningsTable({required this.items, required this.moneyFmt});

  final List<HallEarningsListItem> items;
  final NumberFormat moneyFmt;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return const Center(child: Text('Nema podataka o zaradi po dvoranama.'));
    }
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Card(
        elevation: 0,
        child: DataTable(
          columns: const [
            DataColumn(label: Text('Dvorana')),
            DataColumn(label: Text('Grad')),
            DataColumn(label: Text('Broj termina')),
            DataColumn(label: Text('Ukupno koina')),
          ],
          rows: [
            for (final h in items)
              DataRow(
                cells: [
                  DataCell(Text(h.hallName)),
                  DataCell(Text(h.cityName)),
                  DataCell(Text('${h.sessionCount}')),
                  DataCell(Text(moneyFmt.format(h.totalCoinsEarned))),
                ],
              ),
          ],
        ),
      ),
    );
  }
}

class _ReportsPanel extends StatelessWidget {
  const _ReportsPanel({
    required this.reportFrom,
    required this.reportTo,
    required this.downloading,
    required this.dateFmt,
    required this.onPickFrom,
    required this.onPickToEnd,
    required this.onSessions,
    required this.onTransactions,
    required this.onUsers,
  });

  final DateTime? reportFrom;
  final DateTime? reportTo;
  final bool downloading;
  final DateFormat dateFmt;
  final VoidCallback onPickFrom;
  final VoidCallback onPickToEnd;
  final VoidCallback onSessions;
  final VoidCallback onTransactions;
  final VoidCallback onUsers;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Material(
      color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 16, 24, 20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Generisanje izvještaja (PDF)',
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            Text(
              'Odaberite period (opcionalno) i preuzmite izvještaj. Prazan period uključuje sve zapise.',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 12,
              runSpacing: 8,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                OutlinedButton.icon(
                  onPressed: onPickFrom,
                  icon: const Icon(Icons.date_range, size: 18),
                  label: Text(reportFrom == null ? 'Period od' : dateFmt.format(reportFrom!)),
                ),
                OutlinedButton.icon(
                  onPressed: onPickToEnd,
                  icon: const Icon(Icons.date_range, size: 18),
                  label: Text(reportTo == null ? 'Period do' : dateFmt.format(reportTo!)),
                ),
                if (downloading)
                  const Padding(
                    padding: EdgeInsets.all(8),
                    child: SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    ),
                  )
                else ...[
                  FilledButton.icon(
                    onPressed: onSessions,
                    icon: const Icon(Icons.picture_as_pdf_outlined),
                    label: const Text('Rezervacije'),
                  ),
                  FilledButton.icon(
                    onPressed: onTransactions,
                    icon: const Icon(Icons.picture_as_pdf_outlined),
                    label: const Text('Transakcije'),
                  ),
                  FilledButton.tonalIcon(
                    onPressed: onUsers,
                    icon: const Icon(Icons.picture_as_pdf_outlined),
                    label: const Text('Korisnici'),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _TableUserCell extends StatelessWidget {
  const _TableUserCell({
    required this.email,
    required this.userId,
    this.isDemo = false,
  });

  final String? email;
  final String userId;
  final bool isDemo;

  @override
  Widget build(BuildContext context) {
    final label = email ?? userId;
    if (!isDemo) {
      return Text(label, overflow: TextOverflow.ellipsis, maxLines: 2);
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, overflow: TextOverflow.ellipsis, maxLines: 2),
        const SizedBox(height: 4),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
          decoration: BoxDecoration(
            color: const Color(0xFFE2E8F0),
            borderRadius: BorderRadius.circular(4),
          ),
          child: const Text('Demo', style: TextStyle(fontSize: 10, height: 1.1)),
        ),
      ],
    );
  }
}

