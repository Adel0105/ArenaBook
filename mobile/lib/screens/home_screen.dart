import 'package:arena_book_mobile/core/app_theme.dart';

import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/models/hall_models.dart';
import 'package:arena_book_mobile/models/paged_list.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';

import 'package:arena_book_mobile/screens/create_edit_session_screen.dart';

import 'package:arena_book_mobile/screens/hall_detail_screen.dart';

import 'package:arena_book_mobile/screens/join_session_screen.dart';

import 'package:arena_book_mobile/screens/my_organized_sessions_screen.dart';

import 'package:arena_book_mobile/screens/notifications_screen.dart';

import 'package:arena_book_mobile/screens/recommendations_screen.dart';

import 'package:arena_book_mobile/screens/review_screen.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/services/notification_polling_controller.dart';

import 'package:arena_book_mobile/widgets/app_section.dart';

import 'package:flutter/material.dart';

import 'package:intl/intl.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({
    super.key,
    required this.api,
    required this.user,
    required this.notifications,
  });

  final ArenaBookApi api;

  final CurrentUser user;

  final NotificationPollingController notifications;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<RecommendedHall> _recommended = [];

  List<RecommendedSession> _recommendedSessions = [];

  List<SessionListItem> _upcoming = [];

  List<SessionListItem> _publicNearby = [];

  List<PendingReview> _pendingReviews = [];

  bool _loading = true;

  String? _error;

  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();

    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;

      _error = null;
    });

    try {
      final results = await Future.wait([
        widget.api.recommendations(limit: 5),
        widget.api.recommendedSessions(limit: 5),
        widget.api.mySessions(
          dateFromUtc: DateTime.now().toUtc(),
          pageSize: 10,
        ),
        widget.api.sessions(
          dateFromUtc: DateTime.now().toUtc(),
          pageSize: 20,
        ),
        widget.api.pendingReviews(),
      ]);

      if (!mounted) {
        return;
      }

      final recommended = results[0] as List<RecommendedHall>;
      final recSessions = results[1] as List<RecommendedSession>;
      final upcomingPage = results[2] as PagedList<SessionListItem>;
      final public = results[3] as PagedList<SessionListItem>;
      final pending = results[4] as List<PendingReview>;

      setState(() {
        _recommended = recommended;
        _recommendedSessions = recSessions;
        _upcoming = upcomingPage.items;
        _publicNearby = public.items
            .where((s) =>
                s.sessionKindCode == 'PUBLIC' &&
                s.sessionLifecycleCode == 'CONFIRMED')
            .take(8)
            .toList();
        _pendingReviews = pending;
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
          _error = ApiError.friendlyMessage(e);
          _loading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const Icon(Icons.waving_hand_outlined,
                size: 22, color: AppColors.forest),
            const SizedBox(width: 8),
            Text('Bok, ${widget.user.firstName}'),
          ],
        ),
        actions: [
          ListenableBuilder(
            listenable: widget.notifications,
            builder: (context, _) {
              final unread = widget.notifications.unreadCount;
              return IconButton(
                tooltip: 'Notifikacije',
                icon: Badge(
                  label: Text('$unread'),
                  isLabelVisible: unread > 0,
                  child: const Icon(Icons.notifications_outlined),
                ),
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => NotificationsScreen(
                        api: widget.api,
                        notifications: widget.notifications,
                      ),
                    ),
                  );
                },
              );
            },
          ),
          IconButton(
            tooltip: 'Osvježi',
            icon: const Icon(Icons.refresh),
            onPressed: _load,
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final ok = await Navigator.of(context).push<bool>(
            MaterialPageRoute(
                builder: (_) => CreateEditSessionScreen(api: widget.api)),
          );

          if (ok == true) {
            _load();
          }
        },
        icon: const Icon(Icons.add),
        label: const Text('Kreiraj termin'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(0, 8, 0, 88),
                children: [
                  if (_error != null)
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: Text(_error!,
                          style: TextStyle(
                              color: Theme.of(context).colorScheme.error)),
                    ),
                  AppSection(
                    title: 'Brze radnje',
                    subtitle: 'Organizator termina',
                    icon: Icons.bolt_outlined,
                    tone: AppSectionTone.slate,
                    action: null,
                    children: [
                      OutlinedButton.icon(
                        onPressed: () {
                          Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (_) =>
                                  MyOrganizedSessionsScreen(api: widget.api),
                            ),
                          );
                        },
                        icon: const Icon(Icons.event_note_outlined),
                        label: const Text('Upravljaj mojim terminima'),
                      ),
                    ],
                  ),
                  AppSection(
                    title: 'Preporučene dvorane',
                    subtitle: 'Iz vašeg grada, rangirane po ocjenama i lajkovima',
                    icon: Icons.recommend_outlined,
                    tone: AppSectionTone.mint,
                    action: TextButton(
                      onPressed: () {
                        Navigator.of(context).push(
                          MaterialPageRoute(
                              builder: (_) =>
                                  RecommendationsScreen(
                                      api: widget.api, user: widget.user)),
                        );
                      },
                      child: const Text('Vidi sve'),
                    ),
                    children: [
                      if (_recommended.isEmpty)
                        Text(
                          widget.user.cityId == null
                              ? 'Postavite grad u profilu za preporuke dvorana.'
                              : 'Nema preporučenih dvorana u vašem gradu.',
                        )
                      else
                        ..._recommended.map(
                          (h) => AppListTile(
                            icon: Icons.stadium_outlined,
                            title: h.name,
                            subtitle:
                                '${h.cityName} · ★ ${h.averageRating.toStringAsFixed(1)}',
                            trailing: FilledButton(
                              onPressed: () {
                                Navigator.of(context).push(
                                  MaterialPageRoute(
                                    builder: (_) => HallDetailScreen(
                                        api: widget.api,
                                        hallId: h.hallId,
                                        user: widget.user),
                                  ),
                                );
                              },
                              child: const Text('Pogledaj'),
                            ),
                            onTap: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => HallDetailScreen(
                                      api: widget.api,
                                      hallId: h.hallId,
                                      user: widget.user),
                                ),
                              );
                            },
                          ),
                        ),
                    ],
                  ),
                  AppSection(
                    title: 'Preporučeni termini',
                    subtitle: 'Termini u dvoranama iz vašeg grada',
                    icon: Icons.event_available_outlined,
                    tone: AppSectionTone.neutral,
                    children: [
                      if (_recommendedSessions.isEmpty)
                        Text(
                          widget.user.cityId == null
                              ? 'Postavite grad u profilu za preporuke termina.'
                              : 'Nema preporučenih termina u vašem gradu.',
                        )
                      else
                        ..._recommendedSessions.map(
                          (s) => AppListTile(
                            icon: Icons.sports_soccer_outlined,
                            title: s.hallName,
                            subtitle:
                                '${_fmt.format(s.startUtc.toLocal())} · ${s.sessionKindCode}\nOrganizator: ${s.organizerEmail ?? '—'}',
                            trailing: const Icon(Icons.chevron_right),
                            onTap: () {
                              Navigator.of(context)
                                  .push(
                                    MaterialPageRoute(
                                      builder: (_) => JoinSessionScreen(
                                        api: widget.api,
                                        session: SessionListItem(
                                          id: s.sessionId,
                                          hallId: s.hallId,
                                          hallName: s.hallName,
                                          sessionKindCode: s.sessionKindCode,
                                          sessionLifecycleCode: 'CONFIRMED',
                                          startUtc: s.startUtc,
                                          endUtc: s.endUtc,
                                          maxParticipants: s.maxParticipants,
                                          participantCount: s.participantCount,
                                          priceTotalCoins: s.priceTotalCoins,
                                          organizerEmail: s.organizerEmail,
                                        ),
                                      ),
                                    ),
                                  )
                                  .then((_) => _load());
                            },
                          ),
                        ),
                    ],
                  ),
                  AppSection(
                    title: 'Javni termini u ponudi',
                    subtitle: 'Potvrđeni termini otvoreni za prijavu',
                    icon: Icons.groups_outlined,
                    tone: AppSectionTone.slate,
                    children: [
                      if (_publicNearby.isEmpty)
                        const Text('Trenutno nema javnih termina.')
                      else
                        ..._publicNearby.map(
                          (s) => AppListTile(
                            icon: Icons.login,
                            title: s.hallName,
                            subtitle:
                                '${_fmt.format(s.startUtc.toLocal())} · ${s.participantCount}/${s.maxParticipants} mjesta',
                            trailing: NovcicChip(amount: s.priceTotalCoins),
                            onTap: () {
                              Navigator.of(context)
                                  .push(
                                    MaterialPageRoute(
                                      builder: (_) => JoinSessionScreen(
                                          api: widget.api, session: s),
                                    ),
                                  )
                                  .then((_) => _load());
                            },
                          ),
                        ),
                    ],
                  ),
                  AppSection(
                    title: 'Nadolazeći termini',
                    subtitle: 'Vaše buduće rezervacije',
                    icon: Icons.upcoming_outlined,
                    tone: AppSectionTone.mint,
                    children: [
                      if (_upcoming.isEmpty)
                        const Text('Nemate nadolazećih termina.')
                      else
                        ..._upcoming.map(
                          (s) => AppListTile(
                            icon: Icons.calendar_today_outlined,
                            title: s.hallName,
                            subtitle:
                                '${_fmt.format(s.startUtc.toLocal())} · ${s.sessionLifecycleCode}',
                          ),
                        ),
                    ],
                  ),
                  if (_pendingReviews.isNotEmpty)
                    AppSection(
                      title: 'Ocijenite dvoranu',
                      subtitle: 'Nakon završenog termina',
                      icon: Icons.star_outline,
                      tone: AppSectionTone.neutral,
                      children: _pendingReviews
                          .map(
                            (p) => AppListTile(
                              icon: Icons.rate_review_outlined,
                              title: p.hallName,
                              trailing: const Text('Ocijeni →'),
                              onTap: () async {
                                await Navigator.of(context).push(
                                  MaterialPageRoute(
                                    builder: (_) => ReviewScreen(
                                      api: widget.api,
                                      hallId: p.hallId,
                                      hallName: p.hallName,
                                      sessionId: p.scheduledSessionId,
                                    ),
                                  ),
                                );

                                _load();
                              },
                            ),
                          )
                          .toList(),
                    ),
                ],
              ),
            ),
    );
  }
}

