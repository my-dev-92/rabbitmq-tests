namespace Receiver;

public class SleepDurationProvider
{
    #region Properties

    /// <summary>
    /// Generate exponential delays
    /// <example>
    /// for 15 retries it will be following exponential delays, in total 01:55 hr
    /// <list type="number">
    /// <item>
    ///     00:00:01.7
    /// </item>
    /// <item>
    ///     00:00:02.8
    /// </item>
    /// <item>
    ///     00:00:04.9
    /// </item>
    /// <item>
    ///     00:00:08.3
    /// </item>
    /// <item>
    ///     00:00:14.1
    /// </item>
    /// <item>
    ///     00:00:24.1
    /// </item>
    /// <item>
    ///     00:00:41.0
    /// </item>
    /// <item>
    ///     00:01:09.7
    /// </item>
    /// <item>
    ///     00:01:58.5
    /// </item>
    /// <item>
    ///     00:03:21.5
    /// </item>
    /// <item>
    ///     00:05:42.7
    /// </item>
    /// <item>
    ///     00:09:42.6
    /// </item>
    /// <item>
    ///     00:16:30.4
    /// </item>
    /// <item>
    ///     00:28:03.7
    /// </item>
    /// <item>
    ///     00:47:42.4
    /// </item>
    /// </list>
    /// </example>
    /// </summary>
    internal static Func<int, TimeSpan> ExponentialSleepDuration => reTryCount => TimeSpan.FromSeconds(Math.Pow(1.7, reTryCount));

    #endregion
}