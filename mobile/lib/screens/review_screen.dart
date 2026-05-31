import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:flutter/material.dart';

class ReviewScreen extends StatefulWidget {
  const ReviewScreen({
    super.key,
    required this.api,
    required this.hallId,
    required this.hallName,
    required this.sessionId,
  });

  final ArenaBookApi api;
  final int hallId;
  final String hallName;
  final int sessionId;

  @override
  State<ReviewScreen> createState() => _ReviewScreenState();
}

class _ReviewScreenState extends State<ReviewScreen> {
  int _stars = 5;
  final _commentCtrl = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _commentCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _saving = true);
    try {
      await widget.api.createHallReview(widget.hallId, {
        'scheduledSessionId': widget.sessionId,
        'ratingStars': _stars,
        'comment': _commentCtrl.text.trim(),
      });
      if (!mounted) {
        return;
      }
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Recenzija poslana'),
          content: const Text(
            'Hvala na povratnim informacijama. Vaša recenzija pomaže drugim igračima.',
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: const Text('Gotovo'),
            ),
          ],
        ),
      );
      if (mounted) {
        Navigator.of(context).pop(true);
      }
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Ocijeni ${widget.hallName}')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(
                5,
                (i) => IconButton(
                  icon: Icon(
                    i < _stars ? Icons.star : Icons.star_border,
                    color: Colors.amber,
                    size: 36,
                  ),
                  onPressed: () => setState(() => _stars = i + 1),
                ),
              ),
            ),
            TextField(
              controller: _commentCtrl,
              decoration: const InputDecoration(
                labelText: 'Komentar',
                alignLabelWithHint: true,
              ),
              maxLines: 4,
            ),
            const Spacer(),
            FilledButton(
              onPressed: _saving ? null : _submit,
              child: _saving
                  ? const CircularProgressIndicator()
                  : const Text('Pošalji recenziju'),
            ),
          ],
        ),
      ),
    );
  }
}

