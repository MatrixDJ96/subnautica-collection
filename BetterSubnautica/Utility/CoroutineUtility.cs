using System;
using System.Collections;
using UnityEngine;

namespace BetterSubnautica.Utility
{
    public static class CoroutineUtility
    {
        public static IEnumerator WaitUntil(Func<bool> predicate, Action action = null)
        {
            yield return new WaitUntil(predicate);

            if (action != null)
            {
                action.Invoke();
            }
        }
    }
}
