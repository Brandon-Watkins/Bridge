using System;
using System.Collections.Generic;
using System.Text;

namespace ISU_Bridge
{
    /// <summary>
    /// An interface for a simple observer. Just ensures they use the OnNext method
    /// </summary>
    public interface SimpleObserver
    {
        /// <summary>
        /// This will be called whenever its subscribed Observables call NotifyObservers.
        /// Brandon Watkins
        /// </summary>
        /// <param name="o">(SimpleObservable) The specific Observable that called NotifyObservers</param>
        public void OnNext(SimpleObservable o = null)
        {

        }
    }
}
