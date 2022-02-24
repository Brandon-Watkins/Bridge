using System.Collections.Generic;

namespace ISU_Bridge
{

    /// <summary>
    /// Simple observable class, for managing subscribers, and alerting them when NotifyObservers() is called.
    /// Brandon Watkins
    /// </summary>
    public class SimpleObservable
    {

        // List of observers who subscribed
        protected List<SimpleObserver> observers = new List<SimpleObserver>();

        /// <summary>
        /// Removes the observer from it's list of subscribers, so it won't be notified anymore.
        /// Brandon Watkins
        /// </summary>
        /// <param name="o">(SimpleObserver) The observer to remove from subscribers</param>
        public void Unsubscribe(SimpleObserver o)
        {
            if (observers.Contains(o)) observers.Remove(o);
        }

        /// <summary>
        /// Adds the observer to the list of subscribers, so it will be notified on NotifyObservers call.
        /// Brandon Watkins
        /// </summary>
        /// <param name="o">(SimpleObserver) The observer to add to subscribers</param>
        public void Subscribe(SimpleObserver o)
        {
            if (o is SimpleObserver && !observers.Contains(o))
                observers.Add(o);
        }

        /// <summary>
        /// Calls OnNext on each of the subscribers. 
        /// Use this whenever the data you're interested in changes.
        /// Brandon Watkins
        /// </summary>
        protected void NotifyObservers()
        {
            foreach (SimpleObserver observer in observers)
                observer.OnNext(this);
        }
    }
}
