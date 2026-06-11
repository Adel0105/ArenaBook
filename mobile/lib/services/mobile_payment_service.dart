import 'package:arena_book_mobile/core/api_config.dart';
import 'package:arena_book_mobile/models/coin_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/services/paypal_checkout_handler.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:url_launcher/url_launcher.dart';

class MobilePaymentService {
  MobilePaymentService(this.api);

  final ArenaBookApi api;

  Future<CoinPurchaseResult> payWithStripe(double coins) async {
    final intent = await api.stripeCreateIntent(coins);
    if (intent.clientSecret.isEmpty) {
      throw ApiError(null, 'Stripe nije vratio client secret.');
    }

    final publishableKey = intent.publishableKey.isNotEmpty
        ? intent.publishableKey
        : ApiConfig.stripePublishableKey;
    if (publishableKey.isEmpty) {
      throw ApiError(
        null,
        'Stripe publishable key nije konfiguriran (API ili STRIPE_PUBLISHABLE_KEY).',
      );
    }

    Stripe.publishableKey = publishableKey;
    await Stripe.instance.applySettings();

    await Stripe.instance.initPaymentSheet(
      paymentSheetParameters: SetupPaymentSheetParameters(
        paymentIntentClientSecret: intent.clientSecret,
        merchantDisplayName: 'ArenaBook',
      ),
    );

    try {
      await Stripe.instance.presentPaymentSheet();
    } on StripeException catch (e) {
      if (e.error.code == FailureCode.Canceled) {
        throw ApiError(null, 'Plaćanje karticom je otkazano.');
      }
      throw ApiError(
        null,
        e.error.localizedMessage ?? e.error.message ?? 'Stripe plaćanje nije uspjelo.',
      );
    }

    return api.stripeCompletePurchase(intent.paymentIntentId);
  }

  Future<CoinPurchaseResult> payWithPayPal(double coins) async {
    final order = await api.paypalCreateOrder(
      coins,
      returnUrl: PayPalCheckoutHandler.returnUrl,
      cancelUrl: PayPalCheckoutHandler.cancelUrl,
    );

    if (order.approvalUrl.isEmpty) {
      throw ApiError(null, 'PayPal nije vratio approval URL.');
    }

    final approvalFuture =
        PayPalCheckoutHandler.instance.waitForApproval(order.payPalOrderId);

    final launched = await launchUrl(
      Uri.parse(order.approvalUrl),
      mode: LaunchMode.externalApplication,
    );
    if (!launched) {
      PayPalCheckoutHandler.instance.cancelPending();
      throw ApiError(null, 'PayPal stranica nije mogla biti otvorena.');
    }

    final approvedOrderId = await approvalFuture;
    return api.paypalCapture(approvedOrderId);
  }
}
