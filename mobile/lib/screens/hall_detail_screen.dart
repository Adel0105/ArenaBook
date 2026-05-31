import 'package:arena_book_mobile/core/app_currency.dart';
import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/models/hall_models.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/screens/join_session_screen.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class HallDetailScreen extends StatefulWidget {
  const HallDetailScreen({
    super.key,
    required this.api,
    required this.hallId,
    required this.user,
  });

  final ArenaBookApi api;
  final int hallId;
  final CurrentUser user;

  @override
  State<HallDetailScreen> createState() => _HallDetailScreenState();
}

class _HallDetailScreenState extends State<HallDetailScreen> {
  HallDetails? _hall;
  List<HallPhoto> _photos = [];
  List<HallEquipmentItem> _equipment = [];
  List<HallReview> _reviews = [];
  List<SessionListItem> _sessions = [];
  HallReactionSummary? _reactions;
  bool _loading = true;
  bool _savingReview = false;
  bool _savingReaction = false;
  int _draftStars = 5;
  final _commentCtrl = TextEditingController();
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');
  static final _reviewDateFmt = DateFormat('dd.MM.yyyy');

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _commentCtrl.dispose();
    super.dispose();
  }

  HallReview? get _myReview {
    for (final r in _reviews) {
      if (r.userId == widget.user.userId) return r;
    }
    return null;
  }

  double get _averageRating {
    if (_reviews.isEmpty) return 0;
    final sum = _reviews.fold<int>(0, (a, r) => a + r.ratingStars);
    return sum / _reviews.length;
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final hall = await widget.api.hallById(widget.hallId);
      final photos = await widget.api.hallPhotos(widget.hallId);
      final equipment = await widget.api.hallEquipment(widget.hallId);
      final reviews = await widget.api.hallReviews(widget.hallId);
      final reactions = await widget.api.hallReactions(widget.hallId);
      final sessions =
          await widget.api.sessions(hallId: widget.hallId, pageSize: 30);
      if (mounted) {
        HallReview? mine;
        for (final r in reviews.items) {
          if (r.userId == widget.user.userId) {
            mine = r;
            break;
          }
        }
        setState(() {
          _hall = hall;
          _photos = photos.items;
          _equipment = equipment.items;
          _reviews = reviews.items;
          _reactions = reactions;
          _sessions = sessions.items
              .where((s) => s.sessionLifecycleCode == 'CONFIRMED')
              .toList();
          if (mine != null) {
            _draftStars = mine.ratingStars;
            _commentCtrl.text = mine.comment ?? '';
          }
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _submitReview() async {
    setState(() => _savingReview = true);
    try {
      await widget.api.createHallReview(widget.hallId, {
        'ratingStars': _draftStars,
        'comment': _commentCtrl.text.trim(),
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _myReview != null
                  ? 'Recenzija je ažurirana.'
                  : 'Hvala! Recenzija je objavljena.',
            ),
          ),
        );
      }
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _savingReview = false);
      }
    }
  }

  Future<void> _setReaction(String reaction) async {
    if (_savingReaction) return;
    setState(() => _savingReaction = true);
    try {
      final current = _reactions?.userReaction;
      final next = current == reaction ? 'none' : reaction;
      final summary =
          await widget.api.setHallReaction(widget.hallId, next);
      if (mounted) {
        setState(() => _reactions = summary);
      }
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _savingReaction = false);
      }
    }
  }

  Widget _buildStars(int rating, {double size = 18}) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: List.generate(
        5,
        (i) => Icon(
          i < rating ? Icons.star_rounded : Icons.star_border_rounded,
          color: Colors.amber.shade700,
          size: size,
        ),
      ),
    );
  }

  Widget _interactiveStarPicker() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: List.generate(
        5,
        (i) => IconButton(
          tooltip: '${i + 1} zvjezdica',
          icon: Icon(
            i < _draftStars ? Icons.star_rounded : Icons.star_border_rounded,
            color: Colors.amber.shade700,
            size: 34,
          ),
          onPressed: () => setState(() => _draftStars = i + 1),
        ),
      ),
    );
  }

  Widget _reactionButton({
    required IconData icon,
    required String label,
    required int count,
    required bool selected,
    required VoidCallback onTap,
    required Color selectedColor,
  }) {
    return Expanded(
      child: OutlinedButton.icon(
        onPressed: _savingReaction ? null : onTap,
        icon: Icon(icon, size: 20),
        label: Text('$label ($count)'),
        style: OutlinedButton.styleFrom(
          foregroundColor: selected ? selectedColor : null,
          backgroundColor: selected ? selectedColor.withValues(alpha: 0.12) : null,
          side: BorderSide(
            color: selected ? selectedColor : AppColors.cardBorder,
          ),
          padding: const EdgeInsets.symmetric(vertical: 12),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_loading || _hall == null) {
      return Scaffold(
        appBar: AppBar(),
        body: const Center(child: CircularProgressIndicator()),
      );
    }
    final h = _hall!;
    final reactions = _reactions;
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const Icon(Icons.stadium_outlined, size: 22),
            const SizedBox(width: 8),
            Expanded(child: Text(h.name, overflow: TextOverflow.ellipsis)),
          ],
        ),
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(0, 8, 0, 24),
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            AppSection(
              title: 'Osnovne informacije',
              subtitle: '${h.cityName}, ${h.countryName}',
              icon: Icons.info_outline,
              tone: AppSectionTone.mint,
              children: [
                _row(Icons.place_outlined, h.streetAddress),
                _row(Icons.groups_outlined, 'Kapacitet: ${h.capacityPeople}'),
                _row(Icons.toll_outlined,
                    AppCurrency.format(h.pricePerHourCoins, perHour: true)),
                _row(Icons.phone_outlined, h.contactPhone),
              ],
            ),
            if (_equipment.isNotEmpty)
              AppSection(
                title: 'Oprema',
                subtitle: 'Dostupna u dvorani',
                icon: Icons.sports_tennis,
                tone: AppSectionTone.slate,
                children: [
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: _equipment
                        .map((e) => Chip(
                              avatar: const Icon(Icons.check, size: 16),
                              label:
                                  Text('${e.equipmentTypeName} ×${e.quantity}'),
                            ))
                        .toList(),
                  ),
                ],
              ),
            if (_photos.isNotEmpty)
              AppSection(
                title: 'Fotografije',
                icon: Icons.photo_library_outlined,
                tone: AppSectionTone.neutral,
                children: [
                  SizedBox(
                    height: 120,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      children: _photos
                          .map(
                            (p) => Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ClipRRect(
                                borderRadius: BorderRadius.circular(8),
                                child: Image.network(
                                  p.imageUrl,
                                  width: 160,
                                  height: 120,
                                  fit: BoxFit.cover,
                                  errorBuilder: (_, __, ___) => Container(
                                    width: 160,
                                    height: 120,
                                    color: AppColors.slateSurface,
                                    child:
                                        const Icon(Icons.broken_image_outlined),
                                  ),
                                ),
                              ),
                            ),
                          )
                          .toList(),
                    ),
                  ),
                ],
              ),
            AppSection(
              title: 'Dostupni termini',
              subtitle: 'Potvrđeni termini za prijavu',
              icon: Icons.event_available_outlined,
              tone: AppSectionTone.mint,
              children: _sessions.isEmpty
                  ? [const Text('Nema potvrđenih termina.')]
                  : _sessions
                      .map(
                        (s) => AppListTile(
                          icon: Icons.schedule,
                          title: _fmt.format(s.startUtc.toLocal()),
                          subtitle:
                              '${s.sessionKindCode} · ${s.participantCount}/${s.maxParticipants}\nOrg: ${s.organizerEmail ?? '—'}',
                          trailing: NovcicChip(amount: s.priceTotalCoins),
                          onTap: () async {
                            await Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => JoinSessionScreen(
                                    api: widget.api, session: s),
                              ),
                            );
                            _load();
                          },
                        ),
                      )
                      .toList(),
            ),
            AppSection(
              title: 'Recenzije i ocjene',
              subtitle: _reviews.isEmpty
                  ? 'Budite prvi koji ocjenjuje ovu dvoranu'
                  : '${_reviews.length} recenzija · prosjek ${_averageRating.toStringAsFixed(1)} ★',
              icon: Icons.star_outline,
              tone: AppSectionTone.slate,
              children: [
                if (reactions != null) ...[
                  Row(
                    children: [
                      _reactionButton(
                        icon: Icons.thumb_up_outlined,
                        label: 'Sviđa mi se',
                        count: reactions.likeCount,
                        selected: reactions.userReaction == 'like',
                        selectedColor: AppColors.primaryGreen,
                        onTap: () => _setReaction('like'),
                      ),
                      const SizedBox(width: 10),
                      _reactionButton(
                        icon: Icons.thumb_down_outlined,
                        label: 'Ne sviđa mi se',
                        count: reactions.dislikeCount,
                        selected: reactions.userReaction == 'dislike',
                        selectedColor: Colors.red.shade700,
                        onTap: () => _setReaction('dislike'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                ],
                DecoratedBox(
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: AppColors.cardBorder),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          _myReview != null
                              ? 'Ažurirajte svoju recenziju'
                              : 'Ostavite recenziju',
                          style: Theme.of(context).textTheme.titleSmall,
                        ),
                        const SizedBox(height: 4),
                        _interactiveStarPicker(),
                        TextField(
                          controller: _commentCtrl,
                          decoration: const InputDecoration(
                            labelText: 'Komentar (opcionalno)',
                            hintText: 'Podijelite iskustvo s drugim igračima…',
                            alignLabelWithHint: true,
                          ),
                          maxLines: 3,
                          maxLength: 2000,
                        ),
                        const SizedBox(height: 4),
                        FilledButton.icon(
                          onPressed: _savingReview ? null : _submitReview,
                          icon: _savingReview
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : Icon(_myReview != null
                                  ? Icons.edit_outlined
                                  : Icons.send_outlined),
                          label: Text(_myReview != null
                              ? 'Spremi promjene'
                              : 'Objavi recenziju'),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                if (_reviews.isEmpty)
                  const Text('Još nema recenzija drugih igrača.')
                else
                  ..._reviews.map((r) {
                    final isMine = r.userId == widget.user.userId;
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: DecoratedBox(
                        decoration: BoxDecoration(
                          color: isMine
                              ? AppColors.primaryGreen.withValues(alpha: 0.06)
                              : Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: isMine
                                ? AppColors.primaryGreen.withValues(alpha: 0.25)
                                : AppColors.cardBorder,
                          ),
                        ),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(
                                      isMine
                                          ? '${r.userDisplayName} (vi)'
                                          : r.userDisplayName,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ),
                                  _buildStars(r.ratingStars),
                                ],
                              ),
                              if (r.comment != null &&
                                  r.comment!.trim().isNotEmpty) ...[
                                const SizedBox(height: 6),
                                Text(r.comment!),
                              ],
                              const SizedBox(height: 4),
                              Text(
                                _reviewDateFmt.format(r.createdUtc.toLocal()),
                                style: Theme.of(context)
                                    .textTheme
                                    .bodySmall
                                    ?.copyWith(color: Colors.grey.shade600),
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  }),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _row(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20),
          const SizedBox(width: 10),
          Expanded(child: Text(text)),
        ],
      ),
    );
  }
}

