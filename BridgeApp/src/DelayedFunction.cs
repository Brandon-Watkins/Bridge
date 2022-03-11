using System.Collections.Generic;
using System.Timers;

namespace ISU_Bridge
{
    /// <summary>
    /// Used to call a function after the set amount of time.
    /// Note: The function being passed to this needs 2 arguments: Object, and ElapsedEventArgs
    /// Brandon Watkins
    /// </summary>
    public class DelayedFunction
    {

        private Timer timer;
        private string caller = "";
        // List of string representations of the objects calling the delayed function.
        // Used to keep from adding a timer when it already has an active timer.
        private static List<string> callers = new List<string>();
        // List of all active timers.
        // Mostly used in the event that you want to cancel all active timers.
        private static List<DelayedFunction> timers = new List<DelayedFunction>();

        /// <summary>
        /// Constructor.
        /// Brandon Watkins
        /// </summary>
        /// <param name="time">(double) Milliseconds to wait for before calling the function</param>
        /// <param name="eventHandler">(ElapsedEventHandler) Function to call after timer</param>
        /// <param name="source">(object) The instance calling the function</param>
        public DelayedFunction(double time, ElapsedEventHandler eventHandler, object source)
        {
            caller = source.ToString() + "-" + eventHandler.Method.Name.ToString();
            // Don't create a new timer if one exists.
            if (!callers.Contains(caller))
            {
                // Can't create a timer with 0 time. If 0, just run the function now.
                // 0 is used when the user calls the function, vs. the AI.
                if (time < 1)
                {
                    eventHandler.Invoke(source, null);
                    return;
                }

                callers.Add(caller);

                // Creating the actual timer being used.
                timer = new System.Timers.Timer(time / Game.computerSpeedMultiplier);
                timer.Elapsed += eventHandler;
                // automatically removes the caller and timer from the lists, once the given function is executed.
                timer.Elapsed += RemoveTimer;
                timer.AutoReset = false;
                timer.Enabled = true;

                timers.Add(this);
            }
        }

        /// <summary>
        /// Removes the caller and the timer from the static lists, allowing the caller to create another timer.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void RemoveTimer(object src, ElapsedEventArgs e)
        {
            if (callers.Contains(caller)) callers.Remove(caller);
            if (timers.Contains(this)) timers.Remove(this);
            caller = null;
            timer = null;
        }

        /// <summary>
        /// Use this at the end of a trick, or other times you might need to cancel your active timers.
        /// I don't believe I need this anymore.
        /// Brandon Watkins
        /// </summary>
        public static void ClearTimers()
        {
            foreach (DelayedFunction tmr in timers)
            {
                tmr.timer = null;
            }
            timers.Clear();
            callers.Clear();
        }
    }
}
