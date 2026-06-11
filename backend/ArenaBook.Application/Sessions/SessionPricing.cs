namespace ArenaBook.Application.Sessions;

/// <summary>
/// Ekonomija termina:
/// <list type="bullet">
/// <item><description><see cref="ComputeTotalPrice"/> — ukupna cijena najma dvorane za slot (satnica × trajanje).</description></item>
/// <item><description><see cref="ComputeParticipantJoinPrice"/> — iznos koji jedan učesnik plaća pri pridruživanju (organizator ne plaća).</description></item>
/// </list>
/// Vrijednosti se snapshotuju na <c>ScheduledSession</c> pri kreiranju i ne mijenjaju se nakon što postoji plaćeni učesnik.
/// </summary>
public static class SessionPricing
{
  public const decimal MinimumBillableHours = 0.25m;

  public static decimal ComputeTotalPrice(decimal pricePerHourCoins, DateTime startUtc, DateTime endUtc)
  {
    var hours = (decimal)(endUtc - startUtc).TotalHours;
    if (hours <= 0)
      return 0;

    if (hours < MinimumBillableHours)
      hours = MinimumBillableHours;

    return Math.Round(pricePerHourCoins * hours, 2, MidpointRounding.AwayFromZero);
  }

  /// <summary>
  /// Svaki učesnik (osim organizatora) plaća pun iznos najma termina pri pridruživanju.
  /// </summary>
  public static decimal ComputeParticipantJoinPrice(decimal totalSessionPriceCoins) =>
      totalSessionPriceCoins;
}
