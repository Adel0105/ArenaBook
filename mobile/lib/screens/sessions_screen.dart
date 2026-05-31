import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/screens/join_session_screen.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:table_calendar/table_calendar.dart';

class SessionsScreen extends StatefulWidget {
  const SessionsScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<SessionsScreen> createState() => _SessionsScreenState();
}

class _SessionsScreenState extends State<SessionsScreen> {
  DateTime _focused = DateTime.now();
  DateTime? _selected;
  List<SessionListItem> _sessions = [];
  List<SessionListItem> _mine = [];
  List<SessionKindItem> _kinds = [];
  int? _kindFilterId;
  String? _kindFilterCode;
  bool _loading = true;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');
  static final _dayFmt = DateFormat('dd.MM.yyyy');

  @override
  void initState() {
    super.initState();
    _selected = DateTime.now();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    try {
      final kinds = await widget.api.sessionKinds();
      if (mounted) {
        setState(() => _kinds = kinds.items);
      }
    } catch (_) {}
    await _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final from = DateTime(_focused.year, _focused.month, 1);
    final to = DateTime(_focused.year, _focused.month + 1, 0, 23, 59, 59);
    try {
      final all = await widget.api.sessions(
        dateFromUtc: from.toUtc(),
        dateToUtc: to.toUtc(),
        sessionKindId: _kindFilterId,
        pageSize: 100,
      );
      final mine = await widget.api.mySessions(pageSize: 100);
      if (mounted) {
        var sessions = all.items
            .where((s) => s.sessionLifecycleCode == 'CONFIRMED')
            .toList();
        if (_kindFilterCode != null) {
          sessions = sessions
              .where((s) => s.sessionKindCode == _kindFilterCode)
              .toList();
        }
        setState(() {
          _sessions = sessions;
          _mine = mine.items;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  List<SessionListItem> _forDay(DateTime day) {
    return _sessions.where((s) {
      final local = s.startUtc.toLocal();
      return local.year == day.year &&
          local.month == day.month &&
          local.day == day.day;
    }).toList();
  }

  List<SessionListItem> _upcomingReminders() {
    final now = DateTime.now();
    return _mine.where((s) {
      final local = s.startUtc.toLocal();
      return local.isAfter(now) && local.difference(now).inHours <= 48;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return Scaffold(
        appBar: AppBar(
          title: const Row(
            children: [
              Icon(Icons.event_outlined, size: 22),
              SizedBox(width: 8),
              Text('Kalendar termina'),
            ],
          ),
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    final day = _selected ?? DateTime.now();
    final daySessions = _forDay(day);
    final reminders = _upcomingReminders();
    final bottomInset = MediaQuery.paddingOf(context).bottom;

    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.event_outlined, size: 22),
            SizedBox(width: 8),
            Text('Kalendar termina'),
          ],
        ),
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            if (reminders.isNotEmpty)
              SliverToBoxAdapter(
                child: MaterialBanner(
                  backgroundColor: AppColors.mintSurface,
                  content: Text(
                      'Podsjetnik: ${reminders.length} termin(a) u sljedeća 48h'),
                  leading: const Icon(Icons.alarm, color: AppColors.forest),
                  actions: const [SizedBox.shrink()],
                ),
              ),
            SliverToBoxAdapter(
              child: AppSection(
                title: 'Filter tipa termina',
                icon: Icons.filter_list_outlined,
                tone: AppSectionTone.slate,
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
                children: [
                  SizedBox(
                    height: 40,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      children: [
                        FilterChip(
                          label: const Text('Svi tipovi'),
                          selected: _kindFilterId == null,
                          onSelected: (_) {
                            setState(() {
                              _kindFilterId = null;
                              _kindFilterCode = null;
                            });
                            _load();
                          },
                        ),
                        ..._kinds.map(
                          (k) => Padding(
                            padding: const EdgeInsets.only(left: 6),
                            child: FilterChip(
                              label: Text(k.displayName),
                              selected: _kindFilterId == k.id,
                              onSelected: (_) {
                                setState(() {
                                  _kindFilterId = k.id;
                                  _kindFilterCode = k.code;
                                });
                                _load();
                              },
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            SliverToBoxAdapter(
              child: AppSection(
                title: 'Kalendar',
                subtitle: 'Odaberite dan za prikaz termina',
                icon: Icons.calendar_month_outlined,
                tone: AppSectionTone.mint,
                padding: const EdgeInsets.fromLTRB(16, 4, 16, 4),
                children: [
                  TableCalendar(
                    firstDay: DateTime.utc(2024, 1, 1),
                    lastDay: DateTime.utc(2030, 12, 31),
                    focusedDay: _focused,
                    rowHeight: 40,
                    daysOfWeekHeight: 16,
                    selectedDayPredicate: (d) => isSameDay(_selected, d),
                    calendarStyle:
                        const CalendarStyle(cellMargin: EdgeInsets.all(2)),
                    headerStyle: const HeaderStyle(
                      formatButtonVisible: false,
                      titleCentered: true,
                    ),
                    onDaySelected: (selected, focused) {
                      setState(() {
                        _selected = selected;
                        _focused = focused;
                      });
                    },
                    onPageChanged: (focused) {
                      setState(() => _focused = focused);
                      _load();
                    },
                    eventLoader: _forDay,
                  ),
                ],
              ),
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
                child: AppSectionHeader(
                  title: 'Termini za ${_dayFmt.format(day)}',
                  subtitle: 'Moji termini ukupno: ${_mine.length}',
                  icon: Icons.schedule_outlined,
                ),
              ),
            ),
            if (daySessions.isEmpty)
              const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(child: Text('Nema termina za odabrani dan.')),
              )
            else
              SliverPadding(
                padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + bottomInset),
                sliver: SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, i) {
                      final s = daySessions[i];
                      return AppListTile(
                        icon: Icons.sports_soccer_outlined,
                        title: s.hallName,
                        subtitle:
                            '${_fmt.format(s.startUtc.toLocal())} · ${s.sessionKindCode}\nOrganizator: ${s.organizerEmail ?? '—'}',
                        trailing: NovcicChip(amount: s.priceTotalCoins),
                        onTap: () {
                          Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (_) => JoinSessionScreen(
                                  api: widget.api, session: s),
                            ),
                          );
                        },
                      );
                    },
                    childCount: daySessions.length,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

