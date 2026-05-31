import 'package:arena_book_mobile/core/app_currency.dart';
import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/models/hall_models.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/screens/hall_detail_screen.dart';
import 'package:arena_book_mobile/screens/join_session_screen.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class RecommendationsScreen extends StatefulWidget {
  const RecommendationsScreen({super.key, required this.api, required this.user});

  final ArenaBookApi api;
  final CurrentUser user;

  @override
  State<RecommendationsScreen> createState() => _RecommendationsScreenState();
}

class _RecommendationsScreenState extends State<RecommendationsScreen> {
  List<RecommendedHall> _halls = [];
  List<RecommendedSession> _sessions = [];
  String? _cityLabel;
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
      final halls = await widget.api.recommendations(limit: 30);
      final sessions = await widget.api.recommendedSessions(limit: 30);
      if (mounted) {
        setState(() {
          _halls = halls;
          _sessions = sessions;
          _cityLabel = halls.isNotEmpty
              ? halls.first.cityName
              : (sessions.isNotEmpty ? sessions.first.cityName : null);
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
    final cityHint = _cityLabel != null
        ? 'Preporuke za grad $_cityLabel — rangirano po ocjenama, komentarima i lajkovima.'
        : widget.user.cityId == null
            ? 'Postavite grad u profilu da biste vidjeli personalizirane preporuke.'
            : 'Nema preporuka za vaš grad.';

    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Row(
            children: [
              Icon(Icons.recommend_outlined, size: 22),
              SizedBox(width: 8),
              Text('Preporuke'),
            ],
          ),
          bottom: const TabBar(
            tabs: [
              Tab(icon: Icon(Icons.stadium_outlined), text: 'Dvorane'),
              Tab(icon: Icon(Icons.event_outlined), text: 'Termini'),
            ],
          ),
        ),
        body: Column(
          children: [
            AppSection(
              title: 'Vaš grad',
              subtitle: cityHint,
              icon: Icons.location_city_outlined,
              tone: AppSectionTone.slate,
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
              children: const [],
            ),
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : RefreshIndicator(
                      onRefresh: _load,
                      child: TabBarView(
                        children: [
                          _halls.isEmpty
                              ? ListView(
                                  physics: const AlwaysScrollableScrollPhysics(),
                                  children: const [
                                    SizedBox(height: 48),
                                    Center(
                                      child: Text(
                                        'Nema preporučenih dvorana u vašem gradu.',
                                      ),
                                    ),
                                  ],
                                )
                              : ListView.builder(
                                  padding: const EdgeInsets.all(16),
                                  itemCount: _halls.length,
                                  itemBuilder: (context, i) {
                                    final h = _halls[i];
                                    return Card(
                                      margin: const EdgeInsets.only(bottom: 10),
                                      child: ExpansionTile(
                                        leading:
                                            const Icon(Icons.stadium_outlined),
                                        title: Text(
                                          h.name,
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                        subtitle: Text(
                                          '${h.cityName} · ★ ${h.averageRating.toStringAsFixed(1)} · '
                                          '${h.reviewCount} recenzija · ${h.score.toStringAsFixed(0)} bodova · '
                                          '${AppCurrency.format(h.pricePerHourCoins, perHour: true)}',
                                        ),
                                        children: [
                                          Padding(
                                            padding: const EdgeInsets.all(12),
                                            child: Text(h.explanation),
                                          ),
                                          Align(
                                            alignment: Alignment.centerRight,
                                            child: TextButton.icon(
                                              onPressed: () {
                                                Navigator.of(context).push(
                                                  MaterialPageRoute(
                                                    builder: (_) =>
                                                        HallDetailScreen(
                                                      api: widget.api,
                                                      hallId: h.hallId,
                                                      user: widget.user,
                                                    ),
                                                  ),
                                                );
                                              },
                                              icon: const Icon(
                                                Icons.arrow_forward,
                                              ),
                                              label: const Text(
                                                'Pregled profila',
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    );
                                  },
                                ),
                          _sessions.isEmpty
                              ? ListView(
                                  physics: const AlwaysScrollableScrollPhysics(),
                                  children: const [
                                    SizedBox(height: 48),
                                    Center(
                                      child: Text(
                                        'Nema preporučenih termina u vašem gradu.',
                                      ),
                                    ),
                                  ],
                                )
                              : ListView.builder(
                                  padding: const EdgeInsets.all(16),
                                  itemCount: _sessions.length,
                                  itemBuilder: (context, i) {
                                    final s = _sessions[i];
                                    return AppListTile(
                                      icon: Icons.sports_soccer_outlined,
                                      title: s.hallName,
                                      subtitle:
                                          '${_fmt.format(s.startUtc.toLocal())} · ${s.cityName}\n'
                                          '${s.explanation}',
                                      trailing: const Icon(Icons.chevron_right),
                                      onTap: () {
                                        Navigator.of(context).push(
                                          MaterialPageRoute(
                                            builder: (_) => JoinSessionScreen(
                                              api: widget.api,
                                              session: SessionListItem(
                                                id: s.sessionId,
                                                hallId: s.hallId,
                                                hallName: s.hallName,
                                                sessionKindCode:
                                                    s.sessionKindCode,
                                                sessionLifecycleCode:
                                                    'CONFIRMED',
                                                startUtc: s.startUtc,
                                                endUtc: s.endUtc,
                                                maxParticipants:
                                                    s.maxParticipants,
                                                participantCount:
                                                    s.participantCount,
                                                priceTotalCoins:
                                                    s.priceTotalCoins,
                                                organizerEmail:
                                                    s.organizerEmail,
                                              ),
                                            ),
                                          ),
                                        );
                                      },
                                    );
                                  },
                                ),
                        ],
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

